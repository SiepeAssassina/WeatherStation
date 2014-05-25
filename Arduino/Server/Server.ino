#include <Time.h>
#include <avr/interrupt.h>
#include <avr/eeprom.h>
#include <avr/io.h>
#include <avr/wdt.h>

#define Reset_AVR() wdt_enable(WDTO_30MS); while(1) {} 
#define WAITING 0x0
#define LISTENING 0x10
#define DATA_TX 0x20  
#define STREAM 0x30
#define TIME_SET 0x40
#define RESET 0x50
#define SENSORS 4
#define POOLING 3600000
#define TIMEOUT 1000
#define SUCCESS 0x00
#define ERROR 0xFF

struct sensorData
{
  byte time[2];  
  unsigned short value[4];
}
data;

byte serialState = WAITING;
byte INDEX = 0;
bool ERRORLEVEL = SUCCESS;

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
  if(millis() % POOLING == 0 && serialState == WAITING)
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
  case STREAM:
    {
      digitalWrite(13,1);
      stream();
      digitalWrite(13,0);     
      break;
    }
  case DATA_TX:      
    {
      digitalWrite(13, 1);     
      ERRORLEVEL = sendEEData();     
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
  for(int j = 0; j < SENSORS; j++)
  {
    data.value[j] = 0;
    for(int i = 0; i < 4; i++)
    {      
      data.value[j] += analogRead(j); 
    }
    data.value[j] >> 2;
  }
  data.time[0] = hour();
  data.time[1] = minute();
  if(serialState != STREAM)
  {
    while(!eeprom_is_ready()); 
    cli();
    eeprom_write_block((void*)&data, (void*)(sizeof(sensorData)*INDEX), sizeof(sensorData));    
    sei();
    INDEX++; 
    while(!eeprom_is_ready());
  }
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
  case 0x40:
    {
      Serial.write(0x40);
      if(!waitforSerial(100)) break;
      Reset_AVR();      
    }
  case 0x30:
    {
      serialState = STREAM;
      Serial.write(0x30);
      break;
    }
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

void stream()
{
  if(!waitForSerial(11000)) 
  {
    serialState = WAITING;
    return;  
  }
  switch(Serial.read())
  {
  case 0x31:
    {
      Serial.write(0x31);
      break;
    }
  case 0x32:
    {
      Serial.write(0x32);
      update();
      while(sendLiveData() != SUCCESS);
      break;
    }
  case 0x33:
    {
      Serial.write(0x33);
      serialState = WAITING;
      break;
    }
  default:
    {
      serialState = WAITING;
      break;
    }
  }
}

bool sendEEData()
{
  byte _crc = 0;
  byte _buffer = 0;
  sensorData _data;  
  Serial.write(INDEX);

  for(byte _index = 0; _index < INDEX; _index++)
  {
    while(!eeprom_is_ready());
    cli();   
    eeprom_read_block((void*)&_data, (void*)(sizeof(sensorData)*1), sizeof(sensorData));    
    sei();
    while(!eeprom_is_ready());
    _crc = 0;
    for(byte i = 0; i < SENSORS; i++)
    { 
      _buffer = (_data.value[i] & 0x300) >> 8;
      _crc ^= _buffer;
      Serial.write(_buffer);
      _buffer = _data.value[i] & 0xFF;
      _crc ^= _buffer;
      Serial.write(_buffer);
    }  

    Serial.write(_crc);

    if(!waitForSerial(TIMEOUT)) return ERROR;
    if(Serial.read() != 0x00) return ERROR;

    _crc = 0;  
    _crc ^= _data.time[0];
    _crc ^= _data.time[1]; 
    Serial.write(_data.time[0]);
    Serial.write(_data.time[1]);
    Serial.write(_crc);

    if(!waitForSerial(TIMEOUT)) return ERROR;
    if(Serial.read() != 0x00) return ERROR;    
  }
  INDEX = 0; 
  return SUCCESS;
}

bool sendLiveData()
{
  byte _crc = 0;
  byte _buffer = 0; 
  for(byte i = 0; i < SENSORS; i++)
  { 
    _buffer = (data.value[i] & 0x300) >> 8;
    _crc ^= _buffer;
    Serial.write(_buffer);
    _buffer = data.value[i] & 0xFF;
    _crc ^= _buffer;
    Serial.write(_buffer);
  }  

  Serial.write(_crc);

  if(!waitForSerial(TIMEOUT)) return ERROR;
  if(Serial.read() != 0x00) return ERROR;

  _crc = 0;  
  _crc ^= data.time[0];
  _crc ^= data.time[1]; 
  Serial.write(data.time[0]);
  Serial.write(data.time[1]);
  Serial.write(_crc);

  if(!waitForSerial(TIMEOUT)) return ERROR;
  if(Serial.read() != 0x00) return ERROR;    
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

byte CRC8(const byte *data, byte len) 
{
  byte _crc = 0x00;
  while (len--) 
  {
    byte extract = *data++;
    for (byte i = 8; i; i--) 
    {
      byte sum = (_crc ^ extract) & 0x01;
      _crc >>= 1;
      if (sum)
      {
        _crc ^= 0x8C;
      }
      extract >>= 1;
    }
  }
  return _crc;
}


