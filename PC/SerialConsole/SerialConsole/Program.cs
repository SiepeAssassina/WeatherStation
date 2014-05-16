using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;


namespace SerialConsole
{
    
    class Program
    {
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
            //sendByte(0xAA);
           // Console.WriteLine(com.ReadByte());
            while (true)
            {
                Console.Write("WAITING > ");
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
                    default:
                        {
                            break;
                        }
                }
            }
        }

        static void getSensorData()
        {
            sendByte(PREAMBLE);
            Console.WriteLine("Connecting...");
            sendByte(READ);
            waitForCom(READ);
           
            if (com.ReadByte() != 'D') return;
            byte c = (byte)com.ReadByte();
            for (byte i = 0; i < c; i++)
            {
                Console.Write((byte)com.ReadByte() + ": ");
                Console.WriteLine(((com.ReadByte() & 0x3) << 8) + com.ReadByte());
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
                //System.Threading.Thread.Sleep(100);
                time[1] = (byte)DateTime.Now.Minute;
                //System.Threading.Thread.Sleep(100);
                time[2] = (byte)DateTime.Now.Second;
                //System.Threading.Thread.Sleep(100);            
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
            for (int i = 0; i < b.Length - 1; i++) _crc ^= b[i];
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
