// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure Event Hubs Client for .NET
// For samples see: https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs/samples/README.md
// For documentation see: https://docs.microsoft.com/azure/event-hubs/

using Azure.Messaging.EventHubs.Consumer;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using System.IO;


using System.Net.Http;


using BlazorIoTBridge.SharedDNC;



using static System.Net.Http.HttpClient;
using System.Configuration;

namespace ReadD2cMessagesBlaz
{
    /// <summary>
    /// A sample to illustrate reading Device-to-Cloud messages from a service app.
    /// </summary>
    internal class ReadD2cMessagesBlaz
    {

        static string _host = "https://localhost:44318/";
        static string  connectionString = "";
        static string Hub = "";
        static string EventHubName = "";
        static Int64 StartTimeStamp = 0;
        static Info info;
        private static long lastTS { get; set; } = 637687876439860000;

        public static async Task Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false);

            IConfiguration configuration = builder.Build();
            Settings settings = configuration.GetSection("AppSettings").Get<Settings>();

            _host = $@"{settings.Host}:{settings.Port}/";

            string Id = settings.Id;
            string infoController = (settings.InfoController).Replace("Controller", "");

            Guid GuidId = new Guid(Id);
            using var client = new System.Net.Http.HttpClient();
            client.BaseAddress = new Uri(_host);

