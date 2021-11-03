using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorIoTBridge.Shared
{
    public class Command
    {
        public string Id { get; set; }
        public string Action { get; set; }
        public int? Parameter { get; set; } = Sensor.iNull; //Used to represent null
        public bool Invoke { get; set; } = false;
    }

}
