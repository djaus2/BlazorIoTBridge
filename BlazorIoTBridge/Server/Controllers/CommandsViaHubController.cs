using BlazorIoTBridge.Server.Data;
using BlazorIoTBridge.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorIoTBridge.Server.Controllers
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
    public class CommandsViaHubController : ControllerBase
    {         

        private static Dictionary<string, Sensor.CommandCallback> Callbacks = null;
        private readonly AppSettings appsettings;

        private readonly IDataAccessService dataservice;

  

        public CommandsViaHubController(AppSettings _appsettings, IDataAccessService _dataservice)
        {
            this.appsettings = _appsettings;
            this.dataservice = _dataservice;
            if (Callbacks == null)
            Callbacks = new Dictionary<string, Sensor.CommandCallback>();
        }

        //public async Task CallBack2Device(string cmd, int param)
        //{
        //    Shared.Command command;
        //    if (param != -2147483648)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"TestCallBack was called with command: {cmd} param: {param}.");
        //        command = new Command { Action = cmd, Parameter = param, Invoke = false };
        //    }
        //    else
        //    {
        //        System.Diagnostics.Debug.WriteLine($"TestCallBack was called with command: {cmd}");
        //        command = new Command { Action = cmd,  Invoke = false };
        //    }
        //    dataservice.EnqueueCommand(command);
        //    System.Diagnostics.Debug.WriteLine($"Number of comamnds in Q: {dataservice.CommandsCount()}");
        //    await Task.Delay(333);
        //}

        //public async Task CallBack2DeviceStrn(string cmd, string param)
        //{
        //    System.Diagnostics.Debug.WriteLine($"TestCallBack was called with command: {cmd} param: {param}.");
        //    Shared.CommandStrn commandStrn = new CommandStrn { Action = cmd, Parameter = param, Invoke = false };
        //    Shared.Command command = new Command { Action = commandStrn.Parameter,  Invoke = false };
        //    dataservice.EnqueueCommand(command);
        //    System.Diagnostics.Debug.WriteLine($"Number of comamnds in Q: {dataservice.CommandsCount()}");
        //    await Task.Delay(333);
        //}


        private static string CommandNamesCsv = "";
        private static List<string> cmds;
        [HttpPost]
        public async Task<IActionResult> Post(Object obj)
        {
           
            string json = obj.ToString();
            if ((json[0] == '[') && (json[json.Length-1] ==']'))
            {
                //Is csv list of command names
                string commandNamesCsv = json.Substring(1, json.Length - 2);
                commandNamesCsv = commandNamesCsv.Replace("\"","");
                return SetCommands(commandNamesCsv);
            }

            Command cmd = JsonConvert.DeserializeObject<Command>(json);


            if (cmd.Invoke == true)
            {
                if (cmd.Action.ToUpper() == "LISTCOMMANDS")
                {
                    return Ok($"{CommandNamesCsv}");
                }
                //else if ((cmd.Action.ToUpper() == "STARTLISTENING") || (cmd.Action.ToUpper() == "STARTL"))
                //{
                //    SetCommands(CommandNamesCsv);
                //    //if (!SimulatedDevicewithCommands.Client4Commands.IsRunning)
                //    //{
                //    //    return await SetCommands(CommandNamesCsv);
                //    //}
                //    //else
                //    //    return BadRequest($"{cmd.Action} : Device Command Listener already running.");

                //}
                //else if ((cmd.Action.ToUpper() == "STOPLISTENING") || (cmd.Action.ToUpper() =="STOPL"))
                //{
                //    if (SimulatedDevicewithCommands.Client4Commands.IsRunning)
                //    {
                //        await SimulatedDevicewithCommands.Client4Commands.Stop();
                //        dataservice.Reset(false);
                //        Callbacks = new Dictionary<string, Sensor.CommandCallback>();
                //        return Ok($"{cmd.Action} : Device Command Listener was stopped.");
                //    }
                //    else
                //        return BadRequest($"{cmd.Action} : Device Command Listener was not running.");
                //}
                else
                {

                    // // This goes up to the hub and comes back here to the Callback via the Listener
                    // if ((!SimulatedDevicewithCommands.Client4Commands.IsRunning))
                    // {
                    //     return NotFound($"{ cmd.Action} : Device Command Listener has not been started. Use \"StartListening\" command.");
                    // }
                    //else if (!Callbacks.Keys.Contains(cmd.Action))
                    if(!cmds.Contains(cmd.Action))
                        return NotFound($"{ cmd.Action} : Listener not listening for that command.");
                    string command_string = this.appsettings.SERVICE_CONNECTION_STRING;
                    int res = await InvokeDeviceMethod.InvokeDeviceMethodLib.Main(new string[] { command_string, cmd.Action, cmd.Parameter.ToString() });
                    //return Ok($"Device command {cmd.Action} device issued.");
                    string result = "{\"result\":\"Device Command Issued - {" + cmd.Action + "} Outcome:{" + ((int)res).ToString() + "}\"}";
                    System.Diagnostics.Debug.WriteLine("\t\t\t" + result);
                    
                    return ObjectResult(res, result); // await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), res));
                }
            }
            return BadRequest($"{cmd.Action} : Probably call to wrong controller. Try Commands2DeviceController");
        }

        public IActionResult StatusCodeResult(int statusCode)
        {
            return StatusCode(statusCode);
        }

        public IActionResult ObjectResult(int statusCode, string _content)
        {
            var result = new ObjectResult(new { statusCode = statusCode, currentDate = DateTime.Now , comtent = _content});
            result.StatusCode = statusCode;
            return result;
        }

        public IActionResult NotFoundResult()
        {
            return NotFound();
        }


        [HttpGet]
        public IEnumerable<Command> Get()
        {
            return dataservice.GetCommands().ToArray(); ;
        }



        List<string> commandsList;
   
        public IActionResult SetCommands(string commandNamesCsv)
            //public async Task<IActionResult> SetCommands(string commandNamesCsv)
        {
            
            if (!string.IsNullOrEmpty(commandNamesCsv.Trim()))
            {
                CommandNamesCsv = commandNamesCsv.Trim();
                cmds = CommandNamesCsv.Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).ToList <string>();
                if (cmds.Count() != 0)
                {   
                    return Ok($"Device Comamnd device Listener added commands {commandNamesCsv}.");
                    //if (SimulatedDevicewithCommands.Client4Commands.IsRunning)
                    //{
                    //    await SimulatedDevicewithCommands.Client4Commands.Stop();
                    //    dataservice.Reset(true);
                    //}

                    //Callbacks = new Dictionary<string, Sensor.CommandCallback>();
                    //Sensor.CommandCallback cb = CallBack2Device;
                    //foreach (var name in commandsList)
                    //{
                    //    if ((!Callbacks.ContainsKey(name)) && (!Callbacks.ContainsKey(name.ToUpper())) && (!Callbacks.ContainsKey(name.ToLower())))
                    //    {
                    //        Callbacks.Add(name, cb);
                    //    }
                    //}

                    //string connect_stringDev = this.appsettings.IOTHUB_DEVICE_CONN_STRING;
                    //string commandNames = String.Join(",", commandsList.Select(x => x.ToString()).ToArray());
                    //////await SimulatedDevicewithCommands.Client4Commands.Main(new string[] { connect_stringDev, commandNames }, Callbacks);
                    //return Ok($"Device Comamnd device Listener added commands {commandNames}.");
                }
                else
                    return BadRequest($"Add commnds : No commands in csv");
            }
            else
                return BadRequest($"Add commnds : No commands in csv");
        }
    }
}
