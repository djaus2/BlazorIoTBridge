//using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BlazorSensorAppNet5.Shared;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Hello Sensor! Press [Enter] when service is running.");
            //Console.ReadLine();
            

            var builder = new ConfigurationBuilder()
                           .SetBasePath(Directory.GetCurrentDirectory())
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           //.AddUserSecrets<Program>()
                           .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            var mySettingsConfig = new AppSettings();
            configuration.GetSection("AppSettings").Bind(mySettingsConfig);

            string host = mySettingsConfig.Url;
            string api = mySettingsConfig.Api; ;

            int numToSend = mySettingsConfig.NumToSend;
            Console.WriteLine("Sending {0} messages", numToSend);


            Task[] myTasks = new Task[numToSend];
            for (int i=0;i<myTasks.Length;i+=3)
            {
                myTasks[i]= Send(i,host,api, SensorType.temperature, 67.8, null);
                await Task.Delay(333);
                if ((i + 1) < myTasks.Length)
                {
                    myTasks[i + 1] = Send(i + 1, host,api, SensorType.humidity, 34.78, null);
                    await Task.Delay(333);
                    if ((i + 2) < myTasks.Length)
                    {
                        myTasks[i + 2] = Send(i + 2, host,api, SensorType.pressure, 10001.2, null);
                        await Task.Delay(333);
                    }
                }

            }

            Task.WaitAll(myTasks);
            Console.WriteLine("Hello Sensor! End");

        }

        static int count = 0;
        static async Task Send(int No,string host, string api, SensorType sensorType, Double? value, List<Double> values, bool state=false)
        {
            try
            {
                using System.Net.Http.HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(host);

                Guid guid = Guid.NewGuid();
                long TimeStamp = DateTime.Now.Ticks;
                Sensor sensor = new Sensor();
                sensor.No = No;
                sensor.Id = guid.ToString();
                sensor.SensorType = sensorType;
                sensor.TimeStamp = TimeStamp;
                if (value != null)
                    sensor.Value = value;
                else
                    sensor.Value = (double)Sensor.iNull;
                sensor.Values = values;
                sensor.State = state;

                Console.Write("Sending ...");

                var response = await httpClient.PostAsJsonAsync<Sensor>(api, sensor);
                Console.Write(" Sent: ");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Sent OK");
                    bool keepTrying = true;
                    while (keepTrying)
                    {
                        var responseGet = await httpClient.GetAsync("Sensor");
                        string resp = await responseGet.Content.ReadAsStringAsync();
                        switch (resp)
                        {
                            case "0":
                                Console.WriteLine("Trying");
                                break;
                            case "1":
                                Console.WriteLine("Done");
                                keepTrying = false;
                                break;
                            case "-1":
                                keepTrying = false;
                                Console.WriteLine("There was a a problem with the transmission.");
                                break;
                            case "-2":
                                keepTrying = false;
                                Console.WriteLine("There was a service error.");
                                break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Not OK: {0} {1}", response.StatusCode, response.ReasonPhrase);
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public  class AppSettings{
            public string Url { get; set; }
            public string Api { get; set; }

            public int NumToSend { get; set; }
        }

    }

}
