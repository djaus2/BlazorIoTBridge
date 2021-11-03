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
    public class Commands2DeviceV2Controller : ControllerBase
    {

        private readonly AppSettings appsettings;
        private readonly IDataAccessService dataaccesssservice;

        public Commands2DeviceV2Controller(AppSettings _appsettings, IDataAccessService _dataaccesservice)
        {
            this.dataaccesssservice = _dataaccesservice;
            this.appsettings = _appsettings;
        }



        //public static ConcurrentQueue<Command> Commands { get; set; } = null;

        public void SetCommand(Command value)
        {

            //Monitor.Enter(block);
            dataaccesssservice.EnqueueCommand(value);
            //Monitor.Exit(block);
        }

        /// <summary>
        /// Same as Command=>Get but dequeues it.
        /// </summary>
        /// <returns></returns>
        public Command GetCommand()
        {
            //Monitor.Exit(block);
            return dataaccesssservice.DequeueCommand();
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
                    if (dataaccesssservice.Started)
                    {
                        SetCommand(cmd);
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


        [HttpGet]
        public   IActionResult Get()
        {

            Command cmd;
            if (!dataaccesssservice.Started)
            {
                cmd = new Command { Action = "", Parameter = 0 };
            }
            else
            {
                cmd = GetCommand();
            }
            if (cmd == null)
                cmd = new Command { Action = "", Parameter = 0 };
            return Ok(cmd);
        }

    }
}
