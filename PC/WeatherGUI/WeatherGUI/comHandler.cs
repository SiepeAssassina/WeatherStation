using System;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

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
        private const byte PREAMBLE = 0xAA;
        private const byte WAITING = 0x00;
        private const byte LISTENING = 0x10;
        private const byte READEE = 0x20;
        private const byte STREAM = 0x30;
        private const byte TIME = 0x40;
        private const byte RESET = 0x50;
        private const byte ECHO = 0x60;
        private const byte HEARTBEAT = 0x70;
        private const uint TIMEOUT = 1000;
        public int pooling = 1000;
        static public byte[] lastPacketData = null;
        static public byte lastPacketLenght = 0;
        static public byte lastPacketOpCode;
        private Thread thread = null;
        private sensorData Data;
        private volatile bool shouldStop = false;
        private volatile bool shouldRst = false;

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
            if (!com.IsOpen)
            {
                return;               
            }
            thread = new Thread(() =>
            {
                DoWork();               
            });  

            thread.Start();
            while (!thread.IsAlive) ;
            mWindow.updateState(true);
        }
        
        public void disconnect()
        {
            try
            {                
                if (thread != null && thread.IsAlive)
                {
                    shouldStop = true;
                    thread.Join();
                    mWindow.appendDebug("Thread has been terminated");
                    mWindow.updateState(false);    
                }                
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void reset()
        {
            mWindow.appendDebug("Request sent!");
            shouldRst = true; 
        }

        private void DoWork()
        {
            Stopwatch wtc = Stopwatch.StartNew();
            shouldStop = false;
            byte[] buffer = new byte[] { 0x00, 0x01, 0x03, 0x04 };
            byte retry = 0;
            wtc.Start();
            long currentTime = wtc.ElapsedMilliseconds;

            sendTime();
            while (retry <= 10 && !shouldStop)
            {                          

                if ((wtc.ElapsedMilliseconds - currentTime) > pooling)
                {
                    mWindow.appendDebug("Sensorz");
                    getSensorData();
                    for (int i = 0; i < 4; i++)
                        mWindow.appendDebug("Sensor " + i + " -> " + Data.value[i]);
                    currentTime = wtc.ElapsedMilliseconds;
                }

                sendPacket(null, HEARTBEAT, 0);
                
                if (waitForSerial(TIMEOUT) && safeRead() == PREAMBLE) receivePacket();                

                if (lastPacketOpCode != HEARTBEAT)
                {
                    retry++;
                    mWindow.appendDebug("No answer detected [" + retry + "/10]");
                    continue;
                }
                retry = 0;
                mWindow.appendDebug("Got heartbeat!");

                for (int i = 0; i < pooling; i++)
                {
                    Thread.Sleep(1);
                    if (shouldRst)
                    {
                        sendPacket(null, RESET, 0);
                        Thread.Sleep(4000);
                        shouldRst = false;
                        mWindow.appendDebug("Reset!");
                        sendTime();
                    }
                    if (shouldStop) thread.Abort();
                }  
            }
            wtc.Stop();                   
        }

        public void getSensorData()
        {
            sendPacket(null, STREAM, 0);
            if (waitForSerial(TIMEOUT) && safeRead() == PREAMBLE)
            if (receivePacket())
            {
                Data.value = new int[4];
                Data.time = new uint[2];
                for (int i = 0; i < 4; i++)
                {
                    Data.value[i] = lastPacketData[2 * i] & 0x3;
                    Data.value[i] <<= 8;
                    Data.value[i] += lastPacketData[(2 * i) + 1] & 0xFF;
                }
                Data.time[0] = lastPacketData[8];
                Data.time[1] = lastPacketData[9];
            }
            float P = (float)((Data.value[1] / (1024 * 0.009)) + (0.095 / 0.009));
            int RH = (int)((Data.value[2] / (1024 * 0.00636)) - (0.1515 / 0.00636));
            mWindow.updateRawData("Temp -> " + Data.value[0] + " ("+ ((Data.value[0]*0.0486)) + " °K)", 0);                      
            mWindow.updateRawData("Pres -> " + Data.value[1] + " (" + P + " kPa)", 1);            
            mWindow.updateRawData("Humi -> " + Data.value[2] + " (" + RH + " %RH)", 2);
            mWindow.updateRawData("Rain -> " + Data.value[3] + " (" + Data.value[3] * (1500F/1024F) + " g)", 3);
        }

        public bool sendTime()
        {
            mWindow.appendDebug("Sending time " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            byte[] time = new byte[] { 0, 0, 0, 0 };
            time[0] = (byte)DateTime.Now.Hour;
            time[1] = (byte)DateTime.Now.Minute;
            time[2] = (byte)DateTime.Now.Second;
            return sendPacket(time, TIME, 3); 
        }

        private void sendByte(params byte[] b)
        {
            try
            {
                com.Write(b, 0, b.Length);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private byte computeCRC(byte[] b)
        {
            byte _crc = 0;
            for (int i = 0; i < b.Length; i++) _crc ^= b[i];
            return _crc;
        }

        private bool sendPacket(byte[] payload, byte OpCode, byte lenght)
        {
            byte[] packet = new byte[3 + lenght];
            packet[0] = PREAMBLE;
            packet[1] = OpCode;
            packet[2] = lenght;
            for (int i = 0; i < lenght; i++) packet[i + 3] = payload[i];
            byte retry = 0;
            do
            {
                sendByte(packet);
                sendByte(computeCRC(packet));
                if (!waitForSerial(TIMEOUT, 2))
                {
                    retry++;
                    continue;
                }
                if(safeRead() == 0x00 && safeRead() == 0x00) return true;
                else retry = 0;
            } while (retry <= 10);
            return true;
        }

        private bool receivePacket()
        {
            if (!waitForSerial(TIMEOUT, 2)) return false;
            int opCode = safeRead();
            int lenght = safeRead();

            if (opCode == -1 || lenght == -1) return false;
            
            byte[] buffer = new byte[lenght + 3];
            buffer[0] = PREAMBLE;
            buffer[1] = (byte)opCode;
            buffer[2] = (byte)lenght;
            if (lenght > 0)
            {
                lastPacketData = null;
                lastPacketLenght = 0;
                if (!waitForSerial(TIMEOUT, (byte)lenght)) return false;
                for (int i = 0; i < lenght; i++) buffer[i + 3] = (byte)safeRead();
            }

            if (!waitForSerial(TIMEOUT)) return false;
            if ((byte)safeRead() == computeCRC(buffer))
            {
                lastPacketOpCode = (byte)opCode;
                lastPacketLenght = (byte)lenght;
                lastPacketData = new byte[lenght];
                for (int i = 0; i < lenght; i++)
                    lastPacketData[i] = buffer[i + 3];
                sendByte(0x00, 0x00);
                return true;
            }
            Console.WriteLine("CRCERROR");
            return false;
        }

        private bool waitForSerial(uint time, byte bytes = 1)
        {            
            if (time > 0)
            {
                Stopwatch wtc = new Stopwatch();
                wtc.Start();
                int currentTime = wtc.Elapsed.Milliseconds;
                while (wtc.Elapsed.Milliseconds - currentTime < time)
                    if (com.BytesToRead >= bytes) return true;
                Console.WriteLine("TIMEOUT");
                return false;
            }
            while (true) if (com.BytesToRead >= bytes) return true;
        }

        private int safeRead()
        {
            try
            {
                return com.ReadByte();
            }
            catch (TimeoutException)
            {
                mWindow.appendDebug("Byte lost!");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return -1;
        }

        public void Close()
        {
            disconnect();            
            thread.Join();           
            if(com.IsOpen) com.Close();
        }
    }
}