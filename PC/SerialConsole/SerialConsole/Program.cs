using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;


namespace SerialConsole
{
    
    class Program
    {   
        struct sensorData
        {
            public uint[] time;
            public int[] value;
        };
        
        static public SerialPort com;
        public const byte TIME = 0x10;
        public const byte PREAMBLE = 0xAA;
        public const byte READ = 0x20;

        static void Main(string[] args)
        {
            char[] buffer = new char[1];
            com = new SerialPort("COM3");
            com.BaudRate = 600;
            com.Parity = Parity.None;
            com.StopBits = StopBits.One;
            com.Handshake = Handshake.None;
            com.ReadTimeout = 10000;
            com.ReadBufferSize = 8;
            com.Open();
            while (true)
            {
                Console.Write("WAITING> ");
                switch (Console.ReadLine())
                {
                    case "T":
                        {
                            sendTime();
                            break;
                        }
                    case "D":
                        {
                            getSensorData();
                            break;
                        }
                    case "S":
                        {
                            stream();
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        static void stream()
        {
            Console.WriteLine("Connecting...");
            sensorData Data;
            byte[] _buffer;
            sendByte(PREAMBLE);
            sendByte(0x30);
            waitForCom(0x30);
            Console.Clear();
            while (true)
            {
                /*for (int i = 0; i < 10; i++)
                {
                    sendByte(0x31);
                    waitForCom(0x31);
                    //Thread.Sleep(10);
                }*/
                sendByte(0x31);
                waitForCom(0x31);
                sendByte(0x32);
                waitForCom(0x32);
                _buffer = new byte[8];
                Data.value = new int[4];
                Data.time = new uint[2];
                for (byte i = 0; i < 8; i++)
                {
                    //Console.WriteLine(com.ReadByte());
                    _buffer[i] = (byte)com.ReadByte();
                }

                if (com.ReadByte() != computeCRC(_buffer))
                {
                    sendByte(0xFF);
                    Console.WriteLine("CrcError");
                    // return;
                }
                Console.Clear();
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
                    Console.WriteLine("CrcError");
                    //return;
                }

                Data.time[0] = _buffer[0];
                Data.time[1] = _buffer[1];
                
                sendByte(0x00);
                Console.WriteLine(Data.time[0] + ":" + Data.time[1]);
               
            }
        }

        static void getSensorData()
        {
            Console.WriteLine("Connecting...");
            sensorData Data;
            byte[] _buffer;

            sendByte(PREAMBLE);
            sendByte(READ);
            waitForCom(READ);
            int _index = com.ReadByte();
            Console.WriteLine(_index);
            for (int j = 0; j < _index; j++)
            {
                _buffer = new byte[8];
                Data.value = new int[4];
                Data.time = new uint[2];
                for (byte i = 0; i < 8; i++)
                {
                    //Console.WriteLine(com.ReadByte());
                    _buffer[i] = (byte)com.ReadByte();
                }

                if (com.ReadByte() != computeCRC(_buffer))
                {
                    sendByte(0xFF);
                    Console.WriteLine("CrcError");
                   // return;
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
                    Console.WriteLine("CrcError");
                    //return;
                }

                Data.time[0] = _buffer[0];
                Data.time[1] = _buffer[1];

                Console.WriteLine(Data.time[0] + ":" + Data.time[1]);
                sendByte(0x00);
            }            
        }

        static void sendTime()
        {
            sendByte((byte)PREAMBLE);
            Console.WriteLine("Connecting...");
            sendByte((byte)TIME);
            waitForCom(TIME);
            do
            {
                Console.WriteLine("Sending time " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
                byte[] time = new byte[] { 0, 0, 0, 0 };
                time[0] = (byte)DateTime.Now.Hour;
                time[1] = (byte)DateTime.Now.Minute;
                time[2] = (byte)DateTime.Now.Second;
                time[3] = computeCRC(time);
                sendByte(time);
            } while (com.ReadByte() != 0x0);
            Console.WriteLine("Success");
        }

        static void sendByte(params byte[] b)
        {
            com.Write(b, 0, b.Length);
        }

        static byte computeCRC(byte[] b)
        {
            byte _crc = 0;
            for (int i = 0; i < b.Length; i++) _crc ^= b[i];
            return _crc;   
        }

        static void waitForCom(byte b)
        {
            try
            {
                while (com.ReadByte() != b) ;
            }
            catch (TimeoutException)            
            {
                Console.WriteLine("Time too big");
            }
        }        
    }
}
