using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorSensorAppNet5.SharedDNC
{
    public class Settings
    {
        public bool Auto { get; set; }
        public string ComPort { get; set; }
        public int BaudRate { get; set; }
        public int Delay_Secs { get; set; }
        public string Host { get; set; }
        public uint Port { get; set; }
        public int WriteTimeout { get; set; }
        public int ReadTimeout { get; set; }
        public string ACK { get; set; }
        public string InitialMessage { get; set; }

        public string EVENT_HUBS_CONNECTION_STRING { get; set; }
        public string Hub { get; set; }
        public string EVENT_HUBS_COMPATIBILITY_PATH { get; set; }

        public int StartTimeStamp { get; set; }


    }
}
