using System;
using System.IO.Ports;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using BlazorSensorAppNet5.Shared;
using Newtonsoft.Json;


using static System.Net.Http.HttpClient;
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
        static char ACK = '#';
        static string InitialMessage = "* Begin";
        static bool IsFirstSerialRead = true;
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
            InitialMessage = settings.InitialMessage;
            if (settings.ACK.Length > 0)
                ACK = settings.ACK[0];

            Console.WriteLine("> \tHello IoT Nerd!");
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();
            var lst = new List<string>(ports);
            if ((!auto) || (!lst.Contains(comport)))
            {
                if (!lst.Contains(comport))
                    comport = "";
                Console.WriteLine("> \tThe following serial ports were found:");                                                                                          
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
                        Console.Write($"> \tPort no: (No default) ");
                    else
                        Console.Write($"> \tPort no: (Default {comport}) ");
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

                Console.Write($"> \tbaudrate: (Default: {baudrate}) ");
                string newbaudrate = Console.ReadLine();
                if (!string.IsNullOrEmpty(newbaudrate))
                {
                    if (!int.TryParse(newbaudrate, out baudrate))
                        baudrate = settings.BaudRate;
                }

                Console.Write($"> \tTime (in sec) between reads: (Default:{delay}) ");
                string secs = Console.ReadLine();
                if (!int.TryParse(secs, out delay))
                {
                    delay = settings.Delay_Secs;
                }

                Console.Write($@"> Blazor Server URL:Port: (Default:{_host} ) ");
                string newhost = Console.ReadLine();
                if (!string.IsNullOrEmpty(newhost))
                    _host = newhost;
                
            }

            if (_host[_host.Length - 1] != '/')
                _host += "/";
            isFirstRead = true;
            // Create a new SerialPort on port COM7
            Console.Write("> \tPress enter when ready.");
            Console.ReadLine();
            _serialPort = new SerialPort(comport, baudrate);
            // Set the read/write timeouts
            _serialPort.ReadTimeout = settings.ReadTimeout;
            _serialPort.WriteTimeout = settings.WriteTimeout;
            while (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                }
                catch (Exception)
                {
                    Console.WriteLine("> \tSerial Port {0} didn't open. Please check..", comport);
                    Thread.Sleep(1000);
                }
            }
            
            if (_serialPort.IsOpen)
            {
                Console.WriteLine("> \tOpened the Serial Port {0}.", comport);
                //if (_serialPort.BytesToRead != 0)
                //    _serialPort.DiscardInBuffer();

                   // Console.Write("Press enter when Server is up.");
                //Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine("> \tUse Azure IoT Explorer to display Telemetry sent:");
                Console.WriteLine(@"> \thttps://docs.microsoft.com/en-us/azure/iot-fundamentals/howto-use-iot-explorer");
                Console.WriteLine();
                bool cont = true;
                IsFirstSerialRead = true;
                _serialPort.WriteLine("RESET");
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
            while(IsFirstSerialRead)
            {
                Thread.Sleep(1000);
            }
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
                    string cd = _command.Action;
                    if (cd != null)
                    {
                        cmd = cmd.Trim(); //.ToUpper();
                        if (!string.IsNullOrEmpty(cmd))
                        {
                            // Ignore blank commands.                       
                            cd = cd.Trim();
                            if (cd != "")
                            {
                                Monitor.Enter(_serialPort);
                                _serialPort.WriteLine(cmd);
                                if ((_command.Parameter != null) && (_command.Parameter != 0))
                                    Console.WriteLine("> \tCommand sent: {0} Parameter: {1}", _command.Action, _command.Parameter);
                                else
                                    Console.WriteLine("> \tCommand sent: {0}  No parameter.", _command.Action);
                                Monitor.Exit(_serialPort);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("{0}", _host);
                }
                Thread.Sleep(3333);
            }

        }


        public static async Task Read()
        {
            while(true)
            {
                Monitor.Enter(_serialPort);
                _serialPort.Write("*");
                Monitor.Exit(_serialPort);
                Thread.Sleep(1000);
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
                    sensor = sensor.Trim();

                    if (!string.IsNullOrEmpty(sensor))
                    {
                        Console.WriteLine(sensor);
                        if (IsFirstSerialRead)
                        {
                            IsFirstSerialRead = false;
                            continue;
                        }
                        else if (sensor[0]=='*')
                        {
                            //Ignore message from device
                            continue;
                        }
                        else if (sensor[0] == '{')
                        {
                            await SendSensor(sensor);
                            // Make sure not sending a command when sending an ACK
                            Monitor.Enter(_serialPort);

                            //_serialPort.Write(new char[] { ACK }, 0, 1);
                            Monitor.Exit(_serialPort);
                        }
                        else Console.WriteLine("> \tInvalid Sensor Data.");
                    }
                    //An empty Serial.println(""); gets to here so is OK
                    //else Console.WriteLine("> Invalid Sensor Data.");
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
                Console.Write("Sending ... ");
                var response = await client.PostAsJsonAsync<Sensor>("Sensor", sensor,null);
                Console.Write(" Sent: ");
//
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Sent OK");
                }
                else
                {
                    Console.WriteLine();
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
        public string ACK { get; set; }
        public string InitialMessage { get; set; }


    }
}
