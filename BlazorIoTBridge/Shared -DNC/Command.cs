using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorIoTBridge.SharedDNC
{
    public class Command
    {
        public string Action { get; set; }
        public int? Parameter { get; set; }
    }
}
