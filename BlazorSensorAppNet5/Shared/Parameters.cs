// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json.Serialization;

namespace BlazorSensorAppNet5.Shared
{
    /// <summary>
    /// Parameters for the application
    /// </summary>
    public class Parameters
    {
        [JsonIgnore]
        public static Parameters _Parameters {get; set;}
        public string DeviceGuid  { get; set;}

        public string IotHubSharedAccessKeyName = "service";

        public string EventHubCompatibleEndpoint { get; set; }

        public string EventHubName { get; set; }

        public string SharedAccessKey { get; set; }

        public string EventHubConnectionString { get; set; }

        internal string GetEventHubConnectionString()
        {
            return EventHubConnectionString ?? $"Endpoint={EventHubCompatibleEndpoint};SharedAccessKeyName={IotHubSharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
        }
    }
}
