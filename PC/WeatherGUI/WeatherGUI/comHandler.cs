using System;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

namespace WeatherGUI
{
    class comHandler
    {
        struct sensorData
        {
            public uint[] time;
            public int[] value;
        };
        private SerialPort com;
        private IMainWindow mWindow;
        private const byte TIME = 0x10;
        private const byte PREAMBLE = 0xAA;
        private const byte READ = 0x20;
        private Thread thread = null;

        public comHandler(string COM, int baud, IMainWindow mWindow)
        {
            this.mWindow = mWindow;
            com = new SerialPort(COM);
            com.BaudRate = baud;
            com.Parity = Parity.None;
            com.StopBits = StopBits.One;
            com.Handshake = Handshake.None;
            com.ReadTimeout = 100;
            com.ReadBufferSize = 8;
            try
            {
                com.Open();
                mWindow.appendDebug("Opened " + COM + " @ " + baud + " baud");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }                      
        }

        public void connect()
        {
            thread = new Thread(() =>
            {
                while(!sendTime());
                sendByte(PREAMBLE);
                sendByte(0x30);
                if (safeRead() != 0x30) thread.Abort();
                while(!stream());
            });  
            thread.Start(); 
        }

        public void disconnect()
        {
            if (thread != null) thread.Abort();

            mWindow.appendDebug("Task killed");
        }
       
        public bool stream()
        {
            sensorData Data;
            byte[] _buffer;
           
            while (true)
            {
                for (int i = 0; i < 10; i++)
                {
                    sendByte(0x31);
                    if (safeRead() != 0x31) return false;
                    Thread.Sleep(90);
                }

                sendByte(0x31);
                if (safeRead() != 0x31) return false;
                sendByte(0x32);
                if (safeRead() != 0x32) return false;
                _buffer = new byte[8];
                Data.value = new int[4];
                Data.time = new uint[2];

                for (byte i = 0; i < 8; i++)
                {
                    _buffer[i] = (byte)safeRead();
                }

                if (safeRead() != computeCRC(_buffer))
                {
                    sendByte(0xFF);
                    mWindow.appendDebug("CrcError");
                    return false;
                }
                
                for (int i = 0; i < 4; i++)
                {
                    Data.value[i] = _buffer[2 * i] & 0x3;
                    Data.value[i] <<= 8;
                    Data.value[i] += _buffer[(2 * i) + 1] & 0xFF;

                    mWindow.appendDebug(Data.value[i].ToString());
                }

                sendByte(0x00);

                _buffer = new byte[2];
                _buffer[0] = (byte)com.ReadByte();
                _buffer[1] = (byte)com.ReadByte();

                if (computeCRC(_buffer) != com.ReadByte())
                {
                    sendByte(0xFF);
                    mWindow.appendDebug("CrcError");
                    return false;
                }

                Data.time[0] = _buffer[0];
                Data.time[1] = _buffer[1];

                sendByte(0x00);
                mWindow.appendDebug(Data.time[0] + ":" + Data.time[1]);
                return true;
            }
        }

        public void getSensorData()
        {
            mWindow.appendDebug("Connecting...");
            sensorData Data;
            byte[] _buffer;

            sendByte(PREAMBLE);
            sendByte(READ);
            if (safeRead() != READ) return;
            int _index = com.ReadByte();
            mWindow.appendDebug("Items:" + _index);
            for (int j = 0; j < _index; j++)
            {
                _buffer = new byte[8];
                Data.value = new int[4];
                Data.time = new uint[2];
                for (byte i = 0; i < 8; i++)
                {
                    _buffer[i] = (byte)com.ReadByte();
                }

                if (safeRead() != computeCRC(_buffer))
                {
                    sendByte(0xFF);
                    mWindow.appendDebug("CrcError");
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    Data.value[i] = _buffer[2 * i] & 0x3;
                    Data.value[i] <<= 8;
                    Data.value[i] += _buffer[(2 * i) + 1] & 0xFF;
                    Console.WriteLine(Data.value[i]);
                }

                sendByte(0x00);

                _buffer = new byte[2];
                _buffer[0] = (byte)com.ReadByte();
                _buffer[1] = (byte)com.ReadByte();

                if (computeCRC(_buffer) != com.ReadByte())
                {
                    sendByte(0xFF);
                    mWindow.appendDebug("CrcError");
                    return;
                }

                Data.time[0] = _buffer[0];
                Data.time[1] = _buffer[1];

                mWindow.appendDebug(Data.time[0] + ":" + Data.time[1]);
                sendByte(0x00);
            }
        }

        public bool sendTime()
        {
            sendByte((byte)PREAMBLE);
            mWindow.appendDebug("Connecting...");
            sendByte((byte)TIME);
            if (safeRead() != TIME) return false;
            do
            {
                mWindow.appendDebug("Sending time " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
                byte[] time = new byte[] { 0, 0, 0, 0 };
                time[0] = (byte)DateTime.Now.Hour;
                time[1] = (byte)DateTime.Now.Minute;
                time[2] = (byte)DateTime.Now.Second;
                time[3] = computeCRC(time);
                sendByte(time);
            } while (safeRead() != 0x0);
            mWindow.appendDebug("Success");
            return true;
        }

        private void sendByte(params byte[] b)
        {
            com.Write(b, 0, b.Length);
        }

        private byte computeCRC(byte[] b)
        {
            byte _crc = 0;
            for (int i = 0; i < b.Length; i++) _crc ^= b[i];
            return _crc;
        }

        private int safeRead()
        {
            try
            {
                return com.ReadByte();
            }
            catch (TimeoutException)
            {
                mWindow.appendDebug("Time too big");
            }
            return -1;
        }

        public void Close()
        {
            com.Close();
        }
    }
}