using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorIoTBridge.SharedDNC
{
    public class Info
    {
        public string DeviceGuid { get; set; }
        public Guid Id
        {
            get
            {
                if (Guid.TryParse(DeviceGuid, out Guid guid))
                    return guid;
                else
                    return Guid.Empty;
            }
        }
        public string HUB_NAME { get; set; } = "";
        public string DEVICE_NAME { get; set; } = "";
        public string SHARED_ACCESS_KEY_NAME { get; set; } = "iothubowner";

        public string IOTHUB_DEVICE_CONN_STRING { get; set; } = "";
        public string IOTHUB_HUB_CONN_STRING { get; set; } = "";
        public string SERVICE_CONNECTION_STRING { get; set; } = "";


        public string SYMMETRIC_KEY { get; set; } = "";

        public string EVENT_HUBS_CONNECTION_STRING { get; set; } = "";
        public string EVENT_HUBS_COMPATIBILITY_PATH { get; set; } = "";
        public string EVENT_HUBS_SAS_KEY { get; set; } = "";
        public string EVENT_HUBS_COMPATIBILITY_ENDPOINT { get; set; } = "";

        public string Txt { get; set; } = "";

    }

    public class Settings
    {
        public string UserDir { get; set; }

        public string Id { get; set; }

        public string InfoController { get; set; }

        public bool FwdTelemetrythruBlazorSvr { get; set; }
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

        public string ReadCommandsController { get; set; }
        public string SensorController { get; set; }

        public bool IsRealDevice { get; set; }

        public string CommandsIfIsSimDevice { get; set; }

        public int defaultTimeout { get; set; }

        public Int64 StartTimeStamp { get; set; }
    }
}
