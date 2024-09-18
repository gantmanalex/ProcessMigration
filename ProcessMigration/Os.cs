using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMigration
{
    public enum DataLocation
    {
        BSS,
        HEAP
    }

    public class SystemEvent
    {
        public enum EventState
        {
            SIGNALED,
            NON_SIGNALED
        }

        public SystemEvent(Thread thread, EventState state, int wait)
        {
            OwnerThread = thread;
            State = state;
            WaitPerioud = 0;
        }

        public void Wait()
        {
            lock (ThreadWaitEvent)
            {
                Monitor.Wait(ThreadWaitEvent, 1000 + WaitPerioud);
            }
        }

        private Thread OwnerThread;
        private EventState State;
        private int WaitPerioud;
        private EventWaitHandle ThreadWaitEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        private object test = new object();
        public void Set()
        {

            OwnerThread.SetState(ThreadState.RUNNING, null);

            // Monitor.Enter(ThreadWaitEvent);
        }
        public void Reset()
        {
            lock (ThreadWaitEvent)
            {
                Monitor.Pulse(ThreadWaitEvent);
            }
        }
    }
    public class Os
    {
        private Os()
        {
            //Boot up OS
            Console.WriteLine("Starting OS ...");

            //Adding CPUs
            freeCPUs.Add(new hwCPU(0));
            freeCPUs.Add(new hwCPU(1));
            freeCPUs.Add(new hwCPU(1));
        }

        private static readonly Os os = new Os();

        public static Os GetSingleton()
        {
            return os;
        }

        public const string ProcessLocation =  "C:\\Users\\alexgantman\\source\\repos\\ProcessMigration\\ProcessMigration\\";
        public void MainThread(emSystem system)
        {
            System = system;
            Console.WriteLine("[OS]: Running OS Main Thread ...");


            Console.WriteLine("[OS]: Collecting system CPUs...");

            Console.WriteLine("[OS]: Initializing Worker ...");

            processes = new Dictionary<int, Process>();


            CreateProcess("System", ProcessLocation + "\\system.json", emMemory.GetSingleTone().AllocatePageHive(1024));

            CreateProcess("Worker", ProcessLocation + "\\WorkerProcess.json", emMemory.GetSingleTone().AllocatePageHive(1024));
        }

        internal hwCPU FindFreeCPU(int processId)
        {
            if (freeCPUs.Count != 0)
            {
                hwCPU hwCpu = freeCPUs[0];

                freeCPUs.RemoveAt(0);
                return hwCpu;
            }
            return null;
        }

        internal void CallApi(string name, string param0, int size)
        {
            throw new NotImplementedException();
        }

        internal void CallApi(string name, int code)
        {
            throw new NotImplementedException();
        }

        private int systemProcessIdx = 0;
        private int systemThreadIdx = 0;

        public int CreateProcess(string name, string executable, emMemory.hwMemory _physicalMemory, bool createSuspended = false)
        {
            processes[systemProcessIdx] = new Process(name, systemProcessIdx, executable, _physicalMemory); // 0 - for system process; 1 - worker process

            if (systemProcessIdx == 0) //System main thread
            {
                systemMainThread = new Tuple<Thread, ThreadState>(new Thread(systemThreadIdx++, processes[systemProcessIdx]), ThreadState.HALT);
                threads.Add(systemMainThread);
                processes[systemProcessIdx].CreateMainThread(systemMainThread.Item1);
                systemMainThread.Item1.SetState(ThreadState.RUNNING, processes[systemProcessIdx]);

            }
            else
            {
                Tuple<Thread, ThreadState> processMainThread = new Tuple<Thread, ThreadState>(new Thread(systemThreadIdx++, processes[systemProcessIdx]), ThreadState.HALT);
                threads.Add(processMainThread);
                processes[systemProcessIdx].CreateMainThread(processMainThread.Item1);

                if (!createSuspended)
                    processMainThread.Item1.SetState(ThreadState.RUNNING, processes[systemProcessIdx]);
            }

            return systemProcessIdx++;
        }


        internal Process GetProcessById(int pid)
        {
            return processes[pid];
        }

        //System resourtces
        private List<Tuple<Thread, ThreadState>> threads = new List<Tuple<Thread, ThreadState>>();

        public SystemEvent GetEvent(int systemResoureId)
        {
            return SystemEventsSet[systemResoureId];
        }

        public int CreateSyncEvent(Thread thread, SystemEvent.EventState state, int wait)
        {
            SystemEventsSet[freeSystemResourceIdx] = new SystemEvent(thread, state, wait);

            return freeSystemResourceIdx++;

        }

        //This function will clone process for migration
        public int CloneProcess(int processId)
        {
            Process origin = GetProcessById(processId);
            SuspendProcess(origin);
            int  cloneId = CreateProcess(origin.Name + "#1", "",  emMemory.GetSingleTone().AllocatePageHive(1024), false);
            Process clone = GetProcessById(cloneId);

           Rdma.CopyProcess(origin, clone);

            return clone.GetPId();

        }

        private void SuspendProcess(Process process)
        {
            foreach(Thread thread in process.GetThreads())
            {
                thread.SetState(ThreadState.SUSPENDED , null);
            }
        }

        private Tuple<Thread, ThreadState> systemMainThread;
        private List<hwCPU> freeCPUs = new List<hwCPU>(2);
        private Dictionary<int, Process> processes = new Dictionary<int, Process>();
        private emSystem System;
        private int freeSystemResourceIdx = 0;
        private Dictionary<int, SystemEvent> SystemEventsSet = new Dictionary<int, SystemEvent>();

    }

    //This class actually acts as linker !!!
    public class PageDesc
    {
        //Page control Block
        internal class PCB
        {
            public PCB(bool _mapped, bool _valid) {
                mapped = _mapped;
                valid = _valid;
                durty = false;
            }

            public void SetDurty()
                { durty = true; }

            bool mapped;
            bool valid;
            private bool durty;
        }

        public enum DescType 
        {
            None = 0,
            Code = 1,
            BSS = 2,
            Heap = 3,
            Stack    
        }
        //Page size in indexes
        const int PapgeSize = 1;
        public PageDesc(int virtualAddress, int count, DescType _type)
        {

            Range = new Dictionary<int, PCB>(count);
            Entries = new Dictionary<int, DataElement>();

            head_VirtualAddress = virtualAddress;

            DescriptorLock = false;
            type = _type;

        }

        public Dictionary<int, DataElement> GetEntries()
        {
            return Entries;
        }

        private Dictionary<int, PCB> Range;
        private Dictionary<int, DataElement> Entries;

        private readonly int head_VirtualAddress;

        private readonly DescType type;
        private bool DescriptorLock;

        public void AddData(int virtualAddress, DataElement element)
        {

            if (Entries.ContainsKey(virtualAddress))
            {
                Entries[virtualAddress] = element;
                Range[virtualAddress] = new PCB(true, true);
            }
            else
            {
                Entries.Add(virtualAddress, element);
                Range.Add(virtualAddress,new PCB(true, true));

            }
        }

        public bool DeleteData(int virtualAddress)
        {

            if (Entries.ContainsKey(virtualAddress))
            {
                Entries[virtualAddress] = null;
                Range.Remove(virtualAddress);
                return true;
            }
            return false;
        }

        public int GetDataVirtualAddress(string name)
        {
            //Will owrk for Heap and BSS Hives only !!!
            foreach (memoryLocatedData element in Entries.Values)
            {
                if (element.Name == name)
                {
                    return element.GetPhisicalAddress();
                }
            }
            return 0;
        }
     

        public int GetMemoryAddress(string var_name)
        {
            for (int i=0; i< lastIdx; i++)
            {
                if (varset[i] == var_name)
                {
                    return i;
                }
            }
            return -1;
        }
        public DescType GetDescriptorType()
        {
            return type;
        }

        public void LockPage(int virtualAddress)
        {
            Range[virtualAddress].SetDurty();

        }

        public void LockDescriptor()
        {
            DescriptorLock = true;
        }

        public bool isContainVariable(string param0)
        {
            foreach (var item in varset) 
            {
                if (item.Value == param0)
                {
                    return true;
                }
            }
            return false;
        }

        // This map acts like a linker converts variable name to it "address" 
        // which is actually int in data disctionary
        private Dictionary<int, string> varset;
        private int lastIdx = 0;
    }
}
