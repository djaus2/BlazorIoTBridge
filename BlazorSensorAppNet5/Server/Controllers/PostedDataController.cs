using BlazorSensorAppNet5.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorSensorAppNet5.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostedDataController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<PostedDataController> _logger;

        public PostedDataController(ILogger<PostedDataController> logger)
        {
            _logger = logger;
        }

        public static bool isfirst = true;
        [HttpPost]
        public async Task<IActionResult> Post(Command obj)
        {
            Command cmd = obj;
            if (cmd.Action =="STARTQ")
            {
                if(SensorController.PostLog==null)
                    SensorController.StartQ();
            }
            else
            {
                if (SensorController.PostLog == null)
                    SensorController.StartQ();
                SensorController.Command = cmd;
            }
            await Task.Delay(333);
            return Ok(cmd);
        }

        [HttpGet]
        public IEnumerable<Sensor> Get()
        {
            if (SensorController.PostLog == null)
                SensorController.PostLog = new List<Sensor>();
            return SensorController.PostLog.ToArray();
        }
    }
}
