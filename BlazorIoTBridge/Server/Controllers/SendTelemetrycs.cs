using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using BlazorIoTBridge.Shared;
using System.Threading;

namespace BlazorIoTBridge.Server.Controllers
{
    // Copyright (c) Microsoft. All rights reserved.
    // Licensed under the MIT license. See LICENSE file in the project root for full license information.

    // This application uses the Azure IoT Hub device SDK for .NET
    // For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

    /// <summary>
    /// Forward Telemetry from SensorController as a simulated device, using Azure IoT Huvb C# SDK
    /// .. to the IoT Hub
    /// Runs as a simulated IOT Hub Device.
    /// </summary>
    public class SimulatedDeviceCS
    {



        public static DeviceClient s_deviceClient;
        private static string s_connectionString;


        /// <summary>
        /// Async method to send simulated telemetry,
        /// Sends a single message.
        /// Called by SensorController-Post
        /// </summary>
        public async Task<bool> StartSendDeviceToCloudMessageAsync(Sensor Sensor)
        {
            var messageString = JsonConvert.SerializeObject(Sensor);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            // Send the telemetry message
            try
            {
                System.Diagnostics.Debug.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                await s_deviceClient.SendEventAsync(message);
                System.Diagnostics.Debug.WriteLine("{0} > Sent message (OK?): ");
                return true;
            } catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("{0} > Sent message (Error): {1}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Constructor: Instantiate the DeviceClient.
        /// </summary>
        public SimulatedDeviceCS(string _connection_string)
        {
            System.Diagnostics.Debug.WriteLine("===== Starting StartMessageSending =====");


            s_connectionString = _connection_string; //Shared.AppSettings.settings.IOTHUB_DEVICE_CONN_STRING;

            System.Diagnostics.Debug.WriteLine("Code from IoT Hub Quickstarts from Azure IoT Hub SDK");
            System.Diagnostics.Debug.WriteLine("Using Env Var IOTHUB_DEVICE_CONN_STRING = " + s_connectionString);

            // Connect to the IoT hub using the MQTT protocol
            if (s_deviceClient == null)
                s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);
            System.Diagnostics.Debug.WriteLine("===== Finished StartMessageSending =====");
        }

    }

        
 }
