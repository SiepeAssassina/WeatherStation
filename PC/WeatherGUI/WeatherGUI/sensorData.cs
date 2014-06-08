namespace WeatherGUI
{
    public class sensorData
    {
        public uint[] time;
        public int[] value;
        public int bandGap;
        public sensorData() 
        {
            this.time = new uint[2];
            this.value = new int[4];
            this.bandGap = 0;
        }
    }
}
