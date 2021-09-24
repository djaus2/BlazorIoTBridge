using System;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BlazorSensorAppNet5.Shared;
using Newtonsoft.Json;


using static System.Net.Http.HttpClient;
using System.Net.Http.Json;
using System.Threading;
using System.Configuration;

//rEF: https://stackoverflow.com/questions/65110479/how-to-get-values-from-appsettings-json-in-a-console-application-using-net-core
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;
// NuGet packages:
// Microsoft.Extensions.Configuration.Binder
// Microsoft.Extensions.Configuration.Jso

namespace SerialBlazor
{
    class Program
    {
        static IConfigurationRoot configuration { get; set; }

        static SerialPort _serialPort;

        static string _host = "https://localhost:44318/";
        static int delay = 5;
        static bool isFirstRead = true;
        static string comport = "COM4";
        static int baudrate = 9600;
        static bool auto = false;
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false);

            IConfiguration configuration = builder.Build();
            Settings settings = configuration.GetSection("AppSettings").Get<Settings>();

            _host = $@"{settings.Host}:{settings.Port}/";
            delay = settings.Delay_Secs;
            comport = settings.ComPort;
            baudrate = settings.BaudRate;
            auto = settings.Auto;

            Console.WriteLine("Hello IoT Nerd!");
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();
            var lst = new List<string>(ports);
            if ((!auto) || (!lst.Contains(comport)))
            {
                if (!lst.Contains(comport))
                    comport = "";
                Console.WriteLine("The following serial ports were found:");                                                                                          
                // Display each port name to the console.
                foreach (string _port in ports)
                {
                    Console.WriteLine(_port);
                }

                string port = "";
                bool cont;
                do
                {
                    if (comport =="")
                        Console.Write($"Port no: (No default) ");
                    else
                        Console.Write($"Port no: (Default {comport}) ");
                    port = Console.ReadLine();
                    cont = !lst.Contains(port);
                    // Can just press return for defaults.
                    if ( (string.IsNullOrEmpty(port)) && (comport != ""))
                        cont = false;
                } while (cont);
                if (!string.IsNullOrEmpty(port)) 
                    comport = port;

                Console.Write($"baudrate: (Default: {baudrate}) ");
                string newbaudrate = Console.ReadLine();
                if (!string.IsNullOrEmpty(newbaudrate))
                {
                    if (!int.TryParse(newbaudrate, out baudrate))
                        baudrate = settings.BaudRate;
                }

                Console.Write($"Time (in sec) between reads: (Default:{delay}) ");
                string secs = Console.ReadLine();
                if (!int.TryParse(secs, out delay))
                {
                    delay = settings.Delay_Secs;
                }

                Console.Write($@"Blazor Server URL:Port: (Default:{_host} ) ");
                string newhost = Console.ReadLine();
                if (!string.IsNullOrEmpty(newhost))
                    _host = newhost;
                
            }

            if (_host[_host.Length - 1] != '/')
                _host += "/";
            isFirstRead = true;
            // Create a new SerialPort on port COM7
            _serialPort = new SerialPort(comport, baudrate);
            // Set the read/write timeouts
            _serialPort.ReadTimeout = settings.ReadTimeout;
            _serialPort.WriteTimeout = settings.WriteTimeout;
            _serialPort.Open();
            if (_serialPort.IsOpen)
            {

                Console.WriteLine("Opened the port.");

                Console.Write("Press enter when Server is up.");
                Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine("Use Azure IoT Explorer to display Telemetry sent:");
                Console.WriteLine(@"https://docs.microsoft.com/en-us/azure/iot-fundamentals/howto-use-iot-explorer");
                Console.WriteLine();
                bool cont = true;
                while (cont)
                {
                    Signal();
                    Read().GetAwaiter();
                }
                _serialPort.Close();
            }
            else
                Console.WriteLine("Serial port {0} failed to open.", comport);      
        }

        public static void Signal()
        {
            if(isFirstRead)
            {
                isFirstRead = false;
                if (auto)
                    Thread.Sleep(30000);
                else
                    Thread.Sleep(3000);
            }
            else
             Thread.Sleep(delay*1000);
            _serialPort.WriteLine("READ");
        }

        public static async Task Read()
        {
            try
            {
                string sensor = _serialPort.ReadLine();
                Console.WriteLine(sensor);
                if (sensor[0] =='{')
                {
                    await SendSensor(sensor);
                }
            }
            catch (TimeoutException) { }
        }

        static bool busy = false;
        public static async Task  SendSensor(string sensorJson)
        {
            if (busy)
                return;
            busy = true;
            Sensor sensor = JsonConvert.DeserializeObject<Sensor>(sensorJson);
            using var client = new System.Net.Http.HttpClient();

            var dataAsString = JsonConvert.SerializeObject(sensor);
            var content = new StringContent(dataAsString);
            try
            {
                client.BaseAddress = new Uri(_host);
                // Note no "api/Sensor" but just "Sensor" in next LOC!:
                var response = await client.PostAsJsonAsync<Sensor>("Sensor", sensor,null);
//
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Sent OK");
                }
                else
                {
                    Console.WriteLine("Not OK: {0} {1}", response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            busy = false;        
        }
    }

    public class Settings
    {
        public bool Auto {get; set;}
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        public int Delay_Secs { get; set; }
        public string Host { get; set; }
        public uint Port { get; set; }
        public int WriteTimeout { get; set; }
        public int ReadTimeout {get; set;}


    }
}
