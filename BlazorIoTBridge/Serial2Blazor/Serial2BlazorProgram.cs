using System;
using System.IO.Ports;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using BlazorIoTBridge.Shared;
using Newtonsoft.Json;
using System.Dynamic;


using static System.Net.Http.HttpClient;
using System.Threading;
using System.Configuration;

//rEF: https://stackoverflow.com/questions/65110479/how-to-get-values-from-appsettings-json-in-a-console-application-using-net-core
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using System.Reflection;
// NuGet packages:
// Microsoft.Extensions.Configuration.Binder
// Microsoft.Extensions.Configuration.Jso

using SimulatedDeviceWithDefaultCommandOnly;
using System.ComponentModel;

namespace Serial2Blazor_app
{
    /// <summary>
    /// Get Csv list of commands from device and forward to BlazorIoTBridge.
    /// Or if simulating forward list from settings. See CommandsIfIsSimDevice
    /// Get telemetry from device or serial port, or simulate telemetry.
    /// Forward telemetry to IoT Hub direct from here (method in Client4Commands)  or via BlazorIoTBridge.
    /// - Poll BlazorIoTBridge for commands direct from there.
    /// - SetMethodDefaultHandler (in Client4Commands) as handler of Commands direct from Hub
    /// For both of 2 above either send commands to device serially, or if simulating simulate here.
    /// </summary>
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
        static string ACK = "{\"Action\":\"ACK\"}";
        static string InitialMessage = "* Begin";
        static bool IsFirstSerialRead = true;
        static string ReadCommandsUrl = "";
        static string SensorApi = "sensor";
        static bool IsRealDevice = false;
        static string CommandsIfIsSimDevice = "GETCOMANDS,RATE";
        static int defaultTimeout = 5000;
        static string IOTHUB_DEVICE_CONN_STRING = "";
        static bool fwdTelemetrythruBlazorSvr = true;






        static void Main(string[] args)
        {
            //[1] Load default settings
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false);
            IConfiguration configuration = builder.Build();
            Settings settings = configuration.GetSection("AppSettings").Get<Settings>();


            // [2] Get user location of (saved) appsettings.json from above and load if it exists.
            string userDir = settings.UserDir;
            if (Directory.Exists(userDir))
            {
                if (File.Exists(Path.Combine(userDir, "appsettings.json")))
                {
                    var appsettings  = JsonConvert.DeserializeObject<App_Settings>(File.ReadAllText(@"c:\temp\\appsettings.json"));
                    settings = appsettings.AppSettings;                
                }
            }

            // Start without prompting if COM seetings are OK
            auto = settings.Auto;

            // Is device an actual Arduino or simuloated here?
            IsRealDevice = settings.IsRealDevice;

            // Forward telemetry via Blazor Svc or direct to hub from here
            fwdTelemetrythruBlazorSvr = settings.FwdTelemetrythruBlazorSvr;

            //Blazor Svc
            _host = $@"{settings.Host}:{settings.Port}/";

            delay = settings.Delay_Secs;

            // COM Settings
            comport = settings.ComPort;
            baudrate = settings.BaudRate;

            InitialMessage = settings.InitialMessage;

            // Create json packet for acks back to device
            if (settings.ACK.Length > 0)
                ACK = "{\"Action\":\"" + settings.ACK + "\"}";

            ReadCommandsUrl = (settings.ReadCommandsController).Replace("Controller", "");
            SensorApi = (settings.SensorController).Replace("Controller", "");

            CommandsIfIsSimDevice = settings.CommandsIfIsSimDevice;
            defaultTimeout = settings.defaultTimeout;

            // Hub device connection string
            IOTHUB_DEVICE_CONN_STRING = settings.IOTHUB_DEVICE_CONN_STRING;


            Console.WriteLine("> \tHello IoT Nerd!");
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();
            var lst = new List<string>(ports);

            // Skip COM settings input if Auto mode, COM port is valid and is real device.
            if (((!auto) || (!lst.Contains(comport))) && (IsRealDevice))
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
                bool contin;
                do
                {
                    Console.WriteLine("If USB cable is NOT inserted, insert now.");
                    if (comport == "")
                        Console.Write($"> \tCOM Port/Port no: (No default) ");
                    else
                        Console.Write($"> \tCOM Port/Port no: (Default {comport}) ");

                    port = Console.ReadLine();
                    ports = SerialPort.GetPortNames();
                    lst = new List<string>(ports);
                    //Alllow for just the port number
                    if (port.Length == 1)
                        port = "COM" + port;
                    // Can just press return for defaults.
                    if ((string.IsNullOrEmpty(port)) && (comport != ""))
                        contin = false;
                    else
                        contin = !lst.Contains(port);
                } while (contin);

