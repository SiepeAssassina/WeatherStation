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
#define ECHO 0x60
#define HEARTBEAT 0x70
#define SENSORS 4
#define POOLING 3600000
#define TIMEOUT 1000
#define PREAMBLE 0xAA
#define SUCCESS 0x00
#define ERROR 0xFF

struct sensorData
{
  byte time[2];  
  unsigned short value[4];
}
data;

byte lastPacketLenght = 0;
byte *lastPacketData = NULL;
byte serialState = WAITING;
byte INDEX = 0;
boolean isLinked = false;
unsigned long int lastHeartbeat = 0;

void setup()
{
  Serial.begin(1200, SERIAL_8N1);
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
  
  if((millis() - lastHeartbeat) > 11000) isLinked = false;

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
      receivePacket();
      digitalWrite(13, 0);
      break;
    }  
  case STREAM:
    {
      digitalWrite(13, 1);
      isLinked = true;
      lastHeartbeat = millis();
      if(sendLiveData() == ERROR) serialState = WAITING;      
      digitalWrite(13, 0);
      break;
    }
  case DATA_TX:      
    {
      digitalWrite(13, 1);  
      if(sendEEData() == SUCCESS) serialState = WAITING;
      digitalWrite(13, 0);
      break;
    }
  case TIME_SET:
    {
      digitalWrite(13, 1);
      if(getSerialTime() == SUCCESS) serialState = WAITING;
      digitalWrite(13, 0);
      break;
    }
    case ECHO:
    {
      delay(1000);
      sendPacket(lastPacketData, 0x30, lastPacketLenght);
      serialState = WAITING;
      break;
    }
  }   
}

boolean waitForSerial(unsigned int time, byte nByte = 1)
{
  if(time > 0)
  {
    for(int i = 0; i < time; i++)
    {
      delay(1);
      if(Serial.available() >= nByte) return true;
    }
    serialState = WAITING;
    return false;
  }
  else while(!Serial.available());
  return true;  
}

void waitForPreamble()
{
  if(Serial.read() == PREAMBLE)  serialState = LISTENING;
}

boolean getOperatingMode(byte opCode)
{   
  switch(opCode)
  {
  case RESET:
    {      
      Serial.write(RESET);
      Reset_AVR();
      break;
    }
  case STREAM:
    {
      Serial.write(STREAM);
      serialState = STREAM;
      break;
    }
  case DATA_TX:
    {
      Serial.write(DATA_TX);
      serialState = DATA_TX;
      break;
    }
  case TIME_SET:
    {
      Serial.write(TIME_SET);
      serialState = TIME_SET;
      break;
    }
  case ECHO:
    {
      Serial.write(ECHO);
      serialState = ECHO;      
      break;
    }
  case HEARTBEAT:
    {
      Serial.write(HEARTBEAT);
      lastHeartbeat = millis();
      isLinked = true;
    }
  default:
    {
      return ERROR;
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
  if(!isLinked)
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

boolean sendEEData()
{  
  byte _buffer[10];
  sensorData _data;  
  while(INDEX >= 0)
  {
    while(!eeprom_is_ready());
    cli();   
    eeprom_read_block((void*)&_data, (void*)(sizeof(sensorData)*1), sizeof(sensorData));    
    sei();  
    for(byte i = 0; i < SENSORS; i++)
    { 
      _buffer[i] = (_data.value[i] & 0x300) >> 8; 
      _buffer[i+1] = _data.value[i] & 0xFF; 
    }
    _buffer[8] = _data.time[0];
    _buffer[9] = _data.time[1]; 
    if(sendPacket(_buffer, DATA_TX, 10)) INDEX--;
  } 
  return INDEX == 0;
}

boolean sendLiveData()
{
  byte _buffer[10]; 
  for(byte i = 0; i < SENSORS; i++)
  { 
    _buffer[i] = (data.value[i] & 0x300) >> 8; 
    _buffer[i+1] = data.value[i] & 0xFF; 
  }
  _buffer[8] = data.time[0];
  _buffer[9] = data.time[1];
  return sendPacket(_buffer, STREAM, 10);
}

boolean getSerialTime()
{
  if(lastPacketLenght == 3)
  {
    setTime(lastPacketData[0], lastPacketData[3], lastPacketData[2], 00, 00, 00);
    Serial.write(0x00);
    Serial.write(0x00);
    return SUCCESS;
  }
  Serial.write(0xFF);
  Serial.write(0xFF);
  return ERROR;
}

boolean sendPacket(byte* payload, byte OpCode, byte lenght)
{  
  byte packet[3 + lenght];
  packet[0] = 0xAA;
  packet[1] = OpCode;  
  packet[2] = lenght;
  
  for(int i = 0; i < lenght; i++) packet[i+3] = payload[i];
  byte retry = 0;
  do 
  { 
    for(int i = 0; i < lenght+3; i++) Serial.write(packet[i]);
    Serial.write(computeCRC(packet, lenght+3));
    if(!waitForSerial(TIMEOUT, 2))
    {
      retry++;
      continue;
    }
    else if(Serial.read() == 0x00 && Serial.read() == 0x00)  return SUCCESS; 
    else  retry = 0;
  }
  while(retry <= 10);
  return ERROR;
}

boolean receivePacket()
{   
  if(!waitForSerial(TIMEOUT, 2)) return ERROR;
  byte opCode = Serial.read();   
  byte lenght = Serial.read();
  
  byte _buffer[lenght + 3];    
  _buffer[0] = 0xAA;
  _buffer[1] = opCode;
  _buffer[2] = lenght;
  
  if(!waitForSerial(TIMEOUT, lenght)) return ERROR;
  for(int i = 0; i < lenght; i++)  _buffer[i + 3] = Serial.read();
  
  if(!waitForSerial(TIMEOUT)) return ERROR;  
  if(Serial.read() == computeCRC(_buffer, lenght)) 
  { 
    lastPacketLenght = lenght;    
    lastPacketData = new byte[lenght];
    for(int i = 0; i < lenght; i++) lastPacketData[i] = _buffer[i + 3];    
    Serial.write(0x00);
    Serial.write(0x00);
    getOperatingMode(opCode);
    return SUCCESS;
  }
  Serial.write(0xFF);
  Serial.write(0xFF);
  return ERROR;
}

byte computeCRC(byte* data, byte lenght)
{
  byte _crc = 0;
  for(int i = 0; i < lenght + 3; i++) _crc ^= data[i];
  return _crc;  
}












