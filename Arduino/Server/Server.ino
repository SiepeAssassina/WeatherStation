//Server
#include <Time.h>
#include <avr/interrupt.h>
#include <avr/eeprom.h>

#define WAITING 0
#define LISTENING 1
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
};

byte serialState = WAITING;
byte INDEX = 0;
bool ERRORLEVEL = SUCCESS;
const short pooling = 1000;

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
  if(millis() % pooling == 0 && serialState == WAITING)
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
  if(INDEX > 102) INDEX = 0;  
  sensorData Data;
  for(int i = 0; i < SENSORS; i++)
  {
    Data.value[i] = analogRead(i);    
  }
  Data.time[0] = hour();
  Data.time[1] = minute();
  while(!eeprom_is_ready()); 
  cli();
  //eeprom_write_block((void*)&yolo, (void*)0, sizeof(yolo));
  eeprom_write_block((void*)&Data, (void*)(sizeof(sensorData)*INDEX), sizeof(sensorData));    
  sei();
  INDEX++; 
  while(!eeprom_is_ready());
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

bool sendSensorData()
{
  byte _crc = 0;
  byte _buffer = 0;
  byte yolo;
  sensorData Data;  
  Serial.write(INDEX);
  for(byte _index = 0; _index < INDEX; _index++)
  {
    while(!eeprom_is_ready());
    cli();
    //eeprom_read_block((void*)&yolo, (void*)0, sizeof(yolo));
    eeprom_read_block((void*)&Data, (void*)(sizeof(sensorData)*1), sizeof(sensorData));    
    sei();
    while(!eeprom_is_ready());
    _crc = 0;
    for(byte i = 0; i < SENSORS; i++)
    { 
      _buffer = (Data.value[i] & 0x300) >> 8;
      _crc ^= _buffer;
      Serial.write(_buffer);
      _buffer = Data.value[i] & 0xFF;
      _crc ^= _buffer;
      Serial.write(_buffer);
    }  

    Serial.write(_crc);

    if(!waitForSerial(TIMEOUT)) return ERROR;
    if(Serial.read() != 0x00) return ERROR;

    _crc = 0;  
    _crc ^= Data.time[0];
    _crc ^= Data.time[1]; 
    Serial.write(Data.time[0]);
    Serial.write(Data.time[1]);
    Serial.write(_crc);

    if(!waitForSerial(TIMEOUT)) return ERROR;
    if(Serial.read() != 0x00) return ERROR;    
  }
  INDEX = 0; 
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


