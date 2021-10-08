﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BlazorSensorAppNet5.Shared;

namespace BlazorSensorAppNet5.Shared
{
    public class Sensor
    {
        public const double iNull = (double) int.MinValue;

        public delegate Task CommandCallback(string command, int parameter);

        private long timeStamp;

        public static int Count { get; set; } = 0;
        public int No { get; set; }
        public string Id { get; set; }
        public double? Value { get; set; } = Sensor.iNull;// Signals null

        //public int TemperatureF => 32 + (int)(Value / 0.5556);

        public Environ environ { get; set; }

        public bool State { get; set; }
        public List<double> Values { get; set; }
        public SensorType SensorType { get; set; }

        public long TimeStamp { 
            get => timeStamp; 
            set => timeStamp = value; 
        }

        [JsonIgnore]
        public string DateTime
        {
            get
            {
                var dt = new DateTime(TimeStamp);
                return String.Format("{0:d/MM/yyyy 	h:mm tt}", dt);
            }
        }


    }

    //public class AppSettings
    //{
    //    public string Hub { get; set; }
    //    public static Settings settings { get; set; }
    //    public  Settings Settings { get; set; }
    //    public static string evIOTHUB_DEVICE_CONN_STRING { get; set; }
    //    public string IOTHUB_DEVICE_CONN_STRING { get; set; }
    //}

    public enum SensorType {temperature,pressure,humidity,luminosity,accelerometer,environment,sswitch}
}
