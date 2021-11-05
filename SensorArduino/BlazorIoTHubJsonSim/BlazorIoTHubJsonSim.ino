/*
  By David Jones.
  Post Simulated Sensor Data to a Blazor Service that onforwards it to an Azure IoT Hub.
  Do not need IoT Hub Connection details. Only authority to access the service. In Repo access is Public.
  The Blazor Service on GitHub: https://github.com/djaus2/SensorBlazor
  Mirroring Blazor App to monitor submissions: https://github.com/djaus2/BlazorD2CMessages
  Based uponSimple POST client for ArduinoHttpClient library by Tom Igoe
  and a request body,
 */

 /* Test Hardware:
  * Freetronics: EtherTen https://www.freetronics.com.au/products/etherten 
  * Uses Ethrenet not WiFi
 */
#include "Settings.h"
void(* resetFunc) (void) = 0;

String command="";
String readString;
String previousCommand="";
String Id;
bool g_gotLastAck = false; // Receiver needs to send back "ACK" once sent and/or recieved by IoT Hub

uint32_t g_SampleRate = 3000;      // Delay between Telemetries 
uint32_t g_LastSample = 0;

int Count; //Count the loops. A diffrent sensor for each.

bool waitingForAck = false;

void setup() {
    delay(3000);
    Serial.begin(BAUD);
        Id = "";
    // Receiver must send some initial character/s:
    // * <Guid string>
    char c;
    while (Serial.available()==0);
    delay(SERIAL_BUFFER_FILL_PAUSE);

    // Expect '*'
    do {
      c = (char)Serial.read();
    } while (c != '*');

    // Expect opening brace
    do {
      c = (char)Serial.read();
    } while (c != '{');

   // Get Id a Guid string
   int cntr = 0;
   do{
     while ((Serial.available() >0) && (cntr!=36))
     {  
        delay(SERIAL_BUFFER_FILL_PAUSE); 
        c = (char)Serial.read();
        Id += c;
        cntr++;
     }
  } while (cntr != 36); // Could check if its a Guid here ??

    // Expect closing brace
    do {
      c = (char)Serial.read();
    } while (c != '}');
        
    Serial.println(INITIAL_MESSAGE);
    Count =0;
    command=INITIAL_COMMAND;
    Serial.print("* Initial Mode: ");
    Serial.println(command);
    g_SampleRate = INITIAL_RATE;
    Serial.print("* Initial Rate: ");
    Serial.println(g_SampleRate);
    Serial.println("* Valid Commands (Case insensitive) sent as Json string: ");
    Serial.print("* Id for this device: ");
    Serial.println(Id);
    Serial.print("# ");
    Serial.println(COMMAND_LIST);
    Serial.println("* Rate requires a parameter in mSec");
    Serial.println("* eg. { \"Action\":\"*Rate\", \"Parameter\": 5000}");
    Serial.println("* Nb: Commands with prefix * require a parameter.");
    Serial.println("* Telemetry sends need an Acknowledgement:");
    Serial.println("* {\"Action\" : \"ACK\"  ,\"Parameter\": 0} ");
    Serial.println();
    g_gotLastAck = false;
}

// https://www.arduino.cc/reference/en/language/variables/data-types/stringobject/
// Strings api


String GetCommandJson()
{
    //Check for a new command
    char c;
    int count =0;
    int charCount =0;
    String _readString="";
    if (Serial.available()>0)
    { 
      c=' ';
      // Allow 1 second for start
      int cticksInner1 = millis();
      int lastSampleInner1 = cticksInner1;
      while ((c !='{' ) && ( (cticksInner1 - lastSampleInner1) < JSON_OPEN_BRACKET_TIMEOUT))
      {
        c = (char)Serial.read();
        cticksInner1 = millis();
      }
      if(c =='{')
      {      
        _readString="{";
        //Allow 3 seconds for rest of json string
        int cticksInner5 = millis();
        int lastSampleInner5 = cticksInner5;
        while ((c !='}' ) && ( (cticksInner5 - lastSampleInner5) < JSON_CLOSE_BRACKET_TIMEOUT))
        {
          delay(SERIAL_BUFFER_FILL_PAUSE);  //delay to allow buffer to fill 
          if (Serial.available() >0)
          {
              c = (char)Serial.read();
              _readString += c;
              charCount++;
          } 
          cticksInner5 = millis();
        }
        if (c != '}')
        {          

          Serial.print("* ");
          Serial.println(_readString);
          Serial.println("* Error: Commmand json string incomplete. [Timed out: Closing brace not found]");
          _readString = "";
        }
      }
      else
      {
        // Ignore
        _readString = "";
        //Serial.println(_readString);
        //Serial.println("* Error: Commmand json string not started. [Timed out: No opening brace]");
      }
      // Tidy up
      while (Serial.available() >0)
        c = (char)Serial.read();
    }
    return _readString;
}

int counter = -1;

