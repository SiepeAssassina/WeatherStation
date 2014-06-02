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
  unsigned short bandGap;
}
data;

byte lastPacketLenght = 0;
byte *lastPacketData = NULL;
byte lastPacketOpCode = WAITING;
byte serialState = WAITING;
byte INDEX = 0;
boolean isLinked = false;
boolean memoryFull = false;
unsigned long int lastHeartbeat = 0;

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
  if(millis() % POOLING == 0 && serialState == WAITING) update();
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
      getOperatingMode(lastPacketOpCode);
      digitalWrite(13, 0);
      break;
    }  
  case STREAM:
    {
      digitalWrite(13, 1);
      isLinked = true;
      lastHeartbeat = millis();
      update();
      if(sendLiveData() == SUCCESS) serialState = WAITING;      
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
      digitalWrite(13, 1);
      sendPacket(lastPacketData, ECHO, lastPacketLenght);
      serialState = WAITING;
      digitalWrite(13, 0);
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
      Reset_AVR();
      break;
    }
  case STREAM:
    {     
      serialState = STREAM;
      break;
    }
  case DATA_TX:
    {       
      serialState = DATA_TX;
      break;
    }
  case TIME_SET:
    {      
      serialState = TIME_SET;
      break;
    }
  case ECHO:
    {      
      serialState = ECHO;      
      break;
    }
  case HEARTBEAT:
    {      
      serialState = WAITING;
      sendPacket(NULL, HEARTBEAT, 0);
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
  if(INDEX > 85)
  {
    INDEX = 0;
    memoryFull = true;
  }

  long _buffer[4];  
  for(int j = 0; j < SENSORS; j++)
  {
    _buffer[j] = 0;
    for(int i = 0; i < 16; i++)
    {      
      _buffer[j] += analogRead(j); 
    }
    data.value[j] = (_buffer[j] >> 4) & 0xFFFF;
  }

  data.time[0] = hour();
  data.time[1] = minute();
  data.bandGap = getVCC(); 
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
      _buffer[i*2] = (_data.value[i] & 0x300) >> 8; 
      _buffer[(i*2)+1] = _data.value[i] & 0xFF; 
    }
    _buffer[8] = _data.time[0];
    _buffer[9] = _data.time[1]; 
    if(sendPacket(_buffer, DATA_TX, 10)) INDEX--;
  } 
  return INDEX == 0;
}

boolean sendLiveData()
{
  byte _buffer[12]; 
  for(byte i = 0; i < SENSORS; i++)
  { 
    _buffer[i*2] = (data.value[i] & 0x300) >> 8; 
    _buffer[(i*2)+1] = data.value[i] & 0xFF; 
  }
  _buffer[8] = data.time[0];
  _buffer[9] = data.time[1];
  _buffer[10] = (data.bandGap & 0x300) >> 8;
  _buffer[11] = data.bandGap & 0xFF;
  return sendPacket(_buffer, STREAM, 12);
}

boolean getSerialTime()
{
  if(lastPacketLenght == 3)
  {
    setTime(lastPacketData[0], lastPacketData[1], lastPacketData[2], 00, 00, 00); 
    return SUCCESS;
  }  
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
    Serial.write(computeCRC(packet, lenght));
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

  if(lenght > 0)
  {
    if(!waitForSerial(TIMEOUT, lenght)) return ERROR;
    for(int i = 0; i < lenght; i++)  _buffer[i + 3] = Serial.read();
  }

  if(!waitForSerial(TIMEOUT)) return ERROR;  
  if(Serial.read() == computeCRC(_buffer, lenght)) 
  { 
    free(lastPacketData);
    lastPacketOpCode = opCode;
    lastPacketLenght = lenght;    
    lastPacketData = new byte[lenght];
    for(int i = 0; i < lenght; i++) lastPacketData[i] = _buffer[i + 3];     
    Serial.write(0x00);
    Serial.write(0x00);
    return SUCCESS;
  }
  return ERROR;
}

byte computeCRC(byte* data, byte lenght)
{
  byte _crc = 0;
  for(int i = 0; i < lenght + 3; i++) _crc ^= data[i];
  return _crc;  
}

int getVCC()
{
  const long InternalRef = 1085L;
  ADMUX = (0<<REFS1) | (1<<REFS0) | (0<<ADLAR) | (1<<MUX3) | (1<<MUX2) | (1<<MUX1) | (0<<MUX0);
  delay(50);
  long mean = 0;
  for(int i = 0; i < 16; i++)
  {
    ADCSRA |= _BV( ADSC );
    while(((ADCSRA & (1<<ADSC))!= 0));
    mean += ADC;
  }
  mean = (mean >> 4) & 0xFFFF;
  int VCC =(((InternalRef * 1024L) / mean) + 5L) / 10L;
  return VCC;
}




