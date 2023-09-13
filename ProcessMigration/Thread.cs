using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMigration
{
    public enum ThreadState
    {
        SUSPENDED,
        RUNNING,
        WAIT,
        HALT
    };

    public class Thread
    {
        public Thread(int tid)
        {
            Tid = tid;
        }

        public void AssignToProcess(string hostringProcessName, int pid)
        {
            ProcessName = hostringProcessName;
            Pid = pid;
        }

        private hwCPU current;
        private Dictionary<int, PageDesc> memory;
        private string ProcessName;
        private int Pid;
        private int Tid;
        private ThreadState currentState;
        public int tid { 
            get  { return Tid; } }

        internal void AssingCPU(hwCPU cpu)
        {
            current = cpu;
        }
        internal void AssingMemory(Dictionary<int, PageDesc> memlist)
        {
            memory = memlist;
        }
        public void SetState(ThreadState state, object obj)
        {
            SystemEvent _event;

            switch (state)
            {
                case ThreadState.RUNNING:

                    if (currentState != ThreadState.WAIT)
                    {
                        Process process = (Process)obj;
                        Task<int> task = Task.Run(() => process.EntryPoint(this));
                    }
                    break;
                case ThreadState.WAIT:
                    _event = (SystemEvent)obj;
                    _event.Set();
                    ThreadWaitList.Add(_event);
                    break;
            }
            currentState = state;
        }

        public ThreadState GetState()
        {
            return currentState;
        }
        public Tuple<PageDesc , string> GetDataDesc(string var_name, PageType type)
        {
            foreach (var mem in memory)
            {
                string data = mem.Value.GetData(var_name, type);
                if (data != null)
                {
                    return new Tuple<PageDesc, string> (mem.Value, data);
                }

            }
                return null;
        }

        public string RunInstruction(pseudoInstruction pseudoInstruction)
        {
            string instructionType = pseudoInstruction.GetType().ToString().Substring(pseudoInstruction.GetType().ToString().IndexOf(".") + 1).Trim();
            //OS API executed as monolitic call
            switch (instructionType)
            {
                case "OsInvokeMethod":
                    {
                        Os os = Os.GetSingleton();
                        OsInvokeMethod inst = (OsInvokeMethod)pseudoInstruction;
                        Tuple<PageDesc, string> var;

                        switch (inst.Name)
                        {
                            case "os_ConsolePrint":

                                var = GetDataDesc(inst.Param0, PageType.BSS);

                                Console.WriteLine("[{0}(pid:{1})]:{2}", ProcessName, Pid, var.Item2);
                                break;
                            case "os_CreateProcess":

                                var = GetDataDesc(inst.Param0, PageType.DATA);

                                int pid = os.CreateProcess(inst.Param1.Substring(0, inst.Param1.IndexOf(".")).Trim(), Os.ProcessLocation + inst.Param1);

                                var.Item1.SetData(DataLocation.HEAP, inst.Param0, pid.ToString());
                                
                                break;
                            case "os_MonitorLoad":

                                var = GetDataDesc(inst.Param0, PageType.DATA);
                                int heavyLoadedPid = Int32.Parse(var.Item2);
                                Process ownerProcess = os.GetProcessById(Pid);

                                ownerProcess.MonitorLoad(heavyLoadedPid, inst.Param1);
                                break;
                            case "os_CreateEvent":
                                   
                                var = GetDataDesc(inst.Param0, PageType.DATA);
                                int eventId = os.CreateSyncEvent(this, SystemEvent.EventState.NON_SIGNALED, -1);
                                var.Item1.SetData(DataLocation.HEAP, inst.Param0, eventId.ToString());

                                break;
                            case "os_WaitEvent":
                                var = GetDataDesc(inst.Param0, PageType.DATA);

                                SystemEvent _event =  os.GetEvent(Int32.Parse(var.Item2));

                                SetState(ThreadState.WAIT, _event);

                                break;
                            case "Exit":
                                Console.WriteLine("[{0}(pid:{1})]:Process exit ", ProcessName, Pid, inst.Param0);
                                return "Halt";

                        }

                        break;
                    }
                case "cpuInstruction":
                    {
                        cpuInstruction inst = (cpuInstruction)pseudoInstruction;
                        switch (inst.Name)
                        {
                            case "mov":
                                switch (inst.Operand)
                                {
                                    case "indirect_to":
                                        String data = null;

                                        //Find variable in memory (Emulating Linker work)
                                        foreach (var mem in memory)
                                        {
                                            PageDesc desc = mem.Value;
                                            if (desc.isContainVariable(inst.Param0)) 
                                            { 
                                                desc.SetData(DataLocation.HEAP, inst.Param0, inst.Param1);
                                                break;
                                            }
                                        }

                                        break;
                                }
                                break;
                            case "cmp":
                                switch (inst.Operand)
                                {
                                    case "indirect_while_less":
                                        //Find variable in memory (Emulating Linker work)
                                        foreach (var mem in memory)
                                        {
                                            PageDesc desc = mem.Value;
                                            if (desc.isContainVariable(inst.Param0))
                                            {
                                                int counter = Int32.Parse(desc.GetData(inst.Param0, PageType.DATA));
                                                int condition = Int32.Parse(inst.Param1);

                                                if (counter < condition)
                                                {
                                                    desc.SetData(DataLocation.HEAP, inst.Param0, (++counter).ToString());
                                                    return inst.Param2;
                                                }
                                                return "Fetch";
                                            }
                                        }

                                        break;
                                }
                                break;


                        }
                    break;
                    }
            }

            return "Fetch";
        }

        private List<SystemEvent> ThreadWaitList = new List<SystemEvent>();

        internal void WaitForEvent()
        {
            foreach(SystemEvent _event in ThreadWaitList)
            {
                _event.Wait();
            }
        }
    }
}
