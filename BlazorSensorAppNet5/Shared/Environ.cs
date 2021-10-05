using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorSensorAppNet5
{
    public class Environ
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }

        public Environ ()
        {

        }
        public Environ (double temp, double humid, double press)
        {
            Temperature = temp;
            Humidity = humid;
            Pressure = press;
        }
    }
}
