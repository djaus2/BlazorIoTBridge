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
    /// At start, Get Csv list of commands from device and forward to BlazorIoTBridge.
    /// Or if simulating forward list from settings. See CommandsIfIsSimDevice
    /// Get telemetry from device or serial port, or simulate telemetry.
    /// Forward telemetry to IoT Hub direct from here (method in Client4Commands)  or via BlazorIoTBridge.
    /// - Poll BlazorIoTBridge for commands direct from there. 
    ///   These are commands sent directly from the bridge only, not via the hub.
    /// - SetMethodDefaultHandler (in Client4Commands) as handler of Commands direct from Hub. 
    ///   These are commands fwded from the bridge to the hub or from Azure IoT Explorer to the hub.
    /// For both of 2 above either send commands to device serially, or if simulating simulate, run here.
    /// </summary>
    class Program
    {
        static IConfigurationRoot configuration { get; set; }
        static SerialPort _serialPort;

        static string _host = "https://localhost:44318/";
        //static int delay = 5;
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

        static  string IdGuid = "";
        static Guid GuidId = new Guid(); // Is in appsettings

        static Info info;
        static int PauseSignalAfterCheck4Command = 3333;




        static void Main(string[] args)
        {



            //[1] Load default settings
            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false);
            IConfiguration configuration = builder.Build();
            Settings settings = configuration.GetSection("AppSettings").Get<Settings>();

            //Blazor Svc
            _host = $@"{settings.Host}:{settings.Port}/";

            IdGuid = settings.Id;
            string infoController = (settings.InfoController).Replace("Controller","");

            GuidId = new Guid(IdGuid);
            using var client = new System.Net.Http.HttpClient();
            client.BaseAddress = new Uri(_host);
            IsRealDevice = settings.IsRealDevice;
            if (IsRealDevice)
            {
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" \tPlease remove and reinsert the USB cable NOW ... then wait for settings.");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" \tNb: It needs to be in at start.");
            }
            Console.WriteLine("");

            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.BackgroundColor = ConsoleColor.Yellow;
            //Console.Write("\tPlease press [Enter] when the Blazor IoT Bridge web app is up. ");
            //Console.ResetColor();
            //Console.ReadLine();
            bool found = false;
            int count = 0;
            do
            {
                Thread.Sleep(1000);
                try
                {
                    info = client.GetFromJsonAsync<Info>($"{infoController }/{IdGuid}", null).GetAwaiter().GetResult();
                    if (info == null)
                    {
                        found = false;
                    }
                    else
                        found = true;
                }
                catch (Exception)
                {
                    found = false;
                }
                if (!found)
                {
                    if (count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Waiting for settings from the Azure IoT Bridge: ");
                        Console.ResetColor();
                    }
                    else
                    {
                        if (count % 20 == 0)
                            Console.WriteLine();
                        if (count % 2 == 0)
                            Console.Write('/');
                        else
                            Console.Write('\\');
                    }
                    count++;
                }
            } while (!found);
            if (!found)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Device not registered.");
                //Console.ForegroundColor = ConsoleColor.DarkGreen;
                //Console.Write("Press enter to exit this app.");
                //Console.ResetColor();
                //Console.ReadLine();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Got settings");
                Console.ReadLine();
                // [2] Get user location of (saved) appsettings.json from above and load if it exists.
                string userDir = settings.UserDir;
                if (Directory.Exists(userDir))
                {
                    if (File.Exists(Path.Combine(userDir, "appsettings.json")))
                    {
                        var appsettings = JsonConvert.DeserializeObject<App_Settings>(File.ReadAllText(@"c:\temp\\appsettings.json"));
                        settings = appsettings.AppSettings;
                    }
                }

                // Start without prompting if COM seetings are OK
                auto = settings.Auto;

                // Is device an actual Arduino or simuloated here?
                IsRealDevice = settings.IsRealDevice;

                // Forward telemetry via Blazor Svc or direct to hub from here
                fwdTelemetrythruBlazorSvr = info.FwdTelemetrythruBlazorSvr;



                //delay = settings.Delay_Secs;

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
                IOTHUB_DEVICE_CONN_STRING = info.IOTHUB_DEVICE_CONN_STRING;


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

                    //Console.Write($"> \tTime (in sec) between reads: (Default:{delay}) ");
                    //string secs = Console.ReadLine();
                    //if (!int.TryParse(secs, out delay))
                    //{
                    //    delay = settings.Delay_Secs;
                    //}
                    //settings.Delay_Secs = delay;

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
                    Command _command = await client.GetFromJsonAsync<Command>($"{ReadCommandsUrl}/{GuidId}", null);
                    if (!IsRealDevice && isFirstRead)
                    {
                        isFirstRead = false;
                        await SendCommands($"{CommandsIfIsSimDevice}");
                    }
                    cmd = JsonConvert.SerializeObject(_command);
                    string cd = _command.Action;
                    if (!string.IsNullOrEmpty(cd))
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
                Thread.Sleep(PauseSignalAfterCheck4Command);
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
            // Device expects an initial char to complete SetUp() and is Id
            string idMsg = $"* {{{IdGuid}}}";
            _serialPort.Write(idMsg);
            Monitor.Exit(_serialPort);
            Console.WriteLine("Device has been reset");
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
            sensor.TimeStamp = DateTime.Now.Ticks;

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
                    bool res = await Client4Commands.StartSendDeviceToCloudMessageAsync(sensor, info.IOTHUB_DEVICE_CONN_STRING);
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

        public static string CommandsCsv;

        public static async Task SendCommands(string commands)
        {
            CommandsCsv = commands;
            using var client = new System.Net.Http.HttpClient();

            var content = new StringContent(commands);
            try
            {
                client.BaseAddress = new Uri(_host);
                // Note no "api/Sensor" but just "Sensor" in next LOC!:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\tSending Commands ... ");
                List<string> cmds;
                if (string.IsNullOrEmpty(IdGuid))
                    cmds = new List<string>(commands.Split(','));
                else
                    // Stck the Guid as a command at start.
                    cmds = new List<string>((IdGuid+','+commands).Split(','));
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
            string[] cmds = CommandsCsv.Split(',');
            List<string> cmdsList = new List<string>(cmds);
            bool included = cmdsList.Contains(action);
            //The CSV list of commands has * prefix for commands that require a parameter.
            //Some coomands taht get to here have it removed, some don't
            if (!included)
            {
                if (cmdsList.Contains("*" + action))
                {
                    //action = "*" + action;
                    included = true;
                }
            }
            if (true)
            {
                //Arduino looks for no *
                if (action[0] == '*')
                {
                    action = action.Substring(1);
                }
                Command _command = new Command { Action = action, Invoke = false, Parameter = value };
                string cmd = JsonConvert.SerializeObject(_command);
                cmd = cmd.Replace("\"Id\":null,", "");

                //Forward serially

                Monitor.Enter(_serialPort);
                _serialPort.WriteLine(cmd);
                Monitor.Exit(_serialPort);


                if ((_command.Parameter != null) && (_command.Parameter != Sensor.iNull))
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
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n> \tInvalid Command: \"{0}\" sent.", action);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }
    }

    /// <summary>
    /// Loaded from appsettings.json from app location
    /// i.e. Default settings. 
    /// Can directly edit before running app.
    /// </summary>
    public class Settings
    {
        public string UserDir { get; set; }                 // Keep saved copy of this there.
        public string Id { get; set; }                      // A Unique Guid string for each device
        public bool Auto { get; set; }                      // Just use settings without prompts
        public string ComPort { get; set; }                 // The Arduino (USB) serial port
        public int BaudRate { get; set; }                   // The Arduino Serial BaudRate
        public string Host { get; set; }                    // Url for the Blazor Server
        public uint Port { get; set; }                      // Blazor Server Port
        public int WriteTimeout { get; set; }               // Serial Port Write TimeOut
        public int ReadTimeout { get; set; }                // Serial Port Read Timeout
        public string ACK { get; set; }                     // Message sent back to device upon reception of a message
        public string InitialMessage { get; set; }          // Displayed at start

        public string InfoController { get; set; }          // Will be "InfoCController "Info" gets used for Get/Post
        public string ReadCommandsController { get; set; }  // Will be "Commands2DeviceController" "Commands2Device" is used in Get/Post
        public string SensorController { get; set; }        // Will be "SensorController" "Sensor" is used for Get/Post

        public bool IsRealDevice { get; set; }              // If true then use Arduino serially. If false use simulation here

        public string CommandsIfIsSimDevice { get; set; }   // CSV list of commands from here. Note if real device then that sends the list

        public int defaultTimeout { get; set; }             // A initial period for sending Telemetry if simulating

        public int PauseSignalAfterCheck4Command { get; set; }  // The pause of the Signal thread after completeing a command check/action. 
                                                                // This thread polls for Direct Commands from the Svc
                                                                // Nb: The larger this the longer a command may take before actioned
                                                                // Shorter means its busier.
                                                                // No impact upon commands direct from the Hub                                                           
    }


    /// <summary>
    /// Loaded from appsettings.json in user area (eg c:\temp) 
    /// If not there then appsettings in app location used.
    /// Can be saved after making COM etc selections at start.
    /// </summary>
    public class App_Settings
    {
        public Settings AppSettings { get; set; }
    }
  
}
