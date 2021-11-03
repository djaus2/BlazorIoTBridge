using System.Linq;

using BlazorIoTBridge.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace BlazorIoTBridge.Server.Data
{

    public class DataAccessService : IDataAccessService
    {
        private IConfiguration config;
        public DataAccessService(IConfiguration configuration)
        {
            config = configuration;
            Clear();
            //Guid g = new Guid("6513d5ed-c0f2-4346-b3fa-642c48fd66a5");
            //DeviceIds.Add(g);
            //Devices.Add(g, new Device { Id = g, settings = new Info()});

        }

        public List<Guid> DeviceIds { get; set; }

        public Dictionary<Guid, Device> Devices { get; set; }

        public void Clear()
        {
            Devices = new Dictionary<Guid, Device>();
            DeviceIds = new List<Guid>();
            Reset(true);
        }

        public bool Started { get; set; } = false;

        public void Reset(bool state)
        {
            PostLog = new List<Sensor>();
            Started = state;
        }

        public void Register(Guid id)
        {
            Device device = new Device(id);
            Devices.Add(id, device);
        }

        public void DeRegister(Guid id)
        {
            Devices.Remove(id);
        }

        private List<Sensor> PostLog { get; set; } = null;

        public int LogSensor(Sensor sensor)
        {
            if (PostLog == null)
            {
                PostLog = new List<Sensor>();
            }
            PostLog.Add(sensor);
            return PostLog.Count();
        }

        public List<Sensor> GetLogs()
        {
            return PostLog;
        }

        public string GetInfo()
        {
            return "137";
        }

        static int status { get; set; }

        private static object StatusMonitor = new object();
        public static int Status
        {
            get
            {
                int stat;
                Monitor.Enter(StatusMonitor);
                stat = status;
                Monitor.Exit(StatusMonitor);
                return stat;
            }
            set
            {
                Monitor.Enter(StatusMonitor);
                status = value;
                Monitor.Exit(StatusMonitor);
            }
        }

        public int GetStatus()
        {
            return Status;
        }

        public void SetStatus(int val)
        {
            Status = val;
        }

        public void EnqueueCommand(Guid id, Command cmd)
        {
            if (!Devices.Keys.Contains(id))
                Devices.Add(id, new Device(id));
            Devices[id].EnqueueCommand(cmd);
        }
        public Command DequeueCommand(Guid id)
        {
            if (Devices.Keys.Contains(id))
                return Devices[id].DequeueCommand();
            else
                return null;
        }

        //public List<Command> GetAddedCommandsLog()
        //{
        //    List<Command> cmds = new List<Command>();
        //    Command cmd;
        //    while (Commands.Count() != 0)
        //    {
        //        if (Commands.TryDequeue(out cmd))
        //            cmds.Add(cmd);
        //        else
        //            break;
        //    }
        //    if (cmds.Count() != 0)
        //        System.Diagnostics.Debug.WriteLine("Len {0}", cmds.Count());
        //    cmds.Reverse();
        //    return cmds;
        //}
        public List<Command> GetCommands()
        {
            List<Command> cmds = new List<Command>();
            foreach (Guid dev in Devices.Keys)
            {
                if (Devices[dev] != null)
                    cmds.AddRange(Devices[dev].CommandsToArray());
            }
            return cmds;
        }

        public int CommandsCount()
        {
            int count = 0;
            foreach (Guid dev in Devices.Keys)
            {
                count += Devices[dev].CommandsCount();
            }
            return count;
        }

        public Command[] CommandsToArray()
        {
            List<Command> cmds = new List<Command>();
            foreach (Guid dev in Devices.Keys)
            {
                cmds.AddRange(Devices[dev].CommandsToArray());
            }
            return cmds.ToArray();
        }
    }

    public class Device : IDevice
    {
        public Device()
        {
            Reset(true);
        }
        public Device(Guid id)
        {
            Id = id;
            Reset(true);
        }
        //public Device(Guid id, Info _settings)
        //{
        //    Id = id;
        //    Reset(true);
        //    //settings = _settings;
        //}
        public Guid Id { get; set; }


        //public Info settings { get; set; }

        public void Reset(bool state)
        {

            Commands = new ConcurrentQueue<Command>();
            Commands2 = new ConcurrentQueue<Command>();

        }

        private ConcurrentQueue<Command> Commands { get; set; } = null;
        private ConcurrentQueue<Command> Commands2 { get; set; } = null;

        public void EnqueueCommand(Command cmd)
        {
            Commands.Enqueue(cmd);
        }

        public Command DequeueCommand()
        {
            Command cmd;
            if (Commands.TryDequeue(out cmd))
                return cmd;
            else
                return null;
        }

        //public List<Command> GetAddedCommandsLog()
        //{
        //    List<Command> cmds = new List<Command>();
        //    Command cmd;
        //    while (Commands.Count() != 0)
        //    {
        //        if (Commands.TryDequeue(out cmd))
        //            cmds.Add(cmd);
        //        else
        //            break;
        //    }
        //    if (cmds.Count() != 0)
        //        System.Diagnostics.Debug.WriteLine("Len {0}", cmds.Count());
        //    cmds.Reverse();
        //    return cmds;
        //}
        public List<Command> GetCommands()
        {
            return Commands.ToList(); ;
        }

        public int CommandsCount()
        {
            return Commands.Count();
        }

        public Command[] CommandsToArray()
        {
            return Commands.ToArray();
        }
    }


}
