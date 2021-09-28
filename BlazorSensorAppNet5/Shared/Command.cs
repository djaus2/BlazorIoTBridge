using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorSensorAppNet5.Shared
{
    public class Command
    {
        public string Action { get; set; }
        public int? Parameter { get; set; }
    }
}
