//Server
//WAITING: uP waits until it gets any byte on buffer then reads it and if he gets two consequents 'A' the state is switched to
//LISTENING: uP heard the preamble, sending back an ACK byte (0x0), awaits an answer
//DATA_RX: uP is set to receive DATA from the station
//DATA_TX: uP is set to send DATA to the PC 
//TIME_SET: uP gets the time sent from the PC
#include <Time.h>
#define WAITING 0
#define LISTENING 1
#define DATA_RX 2  //Client -> Server 
#define DATA_TX 3  //Server -> PC
#define TIME_SET 4
#define SENSORS 4

struct sensorData
{
  byte time[4];  
  unsigned short value[4]; //0-1023 2 byte  
};

byte hh = 0, mm = 0, ss = 0;
sensorData Data;
char serialState = WAITING;

void setup()
{
  Serial.begin(1200, SERIAL_8E1);
  pinMode(13, OUTPUT);
  digitalWrite(13, 0);
  setTime(hh, mm, ss, 00, 00, 00);
}

void loop()
{    
  //byte timeFigures[4] = {hour() / 10, hour() % 10, minute() / 10, minute() % 10};
  switch(serialState)
  {
  case WAITING:
    {
      digitalWrite(13,0);
      waitForPreamble();
      break;
    }
  case LISTENING:
    {
      digitalWrite(13, 1);
      getOperatingMode();
      digitalWrite(13, 0);
      break;
    }
  case DATA_RX:
    {
      digitalWrite(13,1);
      receiveSensorData();
      serialState = WAITING;
      digitalWrite(13,0);
      break;
    }
  case DATA_TX:      
    {
      digitalWrite(13, 1);
      sendSensorData();
      serialState = WAITING;
      digitalWrite(13, 0);
      break;
    }
  case TIME_SET:
    {
      digitalWrite(13, 1);
      getSerialTime();
      serialState = WAITING;
      digitalWrite(13, 0);
      break;
    }
  }   
}

void waitForSerial()
{
  while(!Serial.available());
}

void waitForPreamble()
{
  if(Serial.read() == 'A')
  {
    waitForSerial();
    if(Serial.read() == 'A')
    { 
      serialState = LISTENING;    
    }
  }
}

void getOperatingMode()
{  
  Serial.write(0x0);
  waitForSerial();
  switch(Serial.read())
  {
  case 'P':
    {
      serialState = DATA_TX;
      break;
    }
  case 'T':
    {
      serialState = TIME_SET;
      break;
    }
  }
}

void receiveSensorData()
{  
  unsigned short _buffer;
  byte _address; 
  for(int i = 0; i < SENSORS; i++)
  {   
    waitForSerial();
    _address = Serial.read();   
    waitForSerial();
    _buffer = (Serial.read() & 0x3) << 8;    
    waitForSerial();
    _buffer += Serial.read() & 0xFF;  
    Data.value[_address] = _buffer;
  }
}

void sendSensorData()
{
  Serial.write(SENSORS);
  delay(10);
  for(int i = 0; i < SENSORS; i++)
  {   
    Serial.write(i);
    delay(100);
    Serial.write((Data.value[i] & 0x300) >> 8);
    delay(100);
    Serial.write(Data.value[i] & 0xFF);
    delay(100);
  }  
}

void getSerialTime()
{
  while(!Serial.available() == 3);
  hh = Serial.read();
  mm = Serial.read();
  ss = Serial.read();
  setTime(hh, mm, ss, 00, 00, 00);
  serialState = WAITING;
}





















