using System;
using System.IO.Ports;
using System.Net.Http;
using System.Net.Http.Json;
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
        static char TERMINATOR = '#';
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
            if (settings.TerminatorChar.Length > 0)
                TERMINATOR = settings.TerminatorChar[0];

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
                    //Alllow for just the port number
                    if (port.Length == 1)
                        port = "COM" + port;
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
                //if (_serialPort.BytesToRead != 0)
                //    _serialPort.DiscardInBuffer();

                    Console.Write("Press enter when Server is up.");
                Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine("Use Azure IoT Explorer to display Telemetry sent:");
                Console.WriteLine(@"https://docs.microsoft.com/en-us/azure/iot-fundamentals/howto-use-iot-explorer");
                Console.WriteLine();
                bool cont = true;
                while (cont)
                {
                    Task t3 = Task.Run(() => Signal());
                    Task t4 = Task.Run(() => Read());
                    t3.Wait();
                    t4.Wait();
                }
                _serialPort.Close();
            }
            else
                Console.WriteLine("Serial port {0} failed to open.", comport);      
        }

        public static async Task Signal()
        {
            while (true)
            {
                if (isFirstRead)
                {
                    isFirstRead = false;
                    if (auto)
                        Thread.Sleep(1000);
                }
                //else
                //Thread.Sleep(delay*1000);
                string cmd = "";
                try
                {
                    //See if a new command has been sent
                    using var client = new System.Net.Http.HttpClient();
                    client.BaseAddress = new Uri(_host);
                    //var response = await client.GetAsync("Sensor");
                    //cmd = await response.Content.ReadAsStringAsync();
                    Command _command = await client.GetFromJsonAsync<Command>("Sensor", null);  //.GetJsonAsync<Command>("Sensor")
                    cmd = JsonConvert.SerializeObject(_command);
                    //cmd = _command.Action;
                    if (_command.Action != null)
                    {
                        cmd = cmd.Trim(); //.ToUpper();
                        if (!string.IsNullOrEmpty(cmd))
                        {
                            int len = "{ \"12345\":\"12345\",\"12345\":12345}".Length+2;
                            len = 40;
                            /*if ((cmd=="SETRATE") || (cmd == "SET_RATE") || (cmd == "SET RATE"))
                            {
                                cmd = _command.Parameter.ToString();
                            }*/
                            //Set cmd length to 5 characters
                            cmd = cmd.Replace(" ", "");
                            while (cmd.Length < len) // 5)
                                cmd += ' ';
                            cmd = cmd.Substring(0, len);
                            _serialPort.WriteLine(cmd);
                            if (_command.Parameter != null)
                                Console.WriteLine("Command sent: {0} Parameter: {1}", _command.Action, _command.Parameter);
                            else
                                Console.WriteLine("Command sent: {0}  No parameter.", _command.Action);

                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Thread.Sleep(3333);
            }

        }

        public static async Task Read()
        {
            while(true)
            {
                try
                {
                    string sensor = "";
                    //if (_serialPort.BytesToRead > 0)
                    //{
                    //    while (_serialPort.BytesToRead > 0)
                    //    {
                    //        sensor += _serialPort.ReadExisting();
                    //    }
                    sensor = _serialPort.ReadLine();
                        Console.WriteLine(sensor);
                        if (!string.IsNullOrEmpty(sensor))
                        {
                            if (sensor[0] == '{')
                            {
                                await SendSensor(sensor);
                            }
                        }
                    
                }
                catch (TimeoutException) { }
            }
        }

        public static async Task  SendSensor(string sensorJson)
        {
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
        public string TerminatorChar { get; set; }


    }
}
