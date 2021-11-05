#include <Arduino.h>
#include "Settings.h"
#include <ArduinoJson.h>

  StaticJsonDocument<64> ObjectDoc ;
// Call ReadSensor and Json Serialize the Sensor data
// If sent over RS232, then is sent here.
// If Ethernet then is sent when passed back
String DoSensor(int sensorNo, String id)
{ 
  ObjectDoc.clear();
  String postData;

  ObjectDoc["No"] = sensorNo;
  String sensorType = "Sensor";
  sensorType += sensorNo;
  ObjectDoc["Id"] = Id;
  ObjectDoc["SensorType"] = sensorNo;
  //ObjectDoc["Value"] = (((float)random(10000))/100.0);
  ReadSensor(  sensorNo);

  serializeJson(ObjectDoc,postData);
  // This serialzes it and sends over serial port.
  serializeJson(ObjectDoc,Serial);
  Serial.println();
  

  
  return postData;
}

// Generate sensor data and insert into Json object
// Only need to modify this for real sensors and fixed sensor type.
void ReadSensor(  int sensorNo)
{

  if (sensorNo<4)
  {
    ObjectDoc["Value"] = (((float)random(10000))/100.0);
  }
  else if (sensorNo<5)
  {
    JsonArray values = ObjectDoc.createNestedArray("Values");
    values.add(((float)random(1000))/10.0);
    values.add(((float)random(1000))/10.0);
    values.add(((float)random(1000))/10.0);
  }
    else if (sensorNo<6)
  {
    JsonArray values = ObjectDoc.createNestedArray("Values");
    values.add(((float)random(100))/100.0);
    values.add(((float)random(100))/100.0);
    values.add(((float)random(100))/100.0);
  }
  else
  {
    int rnd = random(0,9);
    bool bstate = true;
    if (rnd < 5)
    bstate = false;
    ObjectDoc["State"] = bstate;
  }
}

// https://arduinojson.org/v6/example/

String GetAction(String json, int * parameter)
{
  DeserializationError error = deserializeJson(ObjectDoc, json);

  // Test if parsing succeeds.
  if (error) {
    Serial.print("* ");
    Serial.println(json);
    Serial.print(F("* deserializeJson() failed: "));
    Serial.println(error.f_str());
    return;
  }

   const char* action = ObjectDoc["Action"];
   String Action = String(action);
   Action.trim();
   *parameter = (int)ObjectDoc["Parameter"];
   return Action;
}

