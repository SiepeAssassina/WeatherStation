//Client
void setup()
{
  Serial.begin(1200, SERIAL_8E1);
  pinMode(13, OUTPUT);
  digitalWrite(13, 0);
}

void loop()
{
  int data = 1023;
  data = data >> 8;
  Serial.write(data);
}
