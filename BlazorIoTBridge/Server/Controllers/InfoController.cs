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
    public class InfoController : ControllerBase
    {

        private readonly AppSettings appsettings;

        private readonly IDataAccessService dataservice;

        private readonly ApplicationDBContext _context;

        public InfoController(ApplicationDBContext context, AppSettings _appsettings, IDataAccessService _dataservice)
        {
            this.appsettings = _appsettings;
            this.dataservice = _dataservice;
            dataservice.Devices = new Dictionary<Guid, Data.Device>();
            this._context = context;

        }

        ~InfoController()
        {

        }



        [HttpGet]
        public IActionResult Get()
        {
            var infos = _context.Infos.ToList<Info>();
            var ids = from s in infos select s.Id;
            return Ok(ids.ToArray());
        }



        [HttpGet("{id:Guid}")]
        public IActionResult Get(Guid id)
        {
            var infos = _context.Infos.ToList<Info>();
            var set = from s in infos where s.Id == id select s;
            if ((set != null) &&( set.Count() != 0))
            {
                var info = set.FirstOrDefault();
                if (info != null)
                    return Ok(info);
                else
                    return new NotFoundObjectResult(null);
            }
            return new NotFoundObjectResult(null); ;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Info obj)
        {
            Info info;

            try
            {
                info = (Info)obj;

                if (info != null)
                {
                    var infos = (_context.Infos).ToList();
                    var set = from s in infos where s.Id == info.Id select s;
                    Info info2 = null;
                    if (set.Count() != 0)
                        info2 = set.FirstOrDefault();
                    if (info2 == null)
                    {
                        _context.Add<Info>(info);
                        await _context.SaveChangesAsync();
                        //dataservice.Devices.Add(info.Id, new Data.Device(info.Id, info));
                        return Ok();
                    }
                    else
                    {
                        _context.ChangeTracker.Clear();
                        await Update(info);
                        return Ok();
                    }
                }
                return Ok();
            } catch (Exception ex)
            {
                return Ok();
            }
        }

        public async Task Update(Info info)
        {
            _context.Update<Info>(info);
            await _context.SaveChangesAsync();
        }

    }
}

