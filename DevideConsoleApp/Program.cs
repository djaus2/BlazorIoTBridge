using BlazorIoTBridge.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

using CommandLine;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.ComponentModel;
using SimulatedDeviceWithDefaultCommandOnly;


namespace DevideConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration configuration = builder.Build();
            Settings settings = configuration.GetSection("AppSettings").Get<Settings>();
            await Client4Commands.Main(new string[0]);

            bool res = true;
            Console.WriteLine("Please enter delay in sec when the web app is up.");
            string num = Console.ReadLine();
            int rate = 5;
            if (!int.TryParse(num, out rate))
                rate = 5;
            Console.WriteLine("Press cntrl-C to terminate");
            for (int i=0;i<1000;i++)
            {
                Sensor sensor = new Sensor { TimeStamp = DateTime.Now.Ticks, Id = i.ToString(), No = i, SensorType = SensorType.temperature, Value = 23.45 };
                res = await Client4Commands.StartSendDeviceToCloudMessageAsync(sensor);
                Console.WriteLine("{0} {1}", i, res);
                Thread.Sleep(rate*1000);
            }
        }
    }
}
