using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
        public Thread(int tid, Process owner)
        {
            Tid = tid;
            Owner = owner;
        }

        private hwCPU current;
        private Dictionary<int, PageDesc> memory;
        private string ProcessName;
        private int Tid;
        private ThreadState currentState;
        public object threadLock = new object();
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
                        Task<int> task = Task.Run(() => process.EntryPoint(this, process.AssignCpu()));
                    }
                    currentState = state;
                    break;
                case ThreadState.WAIT:
                    _event = (SystemEvent)obj;
                    ThreadWaitList.Add(_event);
                    currentState = state;
                    _event.Wait();
                    break;
                case ThreadState.SUSPENDED:
                    ThreadSuspendEvent = new SystemEvent(this, SystemEvent.EventState.NON_SIGNALED, -1);
                    currentState = ThreadState.SUSPENDED;
                    break;
            }
          
        }

        public ThreadState GetState()
        {
            return currentState;
        }

        public string RunInstruction(pseudoInstruction pseudoInstruction)
        {
            string instructionType = pseudoInstruction.GetType().ToString().Substring(pseudoInstruction.GetType().ToString().IndexOf(".") + 1).Trim();
            //OS API executed as monolitic call
            switch (instructionType)
            {
                case "OsInvokeMethod":
                    {
                        int virtualAddress;
                        Os os = Os.GetSingleton();
                        OsInvokeMethod inst = (OsInvokeMethod)pseudoInstruction;

                        switch (inst.Name)
                        {
                            case "os_ConsolePrint":

                                virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);
                                Console.WriteLine("[{0}(pid:{1} tid{2})]:{3}", Owner.GetProcessName(), Owner.GetPId(), current.GetTid() , current.LoadData(virtualAddress));
                                break;
                            case "os_CreateProcess":

                                virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);
                                int pid = os.CreateProcess(inst.Param1.Substring(0, inst.Param1.IndexOf(".")).Trim(), Os.ProcessLocation + inst.Param1, emMemory.GetSingleTone().AllocatePageHive(1024));
                                current.StoreData(virtualAddress, pid.ToString());
                                
                                break;
                            case "os_MonitorLoad":

                                virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);
                                int heavyLoadedPid = Int32.Parse(current.LoadData(virtualAddress));
                                Process ownerProcess = os.GetProcessById(Owner.GetPId());

                                ownerProcess.MonitorLoad(heavyLoadedPid, inst.Param1);
                                break;
                            case "os_CreateEvent":

                                virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);
                                int eventId = os.CreateSyncEvent(this, SystemEvent.EventState.NON_SIGNALED, -1);
                                current.StoreData(virtualAddress, eventId.ToString());

                                break;
                            case "os_WaitEvent":
                                virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);

                                SystemEvent _event =  os.GetEvent(Int32.Parse(current.LoadData(virtualAddress)));

                                SetState(ThreadState.WAIT, _event);

                                break;
                            case "Exit":
                                Console.WriteLine("[{0}(pid:{1})]:Process exit ", ProcessName, Owner.GetPId(), inst.Param0);
                                return "Halt";

                            case "MainLoop":
                                Console.WriteLine("[{0}(pid:{1})]: System up and running", ProcessName, Owner.GetPId(), inst.Param0);
                                return "MainLoop";

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
                                        int virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);
                                        current.StoreData(virtualAddress, inst.Param1);
                                        break;
                                }
                                break;
                            case "cmp":
                                switch (inst.Operand)
                                {
                                    case "indirect_while_less":

                                        int virtualAddress = Owner.GetHive(inst.Type).GetDataVirtualAddress(inst.Param0);
                                        int counter = Int32.Parse(current.LoadData(virtualAddress));
                                        int condition = Int32.Parse(inst.Param1);

                                        if (counter < condition)
                                        {
                                            current.StoreData(virtualAddress, (++counter).ToString());
                                            return inst.Param2;
                                        }
                                        return "Fetch";

                                }
                                break;


                        }
                    break;
                    }
            }

            return "Fetch";
        }

        private List<SystemEvent> ThreadWaitList = new List<SystemEvent>();
        private SystemEvent ThreadSuspendEvent;

        private Process Owner;

        internal void WaitForEvent()
        {

            foreach (SystemEvent _event in ThreadWaitList)
            {
                _event.Wait();
            }
        }

        internal void SuspendForEvent(Thread thread)
        {
            ThreadSuspendEvent.Wait();
        }

        internal Process GetOwnerProcces()
        {
            return Owner;
        }

        internal hwCPU GetCurrentCPU()
        {
            //TODO: IT is possible that currently no CPU assigned to Thread
            if (current == null)
            {
                throw new NotImplementedException();
            }
            return current;
        }
    }
}
