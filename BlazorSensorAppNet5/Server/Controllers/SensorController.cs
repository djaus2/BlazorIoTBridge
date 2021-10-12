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
    /// <summary>
    /// Gets Telemetry from Device via Http Post and forwards to SendTelemetry for onforwarding to the Hub
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SensorController : ControllerBase
    {

        private readonly AppSettings appsettings;


        private static SimulatedDeviceCS _SimulatedDeviceCS;

        private static int Count { get; set; } = 0;



        public SensorController(AppSettings _appsettings)
        {
            this.appsettings = _appsettings;

            if (_SimulatedDeviceCS == null)
            {
                Status = 1;
                _SimulatedDeviceCS = new SimulatedDeviceCS(appsettings.IOTHUB_DEVICE_CONN_STRING);
            }
        }

        ~SensorController()
        {
            _SimulatedDeviceCS = null;
        }

        static int status { get; set; }

        private static object meltdownLock = new object();
        private static bool _meltdownIsHappening;
        public static int Status
        {
            get
            {
                int stat;
                Monitor.Enter(meltdownLock);
                stat = status;
                Monitor.Exit(meltdownLock);
                return stat;
            }
            set
            {
                Monitor.Enter(meltdownLock);
                status = value;
                Monitor.Exit(meltdownLock);
            }
        }

        [HttpGet]
        public IActionResult Get()
        {
            int status;
            status = Status;
            return Ok(status);
        }



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
                        PostedTelemetryLogController.PostLog = new List<Sensor>();
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
                    while (status == 0) ;
                    Status = 0;
                    bool res = await _SimulatedDeviceCS.StartSendDeviceToCloudMessageAsync(sensor);//SendDeviceToCloudMessagesAsync(); //
                    if (res)
                        Status = 1;
                    else
                        Status = -1;


                    if (PostedTelemetryLogController.PostLog == null)
                    {
                        Count = 0;
                        PostedTelemetryLogController.PostLog = new List<Sensor>();
                    }
                    PostedTelemetryLogController.PostLog.Add(sensor);
                    //await Task.Delay(1000);
                    Count++;
                    return Ok(Count);
                    //}
                    //else
                    //    return BadRequest();
                }
                catch (Exception)
                {
                    Status = -2;
                    return BadRequest(Status);
                }
            }
        }

    }
}

