# BlazorSensorAppNet5

A .Net 5 version update of
[djaus2/BlazorSensorApp](https://github.com/djaus2/SensorBlazor)

## Includes

-   Blazor Server for forwarding telemetry to an Azure IoT Hub
    -   Connection strings required
-   Wasm app for forwarding simulated telemetry via the server.
-   Can similarly on-forward real or simulated telemetry from RPi, Arduino etc
    -   *Arduino app that forwards via a serial port to the Blazor service …
        simulated data at this stage*
        -   *The original BlazorSensorApp solution had an Arduino Http Post
            version of the app. This will be re-added later.*
    -   *.Net console app for desktop and say, RPi, that forwards via Http Post
        to the Blazor service … simulated data at this stage.*
-   Can monitor IoT messages sent from the service (view on Client)
    -   Directly via a transmission log kept on the service.
    -   Via a D2C monitoring of messages to the IoT Hub . (Integration of the
        Quickstarts **ReadD2cMessages** sample app).
        -   **Nb:** Actually runs as a separate app that forwards to the service
            via Http
        -   The D2C sample app remains as a .NetStandard app (couldn’t be ported
            to .Net5).
        -   It forwards message information over Http to the service
-   Can send commands to the device *(whether simulated or not)*
    -   Direct from the client.
    -   Via the IoT Hub: Direct (with Blazor service) integration of the
        Quickstarts **InvokeDeviceMethodApp** sample app.
        -   Service integrates invocation functionality from the Quickstarts
            **SimulatedDeviceWithCommand** sample app for monitoring commands
            submitted via the IoT Hub.
    -   Service maintains a ConcurrentQueue of commands received directly or
        from the hub that is polled by the device.
    -   (Simulated) device apps forward a list of commands at start that the
        Client uses to generate a menu of commands.
