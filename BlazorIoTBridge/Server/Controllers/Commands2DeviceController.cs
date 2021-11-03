using BlazorIoTBridge.Server.Data;
using BlazorIoTBridge.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorIoTBridge.Server.Controllers
{
    /// <summary>
    /// Commands for Device whether via IoT Hub or from client are placed in a Q here (POST).
    /// The device then monitors, that is Polls this Q and GETs next comamnd when available.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class Commands2DeviceController : ControllerBase
    {

        private readonly AppSettings appsettings;
        private readonly IDataAccessService dataaccesssservice;

        public Commands2DeviceController(AppSettings _appsettings, IDataAccessService _dataaccesservice)
        {
            this.dataaccesssservice = _dataaccesservice;
            this.appsettings = _appsettings;
        }



        //public static ConcurrentQueue<Command> Commands { get; set; } = null;

        public void SetCommand(Guid id, Command cmd)
        {

            //Monitor.Enter(block)
            dataaccesssservice.EnqueueCommand(id, cmd);
            //Monitor.Exit(block);
        }

        /// <summary>
        /// Same as Command=>Get but dequeues it.
        /// </summary>
        /// <returns></returns>
        public Command GetCommand(Guid id)
        {
            //Monitor.Exit(block);
            return dataaccesssservice.DequeueCommand(id);
        }
        public  void ResetQ(bool state)
        {
            dataaccesssservice.Reset(state);
        }

        //public static bool isfirst = true;
        [HttpPost]
        public async Task<IActionResult> Post(Command obj)
        {
            Command cmd = obj;

            if (!cmd.Invoke)
            {
                if (cmd.Action.ToUpper() == "STARTQ")
                {
                    ResetQ(true);
                }
                else if (cmd.Action.ToUpper() == "STOPQ")
                {
                    ResetQ(false);
                }
                else
                {
                    Guid id = Guid.Parse(cmd.Id);
                    if (dataaccesssservice.Started)
                    {
                        SetCommand(id,cmd);
                        System.Diagnostics.Debug.WriteLine($"Number of comamnds in Q: {dataaccesssservice.CommandsCount()}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Device Commands Q not started");
                        return BadRequest($"{cmd.Action} : Device Commands Q not started. Send [StartQ]");
                    }
                    
                }
                await Task.Delay(333);
                return Ok($"Number of comamnds in Q: {dataaccesssservice.CommandsCount()}");
            }
            else
                return BadRequest($"{cmd.Action} : Probably call to wrong controller. Try CommansdsDirectFromHubController.");
        }

        private string IdGuid = "6513d5ed-c0f2-4346-b3fa-642c48fd66a5";

        [HttpGet("{id:guid}")]
        public   IActionResult Get(Guid id)
        {

            Command cmd;
            if (!dataaccesssservice.Started)
            {
                cmd = new Command { Id = "", Action = "", Parameter = 0 };
            }
            else
            {
                cmd = GetCommand(id);
            }
            if (cmd == null)
                cmd = new Command { Id = "", Action = "", Parameter = 0 };
            return Ok(cmd);
        }

    }
}
