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
using System.Dynamic;
using Newtonsoft.Json.Converters;

namespace BlazorIoTBridge.Server.Controllers
{
    /// <summary>
    /// Maintains a list of Telemetry D2C messages
    /// POST adds a message to the List (D2CMessages)
    /// GET returns that list (a list of Json strings).
    /// D2CSensorData.razor in the Wasm client calls get and displays the messages.
    /// ReadD2cMessagesBlaz gets these messages from the Hub and Posts them here.
    /// Note that ReadD2cMessagesBlaz runs as a separate .NetStandard Console app
    ///  ... and needs to be started.
    ///  Also note that it has its own AppSettings for the Server, its Port 
    ///   ... and the IOT Hub specific EVENT_HUBS_CONNECTION_STRING
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class D2CTelemetryLogController : ControllerBase
    {

        private readonly AppSettings appsettings;

        private static int Count { get; set; } = 0;

        private static List<dynamic> D2CMessages { get; set; }
        private static List<string> D2CMessagesJson { get; set; }



        public D2CTelemetryLogController(AppSettings _appsettings)
        {
            appsettings = _appsettings;

            if (D2CMessages == null)
            {
                Count = 0;
                D2CMessages = new List<dynamic>();
                D2CMessagesJson = new List<string>();
            }
        }

        ~D2CTelemetryLogController()
        {

        }

        /// <summary>
        /// Get the list of D2C (IoT Device to Hub) messages as posted here, each as a Json string
        /// </summary>
        /// <returns>The list D2C json strings</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<string> messagesJson = null;
            await Task.Delay(1);
            Monitor.Enter(D2CMessages);
            messagesJson = D2CMessagesJson;
            Monitor.Exit(D2CMessages);
            return Ok(messagesJson);
        }

        public static long lastTS = 0; // Only message at Device after this datetime are stored.
                                       // Ps Want ability to delete messages in IoT Hub. This simulates it.

        /// <summary>
        /// Post messages into local copy of them, as a list. Note two versions.
        /// These are the telemetry messages sent from the Device to the IoT Hub.
        /// The json version is what is returned from Get.  
        /// The Expando version was initially used here to decipher the json content
        /// That is now done by the Client as part of listing these messages.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [HttpPost]
        public  IActionResult Post(object obj)
        {
            string json = obj.ToString();
            try
            {
                dynamic message = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
                long ts = message.TimeStamp;
                if (ts > lastTS)
                {
                    lastTS = ts;
                    Monitor.Enter(D2CMessages);
                    D2CMessages.Add(message);
                    D2CMessagesJson.Add(json);
                    Monitor.Exit(D2CMessages);
                    Count = D2CMessages.Count;
                }
                foreach (var d in D2CMessages)
                {

                }
                return Ok(Count);

            }
            catch (Exception)
            {
                return BadRequest("Problem posting D2C message.");
            }
            
        }

    }
}

