using BlazorSensorAppNet5.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Concurrent;
using System.Dynamic;
using Newtonsoft.Json.Converters;

namespace BlazorSensorAppNet5.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommandController : ControllerBase
    {

        private readonly ILogger<SensorController> logger;

        private static SimulatedDeviceCS _SimulatedDeviceCS;

        private static int Count { get; set; } = 0;

        private static List<dynamic> D2CMessages { get; set; }
        private static List<string> D2CMessagesJson { get; set; }



        public CommandController(ILogger<SensorController> logger)
        {
            this.logger = logger;
            if (D2CMessages == null)
            {
                Count = 0;
                D2CMessages = new List<dynamic>();
                D2CMessagesJson = new List<string>();
            }
        }

        ~CommandController()
        {
            _SimulatedDeviceCS = null;
        }

        private static string block = "BLOCK";

        private static ConcurrentQueue<Command> Commands { get; set; } = null;

        public static Command Command
        {
            get
            {
                if (Commands == null)
                    return null;
                // Note Peeks, doesn't dequeue the value.
                Command val;
                //Monitor.Enter(block);
                if (Commands.Count() == 0)
                    val = null;
                else
                    while(!Commands.TryPeek(out val));
                //Monitor.Exit(block);
                return val;
            }
            set
            {
                if (Commands == null)
                    return;
                //Monitor.Enter(block);
                Commands.Enqueue(value);
                //Monitor.Exit(block);
            }
        }

        /// <summary>
        /// Same as Command=>Get but dequeues it.
        /// </summary>
        /// <returns></returns>
        public static Command GetCommand()
        {
            Command val;
            if (Commands == null)
                val = null;

            //Monitor.Enter(block);
            else if (Commands.Count() == 0)
                val = null;
            else
            {
                while ( !Commands.TryDequeue(out val));
            }
            //Monitor.Exit(block);
            return val;
        }

        public static void StartQ()
        {
            if (Commands == null)
                Commands = new ConcurrentQueue<Command>();
        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<string> messages = null;
            await Task.Delay(1);
            Monitor.Enter(D2CMessages);
            messages = D2CMessagesJson;
            Monitor.Exit(D2CMessages);
            return Ok(messages);
        }

        public static List<Sensor> PostLog { get; set; }

        public static long lastTS = 0;

        [HttpPost]
        public  IActionResult Post(object obj)
        {
            int state;
            Sensor sensor;

            string json = obj.ToString();
            try
            {
                dynamic message = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
                //System.Diagnostics.Debug.WriteLine($"Deserialized JSON into {message.GetType()}");
                //System.Diagnostics.Debug.WriteLine(message.TimeStamp.GetType());
                long ts = message.TimeStamp;
                if (ts > lastTS)
                {
                    lastTS = ts;
                    Monitor.Enter(D2CMessages);
                    D2CMessages.Add(message);
                    D2CMessagesJson.Add(json);
                    Monitor.Exit(D2CMessages);

                    /*System.Diagnostics.Debug.WriteLine("==Telemetry==");
                    foreach (var property in (IDictionary<String, Object>)message)
                    {
                        if (property.Value != null)
                        {
                            if (property.Value.GetType().Name == "List`1") // i.e. System.Collections.Generic.List<T> where T is a simple Value Type ??
                            {
                                dynamic values = property.Value;
                                if (values.Count != 0)
                                {
                                    string listType = values[0].GetType().FullName;
                                    if (listType == 24.3D.GetType().FullName)
                                    {
                                        System.Diagnostics.Debug.WriteLine(" is List<double>:", property.Key);
                                        foreach (var x in values)
                                        {
                                            double xx = (double)x;
                                            System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", "", xx, xx.GetType());
                                        }
                                    }
                                    else if (listType == 137.GetType().FullName)
                                    {
                                        System.Diagnostics.Debug.WriteLine("{0,15} is List<Int64>:", property.Key);
                                        foreach (var x in values)
                                        {
                                            int xx = (int)x;
                                            System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", "", xx.GetType());
                                        }
                                    }
                                    else if (listType == "string".GetType().FullName)
                                    {
                                        System.Diagnostics.Debug.WriteLine("{0,15} is List<string>:", property.Key);
                                        foreach (var x in values)
                                        {
                                            string xx = (string)x;
                                            System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", "", xx, xx.GetType());
                                        }
                                    }
                                    else if (listType == false.GetType().FullName)
                                    {
                                        System.Diagnostics.Debug.WriteLine("{0,15} is List<bool>:", property.Key);
                                        foreach (var x in values)
                                        {
                                            bool xx = (bool)x;
                                            System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", "", xx, xx.GetType());
                                        }
                                    }
                                    else if (listType == DateTime.Now.GetType().FullName)
                                    {
                                        System.Diagnostics.Debug.WriteLine("{0,15} is List<DateTime>:", property.Key);
                                        foreach (var x in values)
                                        {
                                            DateTime xx = (DateTime)x;
                                            System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", "", xx, xx.GetType());
                                        }
                                    }
                                    else
                                    {
                                        int num = values.Count;
                                        System.Diagnostics.Debug.WriteLine("List is of more complex type: {0}. Number of elements: {1}", listType, num);
                                    }
                                }
                                else
                                    System.Diagnostics.Debug.WriteLine("{0,+15} is an empty List<double>:", property.Key);
                            }
                            else if (property.Value.GetType().Name == "121345".GetType().Name)
                            {
                                Guid guid;
                                if (Guid.TryParse((string)property.Value, out guid))
                                {
                                    System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} Guid", property.Key, property.Value);
                                }
                                else
                                    System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", property.Key, property.Value, property.Value.GetType());
                            }
                            else
                                System.Diagnostics.Debug.WriteLine("{0,15} : {1,-40} {2,-10}", property.Key, property.Value, property.Value.GetType()); //, property.Value.GetType()); //, property.Value.GetType());
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("============");

                    /*System.Diagnostics.Debug.WriteLine("\tApplication properties (set by device):");
                    foreach (KeyValuePair<string, object> prop in partitionEvent.Data.Properties)
                    {
                        PrintProperties(prop);
                    }

                    System.Diagnostics.Debug.WriteLine("\tSystem properties (set by IoT Hub):");
                    foreach (KeyValuePair<string, object> prop in partitionEvent.Data.SystemProperties)
                    {
                        PrintProperties(prop);
                    }*/
                }
                foreach (var d in D2CMessages)
                {

                }
                return Ok();

            }
            catch (Exception)
            {
                return BadRequest(Count);
            }
            
        }

    }
}

