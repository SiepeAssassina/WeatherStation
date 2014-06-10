using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeatherGUI
{    
    public class calibrationData
    {
        public float tempK;
        public float weightK;
        public float gaugeHeight, gaugeArea, gaugeWeight, zero;
        public calibrationData()
        {
            tempK = 0.4778481F;
            weightK = 1500F / 1023F;
            gaugeHeight = 100;
            gaugeWeight = 160;
            gaugeArea = 30;
            zero = 110;
        }
    }
}
