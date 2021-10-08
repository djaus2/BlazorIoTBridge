using BlazorSensorAppNet5.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorSensorAppNet5.Server.Controllers
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


        public Commands2DeviceController(AppSettings _appsettings)
        {
            if (Commands == null)
                Commands = new System.Collections.Concurrent.ConcurrentQueue<Command>();
            this.appsettings = _appsettings;
        }

        public static ConcurrentQueue<Command> Commands { get; set; } = null;

        public static void SetCommand(Command value)
        {
            if (Commands == null)
                Commands = new ConcurrentQueue<Command>();
            //Monitor.Enter(block);
            Commands.Enqueue(value);
            //Monitor.Exit(block);
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
                while (!Commands.TryDequeue(out val)) ;
            }
            //Monitor.Exit(block);
            return val;
        }

        private static bool started = false;
        public static bool isfirst = true;
        [HttpPost]
        public async Task<IActionResult> Post(Command obj)
        {
            Command cmd = obj;

            if (!cmd.Invoke)
            {
                if (cmd.Action == "STARTQ")
                {
                    if (Commands == null)
                        Commands = new System.Collections.Concurrent.ConcurrentQueue<Command>();
                    if (PostedTelemetryLogController.PostLog == null)
                        PostedTelemetryLogController.PostLog = new List<Sensor>();
                }
                else
                {
                    if (Commands == null)
                        Commands = new System.Collections.Concurrent.ConcurrentQueue<Command>();
                    SetCommand(cmd);
                    System.Diagnostics.Debug.WriteLine($"Number of comamnds in Q: {Commands.Count()}");
                }
                await Task.Delay(333);
                return Ok(cmd);
            }
            else
                return BadRequest($"{cmd} : Probably call to wrong controller. Try DirectCommansdsFromHub.");
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (Commands == null)
                Commands = new System.Collections.Concurrent.ConcurrentQueue<Command>();
            Command cmd = GetCommand();
            return Ok(cmd);
        }
    }
}