            //Console.ForegroundColor = ConsoleColor.DarkCyan;
            //Console.Write("Press enter when the web app is up. ");
            //Console.ResetColor();
            //Console.ReadLine();
            info = new Info();
            string json;
            bool found = false;
            int count = 0;
            do
            {
                Thread.Sleep(1000);
                try
                {
                    //info = client.GetFromJsonAsync<Info>($"{infoController }/{Id}", null).GetAwaiter().GetResult();
                    var res = client.GetAsync($"{infoController }/{Id}").GetAwaiter().GetResult();

                    if (res.IsSuccessStatusCode)
                    {
                        json = res.Content.ReadAsStringAsync().GetAwaiter().GetResult(); //.ReadFromJsonAsync<Info>>();
                        info = JsonConvert.DeserializeObject<Info>(json);
                        found = true;
                    }
                    else
                    if (info == null)
                    {
                        found = false;
                    }
                }
                catch (Exception)
                {
                    found = false;
                }
                if (!found)
                {
                    if (count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Waiting for settings from the Azure IoT Bridge: ");
                        Console.ResetColor();
                    }
                    else
                    {
                        if (count % 20 ==0 )
                            Console.WriteLine();
                        if (count % 2 == 0)
                            Console.Write('/');
                        else
                            Console.Write('\\');
                    }
                    count++;
                }
            } while (!found);
            Console.WriteLine();
            Console.WriteLine("Got Settings");
            if (!found)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("Device not registered.");
                //Console.ForegroundColor = ConsoleColor.DarkGreen;
                //Console.Write("Press enter to exit this app.");
                //Console.ResetColor();
                //Console.ReadLine();
            }
            else
            {
                connectionString = info.EVENT_HUBS_CONNECTION_STRING;
                Hub = info.HUB_NAME;
                EventHubName = ""; // settings.EVENT_HUBS_COMPATIBILITY_PATH;

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("IoT Hub - Read device to cloud messages. Ctrl-C to exit.\n");
                Console.ResetColor();

                // Set up a way for the user to gracefully shutdown
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("Exiting...");
                };

                // 0 means show all
                // -1 means only show from start of app
                // Otherwise can set a specific time by usings its DateTime.Ticks property
                StartTimeStamp = settings.StartTimeStamp;
                if (StartTimeStamp < 0)
                    lastTS = DateTime.Now.ToUniversalTime().Ticks;
                else
                    lastTS = StartTimeStamp;
                // Run the sample
                await ReceiveMessagesFromDeviceAsync(cts.Token);

                Console.WriteLine("Cloud message reader finished.");
                ;
            }
        }

        static dynamic msg;

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            //string
            //connectionString = "Endpoint=sb://iothub-ns-pnphub4-14855393-855d540dfb.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=qJmy/QM41wN+gXqi75azebWM57f4Jusm+SIny/+NkXw=;EntityPath=pnphub4";
            //_parameters.GetEventHubConnectionString(); ;
            //"Endpoint=sb://iothub-ns-pnphub4-14855393-855d540dfb.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=qJmy/QM41wN+gXqi75azebWM57f4Jusm+SIny/+NkXw=;EntityPath=pnphub4";//
            //Enpoint=sb://iothub-ns-pnphub4-14855393-855d540dfb.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=qJmy/QM41wN+gXqi75azebWM57f4Jusm+SIny/+NkXw=;EntityPath=pnphub4
            // Create the consumer using the default consumer group using a direct connection to the service.
            // Information on using the client with a proxy can be found in the README for this quick start, here:
            // https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Quickstarts/ReadD2cMessages/README.md#websocket-and-proxy-support
            await using var consumer = new EventHubConsumerClient(
                EventHubConsumerClient.DefaultConsumerGroupName,
                connectionString, EventHubName);
                //_parameters.EventHubName);

            Console.WriteLine("Listening for messages on all partitions.");

            try
            {
                // Begin reading events for all partitions, starting with the first event in each partition and waiting indefinitely for
                // events to become available. Reading can be canceled by breaking out of the loop when an event is processed or by
                // signaling the cancellation token.
                //
                // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
                // and samples. For real-world production scenarios, it is strongly recommended that you consider using the
                // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
                //
                // More information on the "EventProcessorClient" and its benefits can be found here:
                //   https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md
                int count = 0;


                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                {

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Int64 ts = partitionEvent.Data.EnqueuedTime.ToUniversalTime().Ticks;
                    dynamic message = JsonConvert.DeserializeObject<ExpandoObject>(data, new ExpandoObjectConverter());
                    msg = message;
                    //Console.WriteLine($"Deserialized JSON into {message.GetType()}");
                    //Console.WriteLine(message.TimeStamp.GetType());
                    long ts2 = message.TimeStamp;
                    if ((ts2 > 0) && (ts == 0))
                        ts = ts2;
                    if (ts > lastTS)
                    {
                        if (ts2 == 0)
                            data = data.Replace(",\"TimeStamp\":0,", $",\"TimeStamp\":{ts},");
                        Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");
                        lastTS = ts;

                        using var client = new System.Net.Http.HttpClient();
                        client.BaseAddress = new Uri(_host);

                        var content = new StringContent(data, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("D2CTelemetryLog", content);
                        if (response.IsSuccessStatusCode)
                            Console.WriteLine("Message has been onforwarded Ok.");
                        else
                            Console.WriteLine("Issue with onforwarding of message: {0}", response.ReasonPhrase);
                        List<double> dummyListofdouble = new List<double>();
                        List<int> dummyListint = new List<int>();
                        List<string> dummyListstring = new List<string>();
                        Environ dummyEnviron = new Environ();

                        //Console.WriteLine($"\tMessage body: {data}");
                        Console.WriteLine("==Telemetry==");
                        foreach (var property in (IDictionary<String, Object>)message)
                        {
                            if (property.Value != null)
                            {
                                if (property.Value.GetType().Name == "ExpandoObject")
                                {
                                    dynamic expandoValues = property.Value;

                                    bool isNullOrEmpty = true;
                                    if (expandoValues != null)
                                    {
                                        foreach (var propertyExpando in (IDictionary<String, Object>)expandoValues)
                                        {
                                            if (isNullOrEmpty)
                                            {
                                                Console.WriteLine("{0,15} : {1,-40} {2,-10}", property.Key, " ... has properties:", property.Value.GetType());
                                                isNullOrEmpty = false;
                                            }
                                            Console.WriteLine("{0,15}   {1,18} : {2,-19} {3,-10}", "", propertyExpando.Key, propertyExpando.Value, propertyExpando.Value.GetType());
                                        }
                                        if (isNullOrEmpty)
                                            Console.WriteLine("{0,+15}  {1,-40} {2-10}", property.Key, " ... has no properties", property.Value.GetType());
                                    }
                                    else
                                        Console.WriteLine("{0,+15}  {1,-40} {2-10}", property.Key, " ...is null", property.Value.GetType());

                                }
                                else if (property.Value.GetType().Name == dummyListofdouble.GetType().Name)
                                {
                                    dynamic values = property.Value;
                                    if (values.Count != 0)
                                    {
                                        if (values[0].GetType().Name == "double")
                                        {
                                            Console.WriteLine("{0,15} is List<double>:", property.Key);
                                            foreach (var x in values)
                                            {
                                                Console.WriteLine("{0,15} : {1,-40} {2,-10}", "", x, x.GetType());
                                            }
                                        }
                                        if (values[0].GetType().Name == "int")
                                        {
                                            Console.WriteLine("{0,15} is List<int>:", property.Key);
                                            foreach (var x in values)
                                            {
                                                Console.WriteLine("{0,15} : {1,-40} {2,-10}", "", x, x.GetType());
                                            }
                                        }
                                        if (values[0].GetType().Name == "string")
                                        {
                                            Console.WriteLine("{0,15} is List<string>:", property.Key);
                                            foreach (var x in values)
                                            {
                                                Console.WriteLine("{0,15} : {1,-40} {2,-10}", "", x, x.GetType());
                                            }
                                        }
                                    }
                                    else
                                        Console.WriteLine("{0,+15} is an empty List<double>:", property.Key);
                                }
                                else if (property.Value.GetType().Name == "121345".GetType().Name)
                                {
                                    Guid guid;
                                    if (Guid.TryParse((string)property.Value, out guid))
                                    {
                                        Console.WriteLine("{0,15} : {1,-40} Guid", property.Key, property.Value);
                                    }
                                    else
                                    {
                                        string val = (string)property.Value;

                                        // DateTime string has a tab in it. Replace with space
                                        if (DateTime.TryParse(val, out DateTime dat))
                                            val = val.Replace('\t', ' ');

                                        Console.WriteLine("{0,15} : {1,-40} {2,-10}", property.Key, val, property.Value.GetType());
                                    }
                                }
                                else
                                {
                                    if (property.Value.GetType().Name == "Double")
                                    {
                                        // Skip null integers. -1 is used as Json value can't be null
                                        double vald = (double)property.Value;
                                        if (vald == Sensor.iNull)
                                            continue;
                                    }
                                    Console.WriteLine("{0,15} : {1,-40} {2,-10}", property.Key, property.Value, property.Value.GetType()); //, property.Value.GetType()); //, property.Value.GetType());
                                }
                            }
                        }
                        Console.WriteLine("============");

                        Console.WriteLine("\tApplication properties (set by device):");
                        foreach (KeyValuePair<string, object> prop in partitionEvent.Data.Properties)
                        {
                            PrintProperties(prop);
                        }

                        Console.WriteLine("\tSystem properties (set by IoT Hub):");
                        foreach (KeyValuePair<string, object> prop in partitionEvent.Data.SystemProperties)
                        {
                            PrintProperties(prop);
                        }
                    }

                    if (Console.KeyAvailable)
                        return;
                }
            }
            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void PrintProperties(KeyValuePair<string, object> prop) 
        {
            string propValue = prop.Value is DateTime
                ? ((DateTime)prop.Value).ToString("O") // using a built-in date format here that includes milliseconds
                : prop.Value.ToString();

            Console.WriteLine($"\t\t{prop.Key}: {propValue}");
        }
    }
}
