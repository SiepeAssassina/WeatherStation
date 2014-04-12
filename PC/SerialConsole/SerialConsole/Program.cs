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
        static void Main(string[] args)
        {
            char[] buffer = new char[1];
            com = new SerialPort("COM3");
            com.BaudRate = 1200;
            com.Parity = Parity.Even;
            com.StopBits = StopBits.One;
            com.Handshake = Handshake.None;
            // com.ReadTimeout = 1000;
            com.ReadBufferSize = 8;
            com.Open();

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
                    case "S":
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

        static void sendTime()
        {
            com.Write("A");
            com.Write("A");
            com.Write("T");
            Console.WriteLine("Sending time " + string.Format("{0:HH:mm:ss tt}", DateTime.Now));
            com.Write(DateTime.Now.Hour.ToString());
            System.Threading.Thread.Sleep(100);
            com.Write(DateTime.Now.Minute.ToString());
            System.Threading.Thread.Sleep(100);
            com.Write(DateTime.Now.Second.ToString());
            System.Threading.Thread.Sleep(100);
        }

        static void waitForCom(char c)
        {
            while (com.ReadByte() != c) ;
        }

        static void getSensorData()
        {
            com.Write("A");
            com.Write("A");
            com.Write("P");
            while (true)
            {
                byte c = (byte)com.ReadByte();
                for (byte i = 0; i < c; i++)
                {
                    Console.Write((byte)com.ReadByte() + ": ");
                    Console.WriteLine(((com.ReadByte() & 0x3) << 8) + com.ReadByte());
                }
            }
        }
    }
}