                if (!string.IsNullOrEmpty(port))
                    comport = port;
                settings.ComPort = comport;
                Console.Write($"> \tbaudrate: (Default: {baudrate}) ");
                string newbaudrate = Console.ReadLine();
                if (!string.IsNullOrEmpty(newbaudrate))
                {
                    if (!int.TryParse(newbaudrate, out baudrate))
                        baudrate = settings.BaudRate;
                }
                settings.BaudRate = baudrate;

                Console.Write($"> \tTime (in sec) between reads: (Default:{delay}) ");
                string secs = Console.ReadLine();
                if (!int.TryParse(secs, out delay))
                {
                    delay = settings.Delay_Secs;
                }
                settings.Delay_Secs = delay;

                Console.Write($@"> Blazor Server URL:Port: (Default:{_host} ) ");
                string newhost = Console.ReadLine();
                if (!string.IsNullOrEmpty(newhost))
                {
                    _host = newhost;
                    string[] parts = _host.Split(":");
                    if (parts.Length == 2)
                    {
                        if (uint.TryParse(parts[1], out uint p))
                        {
                            settings.Host = parts[0];
                            settings.Port = p;
                        }
                    }
                }
                Console.Write($"Save user settings to {userDir}? [Y/N]");
                string yesno = Console.ReadLine();
                if (!string.IsNullOrEmpty(yesno))
                {
                    if (yesno.ToUpper()[0] == 'Y')
                    {
                        if (Directory.Exists(userDir))
                        {
                            App_Settings appsettings = new App_Settings { AppSettings = settings };
                            File.WriteAllText(Path.Combine(userDir, "appsettings.json"), JsonConvert.SerializeObject(appsettings));// This overwrites
                        }
                    }
                }
            }

            if (IsRealDevice)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("> \tPlease remove and reinsert the USB cable NOW ... then ...");
                Console.ResetColor();
                Console.WriteLine("> \tNb: It needs to be in at start.");
            }
            Console.WriteLine();
            if (!fwdTelemetrythruBlazorSvr)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Telemetry send mode set to direct to hub.");
            }
            else 
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Telemetry send mode set to via BlazorIoTBridge.");
            }
            Console.ResetColor();
            Console.WriteLine();

            if (_host[_host.Length - 1] != '/')
                _host += "/";
            isFirstRead = true;
            // Create a new SerialPort on port COM7

            Console.Write("> \tPress [Enter] when web app is ready.");
            Console.ReadLine();
            if (IsRealDevice)
            {
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
            }

            bool ready = true;
            if (IsRealDevice)
            {
                ready = _serialPort.IsOpen;
            }

            if (ready)
            {
                if (IsRealDevice)
                    Console.WriteLine("> \tOpened the Serial Port {0}.", comport);
                else
                    Console.WriteLine("> \tRunning app as device simulator.");
                //if (_serialPort.BytesToRead != 0)
                //    _serialPort.DiscardInBuffer();

                // Console.Write("Press enter when Server is up.");
                //Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine("> \tUse Azure IoT Explorer to display Telemetry sent:");
                Console.WriteLine("> \thttps:////docs.microsoft.com//en-us//azure//iot-fundamentals//howto-use-iot-explorer");
                Console.WriteLine();
                bool cont = true;
                if (IsRealDevice)
                {
                    IsFirstSerialRead = true;

                }
                FirstRecv = true;

                while (cont)
                {
                    if (IsRealDevice)
                    {
                        Task t3 = Task.Run(() => Signal());
                        Task t4 = Task.Run(() => Read());
                        t3.Wait();
                        t4.Wait();
                    }
                    else
                    {
                        Task t5 = Task.Run(() => Signal());
                        Task t6 = Task.Run(() => SimSensor());
                        t5.Wait();
                        t6.Wait();
                    }
                }
                _serialPort.Close();
            }
            else
                Console.WriteLine("Serial port {0} failed to open.", comport);
        }


        /// <summary>
        /// Watch for Commands, sent from Hub via Blazor Svc
        /// Actuially just poll Blazor Svc for them.
        /// </summary>
        /// <returns></returns>
        public static async Task Signal()
        {
            while ((IsFirstSerialRead) && (!IsRealDevice))
            {
                Thread.Sleep(1000);
            }
            while (true)
            {
                if ((isFirstRead) && (!IsRealDevice))
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
                    Command _command = await client.GetFromJsonAsync<Command>(ReadCommandsUrl, null);
                    if (!IsRealDevice && isFirstRead)
                    {
                        isFirstRead = false;
                        await SendCommands($"{CommandsIfIsSimDevice}");
                    }
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
                                if (_command.Action.Length > "Send Telemetry".Length)
                                {
                                    if (_command.Action.Substring(0, "Send Telemetry".Length) == "Send Telemetry")
                                    {
                                        Console.WriteLine();
                                        if (_command.Action.ToLower().Contains("direct"))
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine("Telemetry send mode set to direct to hub.");
                                            fwdTelemetrythruBlazorSvr = false;
                                        }
                                        else if (_command.Action.ToLower().Contains("via"))
                                        {
                                            Console.ForegroundColor = ConsoleColor.Cyan;
                                            Console.WriteLine("Telemetry send mode set to via BlazorIoTBridge.");
                                            fwdTelemetrythruBlazorSvr = true;
                                        }
                                        Console.ResetColor();
                                        Console.WriteLine();
                                        continue;
                                    }
                                }
                                if (IsRealDevice)
                                {
                                    //Forward serially
                                    await DoCommand(_command.Action, (int)_command.Parameter);
                                }
                                else
                                {
                                    string[] cmds = CommandsIfIsSimDevice.Split(',');
                                    if ((new List<string>(cmds)).Contains(_command.Action))
                                    {
                                        if ((_command.Parameter != null) && (_command.Parameter != Sensor.iNull))
                                        {
                                            InvokeMethod imv = new InvokeMethod();
                                            imv.Invoke(_command.Action, _command.Parameter);
                                            Console.ForegroundColor = ConsoleColor.Green;
                                            Console.WriteLine($"Got command {_command.Action} with parameter {_command.Parameter}");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            InvokeMethod imv = new InvokeMethod();
                                            imv.Invoke(_command.Action, null);
                                            Console.ForegroundColor = ConsoleColor.Blue;
                                            Console.WriteLine($"Got command {_command.Action}");
                                            Console.ResetColor();
                                        }
                                    }
                                   else
                                    {
                                        Console.WriteLine($"Got unrecognised command {_command.Action}");

                                    }
                                }
                            }
                        }
                    }

                }
                catch (TimeoutException )
                {
                    //Ignore these
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("{0}", _host);
                }
                Thread.Sleep(3333);
            }

        }

        /// <summary>
        /// Action Commands if in simulate device mode
        /// </summary>
        public class InvokeMethod
        {
            public void Invoke(string name, int? parameter)
            {
                Type thisType = this.GetType();
                MethodInfo theMethod = thisType.GetMethod(name);
                if (parameter != null)
                    theMethod.Invoke(this, new object[] { parameter });
                else
                    theMethod.Invoke(this, null);
            }

            public void Invoke(string name)
            {
                Type thisType = this.GetType();
                MethodInfo theMethod = thisType.GetMethod(name);
                theMethod.Invoke(this, null);
            }

            public void RATE(int param)
            {
                if (param != Sensor.iNull)
                    Timeout = (int)param;
            }

            public void START()
            {
                IsRunning = true;
                Timeout = defaultTimeout;
            }

            public void STOP()
            {
                IsRunning = false;
            }

            public void FAST()
            {
                Timeout /= 2;
                if (Timeout < 1000)
                    Timeout = 1000;
            }

            public void SLOW()
            {
                Timeout *= 2;
                if (Timeout > 10000)
                    Timeout = 10000;
            }
            public void PAUSE()
            {
                IsRunning = false;

            }
        }
            
        static string MonitorTimeout = "MonitorTimeout";
        static int _Timeout = 5000;
        static int Timeout
        {
            get {
                int val;
                Monitor.Enter(MonitorTimeout);
                val = _Timeout;
                Monitor.Exit(MonitorTimeout);
                return val;
            }
            set {
                Monitor.Enter(MonitorTimeout);
                _Timeout= value;
                Monitor.Exit(MonitorTimeout);
            }
        }

        static string MonitorIsRunning = "MonitorIsRunning";
        static bool _IsRunning = true;
        static bool  IsRunning
        {
            get
            {
                bool val;
                Monitor.Enter(MonitorIsRunning);
                val = _IsRunning;
                Monitor.Exit(MonitorIsRunning);
                return val;
            }
            set
            {
                Monitor.Enter(MonitorIsRunning);
                _IsRunning = value;
                Monitor.Exit(MonitorIsRunning);
            }
        }

        public static async Task SimSensor()
        {
            Timeout = defaultTimeout;
            Thread.Sleep(1000);

            while (true)
            {
                Random rnd = new Random();
                int sensorNo = 0;
                long ts = DateTime.Now.Ticks;
                while (IsRunning)
                {
                    try
                    {
                        Sensor sensor = new Sensor
                        {
                            Id = sensorNo.ToString(), // new Guid().ToString(),
                            SensorType = (SensorType)sensorNo,
                            TimeStamp = DateTime.Now.Ticks
                        };

                        sensorNo++;
                        if (sensorNo > 7)
                            sensorNo = 0;

                        if (sensorNo < 4)
                        {
                            sensor.Value = (((float)rnd.Next(10000)) / 100.0);
                        }
                        else if (sensorNo < 5)
                        {
                            sensor.Values = new List<double>();
                            sensor.Values.Add(((float)rnd.Next(10000)) / 100.0);
                            sensor.Values.Add(((float)rnd.Next(10000)) / 100.0);
                            sensor.Values.Add(((float)rnd.Next(10000)) / 100.0);
                        }
                        else if (sensorNo < 6)
                        {
                            sensor.environ = new BlazorIoTBridge.Shared.Environ();
                            sensor.environ.Temperature = ((float)rnd.Next(6000)) / 100.0;
                            sensor.environ.Humidity = ((float)rnd.Next(10000)) / 100.0;
                            sensor.environ.Pressure = ((float)rnd.Next(100010)) / 100.0;
                        }
                        else
                        {
                            int rndv = rnd.Next(9);
                            bool bstate = true;
                            if (rndv < 5)
                                bstate = false;
                            sensor.State = bstate;
                        }

                        string sensorJson = JsonConvert.SerializeObject(sensor, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        Console.WriteLine(sensorJson);

                        await SendSensor(sensorJson);
                    }
                    catch (TimeoutException)
                    {
                    }
                    int timeout = Timeout;
                    timeout *= 10000;
                    long ts2 = DateTime.Now.Ticks;
                    while (ts2 < ts + timeout)
                    {
                        Thread.Sleep(100);
                        ts2 = DateTime.Now.Ticks;
                    }
                    ts = ts2;
                }
            }
        }
        
        
        public static async Task Read()
        {

            Monitor.Enter(_serialPort);
            _serialPort.WriteLine("RESET");
            // Wait for it
            Thread.Sleep(3000);
            // Device expects an initial char to complete SetUp()
            _serialPort.Write("*");
            Monitor.Exit(_serialPort);
            while (true)
            {

                try
                {
                    string sensor = "";

                    sensor = _serialPort.ReadLine();
                    sensor = sensor.Trim();

                    if (!string.IsNullOrEmpty(sensor))
                    {
                        Console.WriteLine("From device:  " + sensor);
                        if (IsFirstSerialRead)
                        {
                            IsFirstSerialRead = false;
                            continue;
                        }
                        else if (sensor[0] == '~')
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        else if (sensor[0] == '#')
                        {
                            string comandsCsv = sensor.Substring(1).Trim();
                            await SendCommands(comandsCsv);
                        }
                        else if (sensor[0] == '*')
                        {
                            //Ignore message(comments) from device
                            continue;
                        }
                        else if (sensor[0] == '{')
                        {
                            await SendSensor(sensor);
                            // Make sure not sending a command when sending an ACK
                            Monitor.Enter(_serialPort);
                            _serialPort.Write(ACK);
                            Monitor.Exit(_serialPort);
                        }

                        else
                        {
                            Console.WriteLine("> \tInvalid Sensor Data: " + sensor);
                        }
                    }
                }
                catch (TimeoutException) {
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Read() - " + ex.Message);
                }
            }
        }


        private static bool FirstRecv = true;
        public static async Task SendSensor(string sensorJson)
        {
            Sensor sensor = JsonConvert.DeserializeObject<Sensor>(sensorJson);

            try
            {
                Console.Write("Sending ... ");
                DateTime now = DateTime.Now;
                if (fwdTelemetrythruBlazorSvr)
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.BaseAddress = new Uri(_host);

                    // Note no "api/Sensor" but just "Sensor" in next LOC!:
                    var response = await httpClient.PostAsJsonAsync<Sensor>(SensorApi, sensor, null);
                    Console.WriteLine("Commands Sent OK");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Sent OK");
                        bool keepTrying = true;
                        while (keepTrying)
                        {
                            var responseGet = await httpClient.GetAsync(SensorApi);
                            string resp = await responseGet.Content.ReadAsStringAsync();
                            switch (resp)
                            {
                                case "0":
                                    Console.Write("Trying");
                                    break;
                                case "1":
                                    Console.Write("Done");
                                    if (FirstRecv)
                                    {
                                        FirstRecv = false;
                                        //await SendCommands($"{CommandsIfIsSimDevice}");
                                    }
                                    keepTrying = false;
                                    break;
                                case "-1":
                                    keepTrying = false;
                                    Console.Write("There was a a problem with the transmission.");
                                    break;
                                case "-2":
                                    keepTrying = false;
                                    Console.Write("There was a service error.");
                                    break;
                            }
                        }
                        TimeSpan ts = DateTime.Now.Subtract(now);
                        Console.WriteLine(" - {0} seconds", ts.TotalMilliseconds / 1000);
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Not OK: {0} {1}", response.StatusCode, response.ReasonPhrase);
                    }
                }
                else
                {
                    // Forwading direc to the hub.
                    bool res = await Client4Commands.StartSendDeviceToCloudMessageAsync(sensor);
                    if (res)
                        Console.Write(" Sent: ");
                    else
                        Console.WriteLine(" Not sent.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendSensor(): " + ex.Message);
            }
        }



        public static async Task SendCommands(string commands)
        {
            using var client = new System.Net.Http.HttpClient();

            var content = new StringContent(commands);
            try
            {
                client.BaseAddress = new Uri(_host);
                // Note no "api/Sensor" but just "Sensor" in next LOC!:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\tSending Commands ... ");
                List<string> cmds = new List<string>(commands.Split(','));
                var response = await client.PostAsJsonAsync<List<string>>("CommandsViaHub", cmds, null);
                Console.Write(" Sent: ");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Commands Sent OK");
                    string[] args = new string[] { IOTHUB_DEVICE_CONN_STRING, commands };
                    // Start monitoring for commands
                    Client4Commands.Main(args, DoCommand).GetAwaiter();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine();
                    Console.WriteLine("Not OK: {0} {1}", response.StatusCode, response.ReasonPhrase);
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendCommands(): " + ex.Message);
                Console.ResetColor();
            }

        }

        static async Task DoCommand(string action, int value)
        {
            Command _command = new Command{ Action = action, Invoke = false, Parameter = value };
            string cmd = JsonConvert.SerializeObject(_command);

            //Forward serially

            Monitor.Enter(_serialPort);
            _serialPort.WriteLine(cmd);
            Monitor.Exit(_serialPort);


            if ((_command.Parameter != null) && (_command.Parameter != (int)Sensor.iNull))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n> \tCommand sent: {0} Parameter: {1}.\n", _command.Action, _command.Parameter);
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("\n> \tCommand sent: {0}  No parameter.\n", _command.Action);
                Console.ForegroundColor = ConsoleColor.White;
            }
            await Task.Delay(1);

        }
    }

    /// <summary>
    /// Loaded from appsettings.json from app location
    /// i.e. Default settings. 
    /// Can directly edit before running app.
    /// </summary>
    public class Settings
    {
        public string UserDir { get; set; } 

        public bool FwdTelemetrythruBlazorSvr { get; set; }
        public bool Auto { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        public int Delay_Secs { get; set; }
        public string Host { get; set; }
        public uint Port { get; set; }
        public int WriteTimeout { get; set; }
        public int ReadTimeout { get; set; }
        public string ACK { get; set; }
        public string InitialMessage { get; set; }

        public string ReadCommandsController { get; set; }
        public string SensorController { get; set; }

        public bool IsRealDevice { get; set; }

        public string CommandsIfIsSimDevice { get; set; }

        public int defaultTimeout { get; set; }

        public string IOTHUB_DEVICE_CONN_STRING { get; set; }
    }


    /// <summary>
    /// Loaded from appsettings.json in user area (eg c:\temp) 
    /// Can be saved after making COM etc selecxtions at start.
    /// </summary>
    public class App_Settings
    {
        public Settings AppSettings { get; set; }
    }
  
}
