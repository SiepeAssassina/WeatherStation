//Client
#define SENSORS 4
#define WAITING 0
#define LISTENING 1
#define DATA_TX 3  //Client -> Server
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

bool waitForSerial(int time)
{
  if(time > 0)
  {
    for(int i = 0; i < time; i++)
    {
      delay(1);
      if(Serial.available()) return true;
    }
    serialState = WAITING;
    return false;
  }
  else while(!Serial.available());
  return true;  
}

void waitForPreamble()
{
  if(Serial.read() == 0xAA)
  {
    serialState = LISTENING;        
  }
}

void sendSensorData()
{ 

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



