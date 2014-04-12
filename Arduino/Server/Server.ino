//Server
#include <Time.h>
#define WAITING 0
#define DATA_RX 1  //Client -> Server 
#define DATA_TX 2  //Server -> PC
#define TIME_SET 3
#define SENSORS 4
byte hh = 0, mm = 0, ss = 0;
struct sensorData
{
  char adress;    //0-5 1byte
  unsigned short value; //0-1023 2byte  
};
sensorData data[4] = {
  {
    0, 1023        }
  , {
    1, 511        }
  , {
    2, 255        }
  , {
    3, 127        }
};

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
  waitForPreamble(); 
  while(serialState != WAITING)
  {
    switch(serialState)
    {
    case DATA_RX:
      {
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
        digitalWrite(13, 0);
        break;
      }
    } 
  }
}

void waitForSerial()
{
  while(!Serial.available());
}

void waitForPreamble()
{
  while(Serial.available() == 3)
  {
    if(Serial.read() == 'A' && Serial.read() == 'A')
    {
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
      digitalWrite(13, 1);
    }     
  }
}

void receiveData()
{

}

void sendSensorData()
{
  Serial.write(SENSORS);
  delay(10);
  for(int i = 0; i < SENSORS; i++)
  {   
    Serial.write(data[i].adress);
    delay(100);
    Serial.write((data[i].value & 0x300) >> 8);
    delay(100);
    Serial.write(data[i].value & 0xFF);
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












