using BlazorIoTBridge.Shared;
using System;
using System.Collections.Generic;

namespace BlazorIoTBridge.Server.Data
{
    public interface IDataAccessService
    {
        Dictionary<Guid, Device> Devices { get; set; }
        bool Started { get; set; }

        List<Guid> DeviceIds { get; set; }
        void Clear();
        int CommandsCount();
        Command[] CommandsToArray();
        Command DequeueCommand(Guid id);
        void DeRegister(Guid id);
        void EnqueueCommand(Guid id, Command cmd);
        List<Command> GetCommands();
        string GetInfo();
        List<Sensor> GetLogs();
        int GetStatus();
        int LogSensor(Sensor sensor);
        void Register(Guid id);
        void Reset(bool state);
        void SetStatus(int val);
    }
}