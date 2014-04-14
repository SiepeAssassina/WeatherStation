//Client
#define SENSORS 4
struct sensorData
{
  char adress;    //0-5 1byte
  unsigned short value; //0-1023 2byte  
};
sensorData data[4] = {
  {
    0, 1023              }
  , {
    1, 511              }
  , {
    2, 255              }
  , {
    3, 127              }
};

void setup()
{
  Serial.begin(1200, SERIAL_8E1);
  pinMode(13, OUTPUT);
  digitalWrite(13, 0);
}

void loop()
{
  waitForPreamble();    
}

void waitForACK()
{
  while(Serial.read() != 'K');
}

void waitForPreamble()
{
  if(Serial.available() == 3)
  {
    if(Serial.read() == 'A' && Serial.read() == 'A' && Serial.read() == 'S')
    {
      digitalWrite(13, 1);
      sendSensorData();      
      digitalWrite(13, 0);
    }    
    else
    {
      while(Serial.available())
      {
        Serial.read();
      }
    } 
  }
}

void sendSensorData()
{ 
  Serial.write('
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



