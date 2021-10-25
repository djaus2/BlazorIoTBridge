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
    public interface IDataAccessService
    {
        string GetInfo();
        int LogSensor(Sensor sensor);
        List<Sensor> GetLogs();
        void Reset(bool state);
        int GetStatus();
        void SetStatus(int val);
        void EnqueueCommand(Command cmd);
        Command DequeueCommand();

        bool Started { get; set; }

        int CommandsCount();
        //Command[] CommandsToArray();

        List<Command> GetAddedCommandsLog();
        List<Command> GetCommands();

    }
    public class DataAccessService: IDataAccessService
    {
        private IConfiguration config;
        public DataAccessService(IConfiguration configuration)
        {
            config = configuration;
            Reset(true);
        }

        public bool Started { get; set; } = false;

        public void Reset(bool state)
        {
            PostLog = new List<Sensor>();
            Commands = new ConcurrentQueue<Command>();
            Commands2 = new ConcurrentQueue<Command>();
            Started = state;
        }

        private List<Sensor> PostLog { get; set; } = null;
        private ConcurrentQueue<Command> Commands { get; set; } = null;
        private ConcurrentQueue<Command> Commands2 { get; set; } = null;

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

        public List<Command> GetAddedCommandsLog()
        {
            List<Command> cmds = new List<Command>();
            Command cmd;
            while (Commands.Count() != 0)
            {
                if (Commands.TryDequeue(out cmd))
                    cmds.Add(cmd);
                else
                    break;
            }
            if (cmds.Count() != 0)
                System.Diagnostics.Debug.WriteLine("Len {0}", cmds.Count());
            cmds.Reverse();
            return cmds;
        }
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
