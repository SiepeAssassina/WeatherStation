//Server
#define IDLE 0
#define ACK 1
#define DATA_RX 2
#define DATA_CHUNKS 2
#define SENSORS 4
char dataChunks[DATA_CHUNKS];
int data[4];
int serialState = IDLE;
void setup()
{
  Serial.begin(1200);
  pinMode(13, OUTPUT);
  digitalWrite(13, 0);
}

void loop()
{   
  waitForSerial();  
  switch(Serial.read())
  {
  case 'D':
    {
      serialState = DATA_RX;
      break;
    }        
  }
  while(serialState == DATA_RX)
  {
    receiveData();
  }
}

void waitForSerial()
{
  while(Serial.available() == 2)
  {
    if(Serial.read() == 'A' && Serial.read() == 'A');
      serialState = ACK;
  }
}

void receiveData()
{
  Serial.write('C');
  char _chunks = Serial.read();
  while(!Serial.available());            
  dataChunks[i] = Serial.read();
  Serial.write('N');    
}





