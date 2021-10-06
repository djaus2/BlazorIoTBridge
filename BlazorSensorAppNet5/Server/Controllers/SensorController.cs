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
using Microsoft.Azure.Devices;

namespace BlazorSensorAppNet5.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SensorController : ControllerBase
    {

        private readonly ILogger<SensorController> logger;

        private static SimulatedDeviceCS _SimulatedDeviceCS;

        private static int Count { get; set; } = 0;



        public SensorController(ILogger<SensorController> logger)
        {
            this.logger = logger;
            if (_SimulatedDeviceCS == null)
            {
                Count = 0;
                _SimulatedDeviceCS = new SimulatedDeviceCS();
            }
            if (Commands == null)
                StartQ();
        }

        ~SensorController()
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
            await Task.Delay(1);
            Command val = GetCommand();
            if (val == null)
                val = new Command();
            return Ok(val);
        }

        public static List<Sensor> PostLog { get; set; }

        [HttpPost]
        public async Task<IActionResult> Post(object obj)
        {
            int state;
            Sensor sensor;

            string json = obj.ToString();

            if (int.TryParse(json, out state))
            {
                //await Task.Delay(333);
                switch (state)
                {
                    case 1:
                        PostLog = new List<Sensor>();
                        break;
                }
                return Ok(Count);
            }
            else
            {
                try
                {
                    //json = "{\"No\":5,\"Id\":\"Sensor5\",\"SensorType\":5,\"Values\":[19.04,56.16,101897.00]}";

                    sensor = JsonConvert.DeserializeObject<Sensor>(json);
                    sensor.TimeStamp = DateTime.Now.Ticks;
                    //if (sensor != null)
                    //{
                    //    if (!SimulatedDevice.KeepRunning)
                    //        await SimulatedDevice.StartMessageSending();
                    await _SimulatedDeviceCS.StartSendDeviceToCloudMessageAsync(sensor);//SendDeviceToCloudMessagesAsync(); //

                    if (PostLog == null)
                    {
                        Count = 0;
                        PostLog = new List<Sensor>();
                    }
                    PostLog.Add(sensor);
                    //await Task.Delay(1000);
                    Count++;
                    return Ok(Count);
                    //}
                    //else
                    //    return BadRequest();
                }
                catch (Exception)
                {
                    return BadRequest(Count);
                }
            }
        }

    }
}

