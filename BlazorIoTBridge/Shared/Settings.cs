using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorIoTBridge.Shared
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

        public string SHARED_ACCESS_KEY_NAME { get; set; }
        public string EVENT_HUBS_CONNECTION_STRING { get; set; }

        public string SERVICE_CONNECTION_STRING { get; set; }
        public string Hub { get; set; }
        public string EVENT_HUBS_COMPATIBILITY_PATH { get; set; }
        public string IOTHUB_CONN_STRING_CSHARP {get; set;}
        public string IOTHUB_DEVICE_CONN_STRING { get; set; }
    }

    public class AppSettings
    { 
        public string Hub { get; set; }
        public string SHARED_ACCESS_KEY_NAME { get; set; }
        public string DEVICE_NAME { get; set; }
        public string IOTHUB_DEVICE_CONN_STRING { get; set; }
        public string IOTHUB_CONN_STRING_CSHARP { get; set; }
        public string SERVICE_CONNECTION_STRING { get; set; }
        public string DEVICE_ID { get; set; }
        public string EVENT_HUBS_COMPATIBILITY_ENDPOINT { get; set; }
        public string EVENT_HUBS_COMPATIBILITY_PATH { get; set; }
        public string EVENT_HUBS_SAS_KEY { get; set; }
        public string EVENT_HUBS_CONNECTION_STRING { get; set; }
        public string REMOTE_HOST_NAME { get; set; }
        public string REMOTE_PORT { get; set; }
        public string StartTimeStamp { get; set; }


    }


    public interface ISetx
    {
        string Num { get; set; }
        string Hi { get; set; }
    }
    public class Setx: ISetx
    {
        public string Num { get; set; }
        public string Hi { get; set; }
    }
}
