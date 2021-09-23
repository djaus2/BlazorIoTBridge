# BlazorSensorAppNet5
A .Net 5 version update of  [djaus2/BlazorSensorApp](https://github.com/djaus2/SensorBlazor)

## Includes
- Blazor Server for forwarding telemetry to an Azure IoT Hub
  - Connection string required
- Wasm app for forwarding simuated telemetry via the server.
- Can similarly forward real telemetry from RPi, Arduino etc
    - _Arduino app to be added  to be added from previous repository._
    - _.Net console apps for say, RPi to be added from previous repository._
- This version has added a Serial2Blazor Console app for forwarding telemetry from a device suchas an arduino over serial port to the Blazor server and then to the Hub.
  - Enables Arduino without WiFi/Ethernet/Bluetooth to use dev machine via USB Com port.
