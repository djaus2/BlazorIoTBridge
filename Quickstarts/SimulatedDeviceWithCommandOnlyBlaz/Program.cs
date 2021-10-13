// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using Microsoft.Azure.Devices.Client;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Net.Http;
using BlazorSensorAppNet5.Shared;


namespace SimulatedDevicewithCommands
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry and receiving a command.
    /// For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/DeviceReconnectionSample"/>.
    /// </summary>
    public static class Program
    {
        private static DeviceClient s_deviceClient
        {
            get;
            set;
        } = null;

        public static bool IsRunning
        {
            get { return (!(s_deviceClient == null)); }
        }

        public static async Task Stop ()
        {
            await s_deviceClient.CloseAsync();
            s_deviceClient.Dispose();
            s_deviceClient = null;
        }

        private static readonly TransportType s_transportType = TransportType.Mqtt;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string s_connectionString = "{Your device connection string here}";

        private static TimeSpan s_telemetryInterval = TimeSpan.FromSeconds(10); // Seconds

        private static CancellationTokenSource Cts;

        private static Dictionary<string, Sensor.CommandCallback> Callbacks = null;

        public static async Task Main(string[] args, Dictionary<string,Sensor.CommandCallback> callbacks = null)
        {
            Callbacks = callbacks;

            s_connectionString = "HostName=PnPHub4.azure-devices.net;DeviceId=PnPDev4;SharedAccessKey=EHU9AXAaVtmYknqqzp6HFIrhZhTbtIhpoTSFxVAm5GM=";
                             //  "HostName=PnPHub4.azure-devices.net;DeviceId=PnPDev4;SharedAccessKey=EHU9AXAaVtmYknqqzp6HFIrhZhTbtIhpoTSFxVAm5GM=",
            string commands = "SetTelemetryInterval,Rate,Stop";

            if (args.Length > 0)
            {
                if (args[0] != "x")
                    s_connectionString = args[0];
            }
            if (args.Length > 1)
            {
                commands = args[1];
            }

            string[] cmds = commands.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (Callbacks != null)
                cmds = Callbacks.Keys.ToArray<string>();

            Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device.");

            // This sample accepts the device connection string as a parameter, if present
            //ValidateConnectionString(args);

            try
            {
                // Connect to the IoT hub using the MQTT protocol
                s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);
            } catch (Exception  ex)
            {
                Console.WriteLine(ex.Message);
            }

            //keepRunning = true;
            // Create a handler for the direct method call
            foreach (var cmd in cmds)
            {
                await s_deviceClient.SetMethodHandlerAsync(cmd, CommandfromHubHandler, null);
                System.Diagnostics.Debug.WriteLine($"Listening for {cmd} command.");
            }
            return; ;/*
            while (keepRunning)
                Thread.Sleep(5000);
            // SendDeviceToCloudMessagesAsync is de0signed to run until cancellation has been explicitly requested by Console.CancelKeyPress.
            // As a result, by the time the control reaches the call to close the device client, the cancellation token source would
            // have already had cancellation requested.
            // Hence, if you want to pass a cancellation token to any subsequent calls, a new token needs to be generated.
            // For device client APIs, you can also call them without a cancellation token, which will set a default
            // cancellation timeout of 4 minutes: https://github.com/Azure/azure-iot-sdk-csharp/blob/64f6e9f24371bc40ab3ec7a8b8accbfb537f0fe1/iothub/device/src/InternalClient.cs#L1922
            await s_deviceClient.CloseAsync();

            s_deviceClient.Dispose();
            System.Diagnostics.Debug.WriteLine("Device simulator finished.");*/

        }

        static bool keepRunning = true;
   

        // Handle the direct method call
        private static async Task<MethodResponse> CommandfromHubHandler(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            if ((int.TryParse(data, out int telemetryIntervalInSeconds)) && (! string.IsNullOrWhiteSpace(methodRequest.Name)))
            {
                try
                {

                    int param = telemetryIntervalInSeconds;

                    string msg = $"{{\"Action\" : \"{methodRequest.Name}\" , \"Parameter\" : {param} }}";

                    System.Diagnostics.Debug.WriteLine(msg);
                    string cmd = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
                    if (Callbacks != null)
                    {
                        if (Callbacks.Keys.Contains(methodRequest.Name))
                        {
                            Sensor.CommandCallback cb = Callbacks[methodRequest.Name];
                            await cb.Invoke(methodRequest.Name,param);
                            return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(cmd), 200));
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"Command callback not found {methodRequest.Name }");
                    string result = "{\"result\":\"Command callback not found - {" + methodRequest.Name + "}\"}";
                    return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid command {methodRequest.Name }");
                    string result = "{\"result\":\"Invalid command - {" + methodRequest.Name + "}\"}";
                    return await Task .FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Invalid command {methodRequest.Name }");
                string result = "{\"result\":\"Invalid command parameter - {" + methodRequest.Name + "}\"}";
                return await Task .FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }
    }
}
