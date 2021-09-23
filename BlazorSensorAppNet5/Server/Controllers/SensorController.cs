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

namespace BlazorSensorAppNet5.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SensorController : ControllerBase
    {

        private readonly ILogger<SensorController> logger;

        private static SimulatedDeviceCS _SimulatedDeviceCS;

        private static int Count {get;set;} = 0;

        public SensorController(ILogger<SensorController> logger)
        {
            this.logger = logger;
            if (_SimulatedDeviceCS == null)
            {
                Count = 0;
                _SimulatedDeviceCS = new SimulatedDeviceCS();
            }

        }

        ~SensorController()
        {
            _SimulatedDeviceCS = null;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await Task.Delay(1);
            return Ok("Sensors Rok");
        }

            [HttpPost]
        public async Task<IActionResult> Post(object obj)
        {
            bool state;
            Sensor sensor;

            string json = obj.ToString();

            if (bool.TryParse(json, out state))
            {
                await Task.Delay(333);
                return Ok(Count);
            }
            else 
            {
                try
                {
                    //json = "{\"No\":5,\"Id\":\"Sensor5\",\"SensorType\":5,\"Values\":[19.04,56.16,101897.00]}";

                    sensor = JsonConvert.DeserializeObject<Sensor>(json);
                    //if (sensor != null)
                    //{
                    //    if (!SimulatedDevice.KeepRunning)
                    //        await SimulatedDevice.StartMessageSending();
                    await _SimulatedDeviceCS.StartSendDeviceToCloudMessageAsync(sensor);//SendDeviceToCloudMessagesAsync(); //
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

