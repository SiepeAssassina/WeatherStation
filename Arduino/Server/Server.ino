//Server
#include <Time.h>
#define WAITING 0
#define DATA_RX 1  //Client -> Server 
#define DATA_TX 2  //Server -> PC
#define SENSORS 4
byte hh = 0, mm = 0, ss = 0;
sensorData data[4] = {
  666, 231, 1022, 454};
struct sensorData
{
  char sensorAdress;    //0-5 1byte
  unsigned short value; //0-1023 2byte
  
};
char serialState = WAITING;
void setup()
{
  Serial.begin(1200, SERIAL_8E1);
  pinMode(13, OUTPUT);
  digitalWrite(13, 0);
}

void loop()
{  
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
        sendSensorData();
        serialState = WAITING;
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
    if(Serial.read() == 'A' && Serial.read() == 'A' && Serial.read() == 'P')
    {
      serialState = DATA_TX;
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
    Serial.write((data[i] & 0x300) >> 8);
    Serial.write(data[i] & 0xFF);
  }
}









