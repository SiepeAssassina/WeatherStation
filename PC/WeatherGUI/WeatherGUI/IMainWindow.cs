using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeatherGUI
{
    interface IMainWindow 
    {        
        void appendDebug(string s);
        void updateState(bool b);
        void updateRawData(sensorData data);
    }
}
