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

namespace SerialBlazor
{
    class Program
    {
        static SerialPort _serialPort;
        const string defaultHost = @"https://localhost";
        const int defaultPort = 44318;
        static string _host = "https://localhost:44318/";
        static int delay = 5;
        static bool isFirstRead = true;
        static string comport = "COM4";
        static string baudrate = "9600";
        static void Main(string[] args)
        {
            _host = $@"{defaultHost}:{defaultPort}/";
            Console.WriteLine("Hello World!");
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();
            Console.WriteLine("The following serial ports were found:");             //
            // Display each port name to the console.
            foreach(string _port in ports)
            {                
                Console.WriteLine(_port);             
            }
            Console.Write("Port no: (Default COM4, x to use all defaults) ");
            string port = Console.ReadLine();
            if (port.ToLower() != "x")
            {
                if (!string.IsNullOrEmpty(port))
                    comport = port;
                Console.Write("baudrate: (Default 9600) ");
                string newbaudrate = Console.ReadLine();
                if (!string.IsNullOrEmpty(newbaudrate))
                    baudrate = newbaudrate;
                Console.Write("Time (in sec) between reads: (Default 5) ");
                string secs = Console.ReadLine();
                if (!int.TryParse(secs, out delay))
                {
                    delay = 5;
                }
                Console.Write($@"Blazor Server URL:Port: (Default {_host} ) ");
                string newhost = Console.ReadLine();
                if (!string.IsNullOrEmpty(newhost))
                    _host = newhost;
            }
            else

            if (_host[_host.Length - 1] != '/')
                _host += "/";
            isFirstRead = true;
            // Create a new SerialPort on port COM7
            _serialPort = new SerialPort(comport, int.Parse(baudrate));
            // Set the read/write timeouts
            _serialPort.ReadTimeout = 10000;
            _serialPort.WriteTimeout = 10000;
            _serialPort.Open();
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
            Console.ReadLine();        
        }

        public static void Signal()
        {
            if(isFirstRead)
            {
                isFirstRead = false;
                Thread.Sleep(1000);
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
}
