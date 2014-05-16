//Server
#include <Time.h>
#define WAITING 0
#define LISTENING 1
#define DATA_RX 2  //Client -> Server 
#define DATA_TX 3  //Server -> PC
#define TIME_SET 4
#define SENSORS 4
#define TIMEOUT 1000
#define SUCCESS 0x00
#define ERROR 0xFF

struct sensorData
{
  byte time[2];  
  unsigned short value[4]; //0-1023 2 byte  
}
sensordata;

sensorData Data;
char serialState = WAITING;
bool ERRORLEVEL = SUCCESS;
const short pooling = 20;

void setup()
{
  Serial.begin(600, SERIAL_8N1);
  analogReference(EXTERNAL);
  pinMode(13, OUTPUT);
  digitalWrite(13, 0);
  setTime(0, 0, 0, 00, 00, 00);
}

void loop()
{   
  if(second() % pooling == 0 && serialState == WAITING)
  {
    update();
  }
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
      ERRORLEVEL = receiveSensorData(); 
      Serial.write(ERRORLEVEL);     
      if(ERRORLEVEL == SUCCESS) serialState = WAITING;
      digitalWrite(13,0);
      break;
    }
  case DATA_TX:      
    {
      digitalWrite(13, 1);     
      ERRORLEVEL = sendSensorData();     
      //Serial.write(ERRORLEVEL);
      if(ERRORLEVEL == SUCCESS) serialState = WAITING;
      digitalWrite(13, 0);
      break;
    }
  case TIME_SET:
    {
      digitalWrite(13, 1);
      ERRORLEVEL = getSerialTime();   
      Serial.write(ERRORLEVEL);   
      if(ERRORLEVEL == SUCCESS) serialState = WAITING;
      digitalWrite(13, 0);
      break;
    }
  }   
}

void update()
{
  digitalWrite(13, 1);
  for(int i = 0; i < SENSORS; i++)
  {
    Data.value[i] = analogRead(i);    
  }
  Data.time[0] = hour();
  Data.time[1] = minute();
  digitalWrite(13, 0);
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

bool getOperatingMode()
{ 
  if(!waitForSerial(TIMEOUT)) return ERROR;
  switch(Serial.read())
  {
  case 0x20:
    {
      serialState = DATA_TX;
      Serial.write(0x20);
      break;
    }
  case 0x10:
    {
      serialState = TIME_SET;
      Serial.write(0x10);
      break;
    }
  default:
    {
      return ERROR;
      break;
    }
  }
}

bool receiveSensorData()
{
  Serial.write(0xAA);  
  if(!waitForSerial(10)) return ERROR;   
  if(Serial.read() != 0xAA) return ERROR;

  unsigned short _buffer;
  byte _data[2 * SENSORS];
  byte _crc; 

  for(int i = 0; i < 2 * SENSORS; i++)
  {  
    if(!waitForSerial(TIMEOUT)) return ERROR;   
    _data[i] = Serial.read();  
  }

  for(int i = 0; i < 2* SENSORS; i++) _crc ^= _data[i];  

  if(!waitForSerial(TIMEOUT)) return ERROR;   

  if(_crc != Serial.read()) return ERROR;
  for(int i = 0; i < SENSORS; i++)
  {
    Data.value[i] = _data[i] & 0x3 << 8;    
    Data.value[i] += _data[i+1] & 0xFF;  
  }
  return SUCCESS;
}

bool sendSensorData()
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
  return SUCCESS;
}

bool getSerialTime()
{
  if(!waitForSerial(TIMEOUT)) return ERROR;
  byte hh = Serial.read();  
  if(!waitForSerial(TIMEOUT)) return ERROR;
  byte mm = Serial.read();  
  if(!waitForSerial(TIMEOUT)) return ERROR;
  byte ss = Serial.read();  
  if(!waitForSerial(TIMEOUT)) return ERROR;
  byte _crc = 0;
  _crc ^= hh;
  _crc ^= mm;
  _crc ^= ss;
  if(Serial.read() != _crc) return ERROR;
  setTime(hh, mm, ss, 00, 00, 00);
  return SUCCESS;
}





























