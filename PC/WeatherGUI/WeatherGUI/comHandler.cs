﻿using System;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace WeatherGUI
{
    class comHandler
    {        
        public volatile bool shouldStop = false;
        private volatile bool shouldRst = false;   
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
        public byte[] lastPacketData = null;
        public byte lastPacketLenght = 0;
        public byte lastPacketOpCode;
        public volatile int pooling = 60000;
        private Thread thread = null;
        private sensorData Data;
        private volatile SerialPort com;
        private volatile IMainWindow mWindow;    
 
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
        }

        public bool Open()
        {
            try
            {
                if (com.IsOpen) return false;
                if (thread != null && thread.IsAlive) return false;
                com.Open();
                mWindow.appendDebug("Opened " + com.PortName + " @ " + com.BaudRate + " baud");                
                connect();               
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return false;            
        }

        public void Close()
        {
            try
            {
                if (thread != null && thread.IsAlive)
                {
                    shouldStop = true;
                    thread.Join();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            try { com.Close(); }
            catch (Exception) { }
        }

        public sensorData asyncronousUpdate()
        {
            if (thread != null && thread.IsAlive)
            {
                sensorData buffer = new sensorData();
                shouldStop = true;
                thread.Join();
                thread = new Thread(() =>
                {
                    DoWork();
                });
                buffer = getSensorData();
                thread.Start();
                return buffer;
            }
            else
            {
                try
                {
                    com.Open();
                    return getSensorData();
                }
                catch (Exception exc) { MessageBox.Show(exc.ToString()); }
                throw new Exception();
            }
        }

        private void connect()
        {            
            thread = new Thread(() =>
            {
                DoWork();               
            });            
            thread.Start();
            while (!thread.IsAlive) ;            
        }        

        public void reset()
        {
            mWindow.appendDebug("Request sent!");
            shouldRst = true; 
        }

        private void DoWork()
        {
            Stopwatch wtc = Stopwatch.StartNew();            
            byte[] buffer = new byte[] { 0x00, 0x01, 0x03, 0x04 };
            byte retry = 0;
            Data = new sensorData();
            long currentTime = wtc.ElapsedMilliseconds;   
            wtc.Start();                    
            mWindow.updateState(true);
            sendTime();            
            //getEEData();           
            while (retry <= 10 && !shouldStop)
            {
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

                for (int i = 0; i < 10000; i++)
                {
                    Thread.Sleep(1);

                    if ((wtc.ElapsedMilliseconds - currentTime) > pooling)
                    {
                        i = 0;
                        mWindow.appendDebug("Data");
                        
                        try
                        {
                            Data = getSensorData();
                            for (int j = 0; j < 4; j++)
                                mWindow.appendDebug("Sensor " + j + " -> " + Data.value[j]);
                        }
                        catch (Exception) { mWindow.appendDebug("Data acquiring error"); }
                        finally {   currentTime = wtc.ElapsedMilliseconds;  }
                    }

                    if (shouldRst)
                    {
                        sendPacket(null, RESET, 0);
                        Thread.Sleep(4000);
                        shouldRst = false;
                        mWindow.appendDebug("Reset!");
                        sendTime();
                    }

                    if (shouldStop) break;
                }  
            }
            wtc.Stop();
            mWindow.appendDebug("Thread has been terminated");
            mWindow.updateState(false);    
            shouldStop = false;            
        }

        private void getEEData()
        {
            sensorData buffer = new sensorData();
            sendPacket(null, READEE, 0);
            Thread.Sleep(1);
            receivePacket();
            if (lastPacketOpCode != READEE) return;
            if (lastPacketData[0] == 0 && lastPacketData[1] == 0)
            {
                MessageBox.Show("EEPROM empty!");
                return;
            }
            if (lastPacketData[0] == 0xFF)
            {
                MessageBox.Show("EEPROM full, " + lastPacketData[1] + " hours lost!");
            }
        }

        private sensorData getSensorData()
        {
            sensorData buffer = new sensorData();           
            int retry = 0;
            do
            {
                sendPacket(null, STREAM, 0);
                if (waitForSerial(TIMEOUT) && safeRead() == PREAMBLE)
                {
                    retry = 0;
                    if (receivePacket())
                    {
                        buffer.value = new int[4];
                        buffer.time = new uint[2];
                        for (int i = 0; i < 4; i++)
                        {
                            buffer.value[i] = lastPacketData[2 * i] & 0x3;
                            buffer.value[i] <<= 8;
                            buffer.value[i] += lastPacketData[(2 * i) + 1] & 0xFF;
                        }
                        buffer.time[0] = lastPacketData[8];
                        buffer.time[1] = lastPacketData[9];
                        buffer.bandGap = lastPacketData[10] & 0x3;
                        buffer.bandGap <<= 8;
                        buffer.bandGap += lastPacketData[11] & 0xFF;
                        mWindow.updateRawData(buffer);
                        return buffer;
                    }
                }
            }while (retry++ <= 10);
            throw new Exception();
        }

        private bool sendTime()
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
            mWindow.appendDebug("CRCERROR");
            return false;
        }

        private bool waitForSerial(uint time, byte bytes = 1)
        {            
            if (time > 0)
            {                         
                for(int i = 0; i < time; i++)
                {
                    Thread.Sleep(1);
                    if (com.BytesToRead >= bytes) return true;                   
                }
                mWindow.appendDebug("TIMEOUT");
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
    }
}