using BlazorIoTBridge.Server.Data;
using BlazorIoTBridge.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorIoTBridge.Server.Controllers
{
    /// <summary>
    /// IoT Hub for D2C messages, local copy.
    /// Get returns the current list (PostLog).
    /// The list is added to by the SensorController when a telemetry message is sent to the hub.
    /// Note that this list is a locally maintained one.
    /// D2CTelemetryController keeps a list that is garnered from the Hub.
    /// 
    /// This controller is queried by the client-PostSensorData.razor
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PostedTelemetryLogController : ControllerBase
    {

        private readonly AppSettings appsettings;
        private readonly IDataAccessService dataaccesservice;

        public PostedTelemetryLogController(AppSettings _appsettings, IDataAccessService _dataaccesservice)
        {
            this.dataaccesservice = _dataaccesservice;
            this.appsettings = _appsettings;
        }


        [HttpGet]
        public IEnumerable<Sensor> Get()
        {
            var logs = dataaccesservice.GetLogs();
            return logs.ToArray();
        }
    }
}
