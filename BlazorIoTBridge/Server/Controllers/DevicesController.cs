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
    public class DevicesController : ControllerBase
    {

        private readonly AppSettings appsettings;

        private readonly IDataAccessService dataservice;


        public DevicesController(AppSettings _appsettings, IDataAccessService _dataservice)
        {
            this.appsettings = _appsettings;
            this.dataservice = _dataservice;

        }

        ~DevicesController()
        {

        }



        [HttpGet]
        public IActionResult Get()
        {
            var ids = dataservice.DeviceIds;
            return Ok(ids);
        }

        //[HttpGet("{id:guid}")]
        //public IActionResult Get(Guid id)
        //{
        //    var settings = dataservice.Devices[id].settings;
        //    return Ok(settings);
        //}



        //[HttpPost]
        //public async Task<IActionResult> Post(Info obj)
        //{
        //    Info info;

        //    try
        //    { 
        //        info = (Info)obj;

        //        if (info != null)
        //        {
        //            if (!dataservice.Devices.ContainsKey(info.Id))
        //                dataservice.Devices.Add(info.Id, new Data.Device(info.Id,info));
        //        }
        //        await Task.Delay(1);
        //        return Ok();
        //    }
        //    catch (Exception)
        //    {
        //        dataservice.SetStatus(-2);
        //        return BadRequest(dataservice.GetStatus());
        //    }
        //}
    }
}

