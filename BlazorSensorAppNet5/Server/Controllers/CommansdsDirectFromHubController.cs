using BlazorSensorAppNet5.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorSensorAppNet5.Server.Controllers
{
    /// <summary>
    /// SimulatedDeviceWithCommandOnlyBlaz monitors for D2C messages at the hub.
    /// This is started by the STARTLISTENING command from the Hub. See below.
    /// This does a Callback here when a telemetry message is received by the Hub.
    /// The callback sends the command to the Commands2Device.Commands queue
    /// Note that the Callback could be pushed further down the stack (Later).
    ///   ... Thinking of SignalR.
    ///  Other 2D: Get list of comamnds from the device.
    ///  And can simplify the dictionary as its not needed: All commands us eteh same Callback . Later.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class CommansdsDirectFromHubController : ControllerBase
    {


        private readonly AppSettings appsettings;

        public CommansdsDirectFromHubController(AppSettings _appsettings)
        {
            this.appsettings = _appsettings;
        }

        public async Task TestCallBack(string cmd, int param)
        {
            System.Diagnostics.Debug.WriteLine($"TestCallBack was called with command: {cmd} param: {param}.");
            Shared.Command command = new Command { Action = cmd, Parameter = param, Invoke = false };
            Commands2DeviceController.SetCommand(command);
            System.Diagnostics.Debug.WriteLine($"Number of comamnds in Q: {Commands2DeviceController.Commands.Count()}");
            await Task.Delay(333);
        }


        private static string commandNames = "Rate,Start,Stop,Pause,Read";
        private static List<string> cmds;
        [HttpPost]
        public async Task<IActionResult> Post(Command obj)
        {
            Command cmd = obj;

            if (cmd.Invoke == true)
            {
                if (cmd.Action.ToUpper() == "STARTLISTENING")
                {
                    if (!SimulatedDevicewithCommands.Program.IsRunning)
                    {
                        Sensor.CommandCallback cb = TestCallBack;

                        cmds = commandNames.Split(',').ToList<string>();
                        Dictionary<string, Sensor.CommandCallback> callbacks = new Dictionary<string, Sensor.CommandCallback>();
                        foreach (var name in cmds)
                        {
                            callbacks.Add(name, cb);
                        }
                        string names = String.Join(",", commandNames.ToArray());
                        string connect_stringDev = this.appsettings.IOTHUB_DEVICE_CONN_STRING;
                        await SimulatedDevicewithCommands.Program.Main(new string[] { connect_stringDev, commandNames },callbacks);
                        return Ok("Device Comamnd device Listener started.");
                    }
                    else
                        return BadRequest($"{cmd} : Device Command Listener already running.");

                }
                else if (cmd.Action.ToUpper() == "STOPLISTENING")
                {
                    if (SimulatedDevicewithCommands.Program.IsRunning)
                    {
                        await SimulatedDevicewithCommands.Program.Stop();
                        return Ok($"{cmd} : Device Command Listener was stopped.");
                    }
                    else
                        return BadRequest($"{cmd} : Device Command Listener was not running.");
                }
                else
                {

                    // This goes up to the hub and comes back here to the Callback via the Listener
                    if ((!SimulatedDevicewithCommands.Program.IsRunning))
                    {
                        return NotFound($"{ cmd} : Device Command Listener has not been started. Use \"StartListening\" command.");
                    }
                   else if (!cmds.Contains(cmd.Action))
                        return NotFound($"{ cmd} : Listener not listening for that command.");
                    string command_string = this.appsettings.SERVICE_CONNECTION_STRING;
                    Task t3 = Task.Run(() => InvokeDeviceMethod.Program.Main(new string[] { command_string, cmd.Action, cmd.Parameter.ToString() }));
                    await t3;
                    return Ok($"Device command {cmd} device issued.");
                }
            }
            return BadRequest($"{cmd} : Probably call to wrong controller. Try Commands2DeviceController");
        }
    }
}
