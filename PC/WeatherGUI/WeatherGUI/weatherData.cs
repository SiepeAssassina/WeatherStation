using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeatherGUI
{
    class weatherData
    {
        public float temp, press, humi, mm;
        public weatherData(float temperature, float pressure, float humidity, float rainmm)
        {
            this.temp = temperature;
            this.press = pressure;
            this.humi = humidity;
            this.mm = rainmm;
        }
    }
}
