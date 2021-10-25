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
using BlazorIoTBridge.Shared;
using Newtonsoft.Json;

namespace SimulatedDevicewithCommands
{
    /// <summary>
    /// This sample illustrates the very basics of a device app sending telemetry and receiving a command.
    /// For a more comprehensive device app sample, please see
    /// <see href="https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Samples/device/DeviceReconnectionSample"/>.
    /// </summary>
    public class Client4Commands
    {

        public static DeviceClient s_deviceClient
        {
            get;
            set;
        } = null;

        public static bool IsRunning
        {
            get { return (!(s_deviceClient == null)); }
        }

 

        private static readonly TransportType s_transportType = TransportType.Mqtt;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string s_connectionString = "{Your device connection string here}";

        private static TimeSpan s_telemetryInterval = TimeSpan.FromSeconds(10); // Seconds

        private static CancellationTokenSource Cts;

        private static Dictionary<string, Sensor.CommandCallback> Callbacks = null;

        private static string[] cmds = null;

        private static bool sending = false;

        /// <summary>
        /// Async method to send simulated telemetry,
        /// Sends a single message.
        /// Called by SensorController-Post
        /// </summary>
        public static async Task<bool> StartSendDeviceToCloudMessageAsync(Sensor Sensor)
        {
            return true;
            System.Diagnostics.Debug.WriteLine("===== SendDeviceToCloudMessageAsync In =====");
            var messageString = JsonConvert.SerializeObject(Sensor);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));
            s_connectionString = "HostName=PnPHub4.azure-devices.net;DeviceId=PnPDev4;SharedAccessKey=EHU9AXAaVtmYknqqzp6HFIrhZhTbtIhpoTSFxVAm5GM=";
            if (s_deviceClient == null)
                s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            // Send the telemetry message
            try
            {
                System.Diagnostics.Debug.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                //Monitor.Enter(s_deviceClient);
                while (receiving) ;
                sending = true;
                await s_deviceClient.SendEventAsync(message);
                sending = false;
                //Monitor.Exit(s_deviceClient);
                System.Diagnostics.Debug.WriteLine("{0} > Sent message (OK?): {1}", DateTime.Now, messageString);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("{0} > Sent message (Error): {1}", ex.Message);
                return false;
            }
        }



        public static async Task Main(string[] args, Dictionary<string,Sensor.CommandCallback> callbacks = null)
        {
            Callbacks = callbacks;
            receiving = false;
            sending = false;

            s_connectionString = "HostName=PnPHub4.azure-devices.net;DeviceId=PnPDev4;SharedAccessKey=EHU9AXAaVtmYknqqzp6HFIrhZhTbtIhpoTSFxVAm5GM=";
            //  "HostName=PnPHub4.azure-devices.net;DeviceId=PnPDev4;SharedAccessKey=EHU9AXAaVtmYknqqzp6HFIrhZhTbtIhpoTSFxVAm5GM=",


            string commands = "RATE,START,STOP";

            if (args.Length > 0)
            {
                if (args[0] != "x")
                    s_connectionString = args[0];
            }
            if (args.Length > 1)
            {
                commands = args[1];
            }

            cmds = commands.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (Callbacks != null)
                cmds = Callbacks.Keys.ToArray<string>();

            Console.WriteLine("IoT Hub Quickstarts #1 - Simulated device.");

            // This sample accepts the device connection string as a parameter, if present
            //ValidateConnectionString(args);

            try
            {
                // Connect to the IoT hub using the MQTT protocol
                if (s_deviceClient == null)
                    s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, s_transportType);
            } catch (Exception  ex)
            {
                Console.WriteLine(ex.Message);
            }

            await s_deviceClient.SetMethodDefaultHandlerAsync(DefaultCommandfromHubHandler, null);

            //keepRunning = true;
            // Create a handler for the direct method call
            //foreach (var cmd in cmds)
            //{
            //    await s_deviceClient.SetMethodHandlerAsync(cmd, CommandfromHubHandler, null);
            //    System.Diagnostics.Debug.WriteLine($"Listening for {cmd} command.");
            //}
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

        public static async Task Stop()
        {
            keepRunning = false;
            try
            {
                if (s_deviceClient != null)
                {
                    if (cmds != null)
                    {
                        foreach (var cmd in cmds)
                        {
                            await s_deviceClient.SetMethodHandlerAsync(null, null, null);
                        }
                    }

                    await s_deviceClient.CloseAsync();

                    s_deviceClient.Dispose();
                    s_deviceClient = null;
                }
                System.Diagnostics.Debug.WriteLine("Device command simulator stopped.");
            } catch (Exception ex)
            {
                s_deviceClient = null;
                System.Diagnostics.Debug.WriteLine("Device command simulator error in stopping. " + ex.Message);
            }
        }

        private static async Task<MethodResponse> DefaultCommandfromHubHandler(MethodRequest methodRequest, object userContext)
        {
            string cmd1 = $"{{\"description\":\"Executed DEFAULT method: {methodRequest.Name}\"}}";
            System.Diagnostics.Debug.WriteLine("---------- " + cmd1);
            return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(cmd1), 200));
        }

        static bool skip = false;
        // Handle the direct method call
        private static async Task<MethodResponse> CommandfromHubHandler(MethodRequest methodRequest, object userContext)
        {
            string cmd1 = $"{{\"result\":\"Executed DIRECT method: {methodRequest.Name}\"}}";

            if (!skip)
            {
                //Monitor.Enter(s_deviceClient);
                Task t3 = Task.Run(() => CommandfromHubHandler2(methodRequest, userContext));
                //Monitor.Exit(s_deviceClient);                
            }
            System.Diagnostics.Debug.WriteLine(">>>>> " + cmd1);
            return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(cmd1), 200));

        }

        static bool receiving = false;
        private static async Task CommandfromHubHandler2(MethodRequest methodRequest, object userContext)
        {
            while (sending) ;
            receiving = true;
            bool done = false;
            try
            {
                if ((Callbacks == null) || (Callbacks.Count() == 0))
                {
                    //System.Diagnostics.Debug.WriteLine("Command handler called OK but command not found (No registered (in-app) commands): " + methodRequest.Name);
                }
                else if (string.IsNullOrEmpty(methodRequest.Name))
                {
                    //System.Diagnostics.Debug.WriteLine("Empty command sent.");
                }
                else
                {
                    Sensor.CommandCallback cb = Callbacks.ContainsKey(methodRequest.Name) ? Callbacks[methodRequest.Name] : null;
                    if (cb == null)
                    {
                        //System.Diagnostics.Debug.WriteLine("Command handler called OK but command not found: " + methodRequest.Name);
                    }
                    if (methodRequest.Data != null)
                    {
                        string data = Encoding.UTF8.GetString(methodRequest.Data);
                        if (
                            (!string.IsNullOrEmpty(data)) &&
                            (data != "null") &&
                            (data != "-2147483648") &&
                            (int.TryParse(data, out int param))
                        )
                        {
                            await cb.Invoke(methodRequest.Name, param);
                            done = true;
                        }
                    }
                        
                    if ((!done) && (methodRequest.Name.ToUpper() != "RATE"))
                    {
                        await cb.Invoke(methodRequest.Name, (int)Sensor.iNull);
                        done = true;
                    }
                }
            } catch (Exception ex)
            {

            }
            receiving = false;
            /*if (done)
                System.Diagnostics.Debug.WriteLine("Done {0}", methodRequest.Name);
            else
                System.Diagnostics.Debug.WriteLine("NOT DONE {0}", methodRequest.Name);*/
            /*
            string data = "";
            if (methodRequest.Data != null)
                data = Encoding.UTF8.GetString(methodRequest.Data);

            if (
                    (!string.IsNullOrEmpty(data)) &&
                    (data != "null") &&
                    (int.TryParse(data, out int telemetryIntervalInSeconds)) &&
                    (!string.IsNullOrWhiteSpace(methodRequest.Name))
                )
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
                            await cb.Invoke(methodRequest.Name, param);
                            System.Diagnostics.Debug.WriteLine("Done {0}",cmd);
                            return;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"Command callback not found {methodRequest.Name } with parameter.");
                    string result = "{\"result\":\"Command callback not found - {" + methodRequest.Name + "}\"}";
                    System.Diagnostics.Debug.WriteLine("Not Done 1 {0}", result);
                    return;
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid command {methodRequest.Name } with parameter.");
                    string result = "{\"result\":\"Invalid command - {" + methodRequest.Name + "}\"}";
                    System.Diagnostics.Debug.WriteLine("Not Done 2 {0}", result);
                    return;
                }
            }
            else
            {
                if (methodRequest.Name == "RATE")
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid command {methodRequest.Name }");
                    string result = "{\"result\":\"Invalid command parameter - {" + methodRequest.Name + "}\"}";
                    System.Diagnostics.Debug.WriteLine("Not Done 3 {0}", result);
                    return;
                }
                else
                {
                    try
                    {

                        string msg = $"{{\"Action\" : \"{methodRequest.Name}\" }}";

                        System.Diagnostics.Debug.WriteLine(msg);
                        string cmd = $"{{\"result\":\"Executing direct method: {methodRequest.Name}\"}}";
                        if (Callbacks != null)
                        {
                            if (Callbacks.Keys.Contains(methodRequest.Name))
                            {
                                Sensor.CommandCallback cb = Callbacks[methodRequest.Name];
                                await cb.Invoke(methodRequest.Name, (int)Sensor.iNull);
                                System.Diagnostics.Debug.WriteLine("Not Done {0}", cmd);
                                return;
                            }
                        }
                        System.Diagnostics.Debug.WriteLine($"Command callback not found {methodRequest.Name } without parameter.");
                        string result = "{\"result\":\"Command callback not found - {" + methodRequest.Name + "}\"}";
                        System.Diagnostics.Debug.WriteLine("Not Done 1.1 {0}", result);
                        return;
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid command {methodRequest.Name } without parameter.");
                        string result = "{\"result\":\"Invalid command - {" + methodRequest.Name + "}\"}";
                        System.Diagnostics.Debug.WriteLine("Not Done 1.2 {0}", result);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(">>>" + ex.Message);
        }
        System.Diagnostics.Debug.WriteLine("Not Done {0}", "Method");*/
        }


    }
}
