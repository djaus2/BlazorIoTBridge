using BlazorIoTBridge.Shared;
using System;
using System.Collections.Generic;

namespace BlazorIoTBridge.Server.Data
{
    public interface IDevice
    {
        Guid Id { get; set; }
        //Info settings { get; set; }

        int CommandsCount();
        Command[] CommandsToArray();
        Command DequeueCommand();
        void EnqueueCommand(Command cmd);
        //List<Command> GetAddedCommandsLog();
        List<Command> GetCommands();
        void Reset(bool state);
    }
}