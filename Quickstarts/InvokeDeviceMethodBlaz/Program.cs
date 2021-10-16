// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub service SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/service

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;


using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using System.IO;


using System.Net.Http;
//using System.Net.Http.Json;

using BlazorIoTBridge.Shared;

namespace InvokeDeviceMethod
{
    /// <summary>
    /// This sample illustrates the very basics of a service app invoking a method on a device.
    /// </summary>
    public class Program
    {
        private static ServiceClient s_serviceClient;

        private static string command = "SetTelemetryInterval";
        private static string payload = "10";

        // Connection string for your IoT Hub
        // az iot hub show-connection-string --hub-name PnPHub4 --policy-name service
        private static string s_connectionString = "{Your service connection string here}";

        public static async Task Main(string[] args)
        {
            if (args.Length > 0)
                s_connectionString =  args[0];
            if (args.Length > 1)
                command = args[1];
            if (args.Length > 1)
                payload = args[2];

            //s_connectionString = "HostName=PnPHub4.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=l6z5TzRQvJK+hVELXWhlbyWEBLuBX1VBi+SVC2t+LGI=";

            System.Diagnostics.Debug.WriteLine("IoT Hub Quickstarts #2 - InvokeDeviceMethod application.");

            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            s_serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString);
            try
            {
                await InvokeMethodAsync();
            } catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally{
                s_serviceClient.Dispose();
            }
            
        }

        // Invoke the direct method on the device, passing the payload
        private static async Task InvokeMethodAsync()
        {
            var methodInvocation = new CloudToDeviceMethod(command)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            methodInvocation.SetPayloadJson(payload);

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await s_serviceClient.InvokeDeviceMethodAsync("PnPDev4", methodInvocation);

            System.Diagnostics.Debug.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
        }

    }
}
