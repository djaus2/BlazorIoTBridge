using BlazorIoTBridge.Shared;
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
using Microsoft.Extensions.Configuration;

using BlazorIoTBridge.Server.Data;

namespace BlazorIoTBridge.Server.Controllers
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

        private readonly IDataAccessService dataservice;


        public SensorController(AppSettings _appsettings, IDataAccessService _dataservice)
        {
            this.appsettings = _appsettings;
            this.dataservice = _dataservice;

            if (_SimulatedDeviceCS == null)
            {
                dataservice.SetStatus(1);
                _SimulatedDeviceCS = new SimulatedDeviceCS(appsettings.IOTHUB_DEVICE_CONN_STRING);
            }
        }

        ~SensorController()
        {
            _SimulatedDeviceCS = null;
        }



        [HttpGet]
        public IActionResult Get()
        {
            int status = dataservice.GetStatus();
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
                        dataservice.Reset(true);
                        break;
                }
                return Ok(dataservice.GetStatus());
            }
            else
            {
                try
                {
                    sensor = JsonConvert.DeserializeObject<Sensor>(json);
                    sensor.TimeStamp = DateTime.Now.Ticks;
;
                    while (dataservice.GetStatus() == 0) ;
                    dataservice.SetStatus(0);
                    //bool res = await SimulatedDevicewithCommands.Client4Commands.StartSendDeviceToCloudMessageAsync(sensor);//SendDeviceToCloudMessagesAsync(); //SimulatedDevicewithCommands
                    bool res = await _SimulatedDeviceCS.StartSendDeviceToCloudMessageAsync(sensor);//SendDeviceToCloudMessagesAsync(); //SimulatedDevicewithCommands
                    if (res)
                        dataservice.SetStatus(1);
                    else
                        dataservice.SetStatus(-1);
                    int count = dataservice.LogSensor(sensor);
                    return Ok(count);
                }
                catch (Exception)
                {
                    dataservice.SetStatus(-2);
                    return BadRequest(dataservice.GetStatus());
                }
            }
        }

    }
}