void loop() {


  uint32_t cticks = millis();
  g_LastSample = cticks; 
do{
    readString = GetCommandJson(); 
    //Serial.println(readString);  
    readString.trim();
    if (readString != "")
    {
      int val;
      String newCommand = GetAction(readString, &val);
      newCommand.trim();
      //Really want a stack for next ...
      if (!newCommand.equals(""))
      {
        if (( newCommand != command)|| (command.equalsIgnoreCase("RATE")))
        {
          String prevPreviousCommand = previousCommand;
          String previousCommand = command;
          if(!newCommand.equalsIgnoreCase("ACK"))
          {
            if(!command.equalsIgnoreCase("CONTINUE"))
            {    
              Serial.print("* Setting command to:");
              Serial.println(newCommand);
            }
          }
          command=newCommand;
          readString ="";
          newCommand = "";
          if (val != -1)
          { 
            Serial.print("* Command (int) parameter is:");
            Serial.println(val);
          }
          

          if(command.equalsIgnoreCase("ACK"))
          {
            g_gotLastAck = true;
            Serial.println("* ACK received for previous send.");
            command = previousCommand;
            previousCommand = prevPreviousCommand;
          }
          else if(command.equalsIgnoreCase("CONTINUE"))
          {
              //if(previousCommand.equalsIgnoreCase("PAUSE"))
              //{    
                // Toggle READ from PAUSE
                command ="READ";
                Serial.print("* Setting command to READ (continuing).");
              //}
          }
          else if(command.equalsIgnoreCase("READ"))
          {
            if (!((g_SampleRate>=1000)&& (g_SampleRate<= 10000)))
            {
              g_SampleRate = INITIAL_RATE;
            }
            if ((val>0)&& (val<= 10))
            {              
              counter = val;
              Serial.print("* Set for ");
              Serial.print(counter);
              Serial.println(" telemetry sends.");
            }
            else
            {
              if(!previousCommand.equalsIgnoreCase("PAUSE"))
              {             
                Serial.println("* Set for continuous telemetry sends.");
                counter = -1;
              }
            }
            Serial.print("* Rate: ");
            Serial.println(g_SampleRate); 
            g_gotLastAck = true;
          }
          else if ((command.equalsIgnoreCase("RESET")) || (command.equalsIgnoreCase("RESTART")))
          {
            // Ref: https://www.theengineeringprojects.com/2015/11/reset-arduino-programmatically.html#:~:text=As%20you%20open%20the%20Serial%20Terminal%2C%20the%20Arduino,at%20How%20to%20get%20Hex%20File%20from%20Arduino.
            // Arduino has a built-in function named as resetFunc()
            // which we need to declare at address 0 
            // and when we execute this function Arduino gets reset automatically.
            //void(* resetFunc) (void) = 0;
            Serial.println("* Restarting Device.");
            Serial.println("* Wait a while (say 10s) then send a dummy (any) command.");
            Serial.println("* Then look for Begin etc. messages:");
            Serial.println("");
            delay(1000);
            resetFunc();
            command = INITIAL_COMMAND;
          }
          else if(command.equalsIgnoreCase("STOP"))
          {
            
          }
          else if(command.equalsIgnoreCase("PAUSE"))
          {
            
          }
          else if(command.equalsIgnoreCase("SLOW"))
          {
            g_SampleRate *=2;
            command="READ";
            if ((val>0)&& (val<= 10))
            {              
              counter = val;
              Serial.print("* Set for ");
              Serial.print(counter);
              Serial.println(" telemetry sends.");
            }
            Serial.print("* Rate: ");
            Serial.println(g_SampleRate);
            g_gotLastAck=true;
          }
          else if (command.equalsIgnoreCase("FAST"))
          {
            g_SampleRate /=2;
            command="READ";
            if ((val>0)&& (val<= 10))
            {              
              counter = val;
              Serial.print("* Set for ");
              Serial.print(counter);
              Serial.println(" telemetry sends.");
            }
            Serial.print("* Rate: ");
            Serial.println(g_SampleRate);
            g_gotLastAck=true;
          }
          else if (command.equalsIgnoreCase("START"))
          {
            g_SampleRate = INITIAL_RATE;
            if ((val>0)&& (val<= 10))
            {              
              counter = val;
              Serial.print("* Set for ");
              Serial.print(counter);
              Serial.println(" telemetry sends.");
            }
            else
            {              
              Serial.println("* Set for continuous telemetry sends.");
              counter = -1;
            }
            Serial.print("* Rate: ");
            Serial.println(g_SampleRate);
 
            command="READ";
            g_gotLastAck = true;
          }     
          else if (command.equalsIgnoreCase("RATE"))
          {
            if ((val>=1000)&& (val<= 10000))
            {
              // Set the sample rate
              g_SampleRate = val;
              command ="READ";
              Serial.print("* Rate: ");
              Serial.println(g_SampleRate);
              g_gotLastAck=true;
            }
            else 
            {
              Serial.println ("* Command ignored");
              command = previousCommand;
              previousCommand = prevPreviousCommand; 
            }  
             
          }
          else 
          {
            Serial.println ("* Command ignored");
            command = previousCommand;
            previousCommand = prevPreviousCommand; 
          } 
        }
      }
    }
    cticks = millis();
  }
  while (( (cticks - g_LastSample) < g_SampleRate) || (!g_gotLastAck));

  // Don't send until previous send is acknowledged.
  // But still do a pass in the Do loop above looking for commands, in particular ACK.
  if ((command.equalsIgnoreCase("READ"))  && g_gotLastAck)
  {
    g_LastSample = cticks;
    g_gotLastAck = false;
    String postData;
    // Note that for Serial, the data gets sent in DoSensor()
    postData = DoSensor(Count,Id);
    Count++;
    if (Count >6)
      Count=0;
    if (counter != -1)
    {
      Serial.print("* Count: ");
      Serial.println(counter);
      if (--counter == 0)
      {
        counter = -1;
        command = "STOP";
        Serial.println("* Command set: STOP");
      }
    }
  }
  

}
