#define Id  "6513d5-c0f2-4346-b3fa-642c48fd66a5"


//Delay after POST done in seconds
#define DELAY  5

String DoSensor(int sensorNo);
//void ReadSensor( int sensorNo);
String GetAction(String json, int * parameter);

#define INITIAL_MESSAGE "* Begin"
#define INITIAL_COMMAND "STOP"
#define BAUD 57600
#define INITIAL_RATE 6000
#define JSON_OPEN_BRACKET_TIMEOUT   1000
#define JSON_CLOSE_BRACKET_TIMEOUT  5000
#define SERIAL_BUFFER_FILL_PAUSE 10
#define ACK '#'
#define COMMAND_LIST "READ,STOP,PAUSE,*RATE,FAST,SLOW,START,RESET,RESTART,CONTINUE"

