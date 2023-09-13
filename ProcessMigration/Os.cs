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
            OwnerThread.SetState(ThreadState.WAIT, this);
        }

        private Thread OwnerThread;
        private EventState State;
        private int WaitPerioud;
        private EventWaitHandle ThreadWaitEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        public void Set()
        {
            ThreadWaitEvent.Set();
        }
        public void Reset()
        {
            ThreadWaitEvent.Reset();
        }
    }
    public class Os
    {
        private Os()
        {
            //Boot up OS
            Console.WriteLine("Starting OS ...");
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
            //Init system resource
            Console.WriteLine("[OS]: Initializing system memory...");
            SystemMemoryList = new PageDesc[system.getMemorySizeInPages()];

            //TODO: Page descriptor can contain more the one physical page
            for (int i = 0; i < system.getMemorySizeInPages(); i++)
                if (i % 2 == -0)

                    SystemMemoryList[i] = new PageDesc(system.getMemory(i).GetPageAddress(), 1, PageType.DATA);
                 else
                    SystemMemoryList[i] = new PageDesc(system.getMemory(i).GetPageAddress(), 1, PageType.BSS);

            Console.WriteLine("[OS]: Initializing Worker ...");

            processes = new Dictionary<int, Process>();

            CreateProcess("Worker", ProcessLocation + "\\WorkerProcess.json");
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

        private int systemProcessIdx = 1;

        public int CreateProcess(string name, string executable)
        {
            processes[systemProcessIdx] = new Process(name, 1, executable); // 0 - for system process; 1 - worker process

            Tuple<Thread, ThreadState> systemMainThread = new Tuple<Thread, ThreadState>(new Thread(1), ThreadState.HALT);
            threads.Add(systemMainThread);

            systemMainThread.Item1.AssignToProcess(processes[systemProcessIdx].GetProcessName(), systemProcessIdx);

            systemMainThread.Item1.SetState(ThreadState.RUNNING, processes[systemProcessIdx]);

            return systemProcessIdx++;
        }

        private bool AllocateMemory(PageDesc desc)
        {
            desc.LockDescriptor();
            return true;
        }

        public PageDesc GetNextFreeMemoryDescriptor(PageType type)
        {
            foreach (PageDesc pageDesc in SystemMemoryList) 
            { 
                if (pageDesc.GetDescriptorType() == type)
                {
                    AllocateMemory(pageDesc);
                    return pageDesc;
                }
            }
            return null;
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

        private List<hwCPU> freeCPUs = new List<hwCPU>();
        private PageDesc[] SystemMemoryList;
        private Dictionary<int, Process> processes = new Dictionary<int, Process>();
        private emSystem System;
        private int freeSystemResourceIdx = 0;
        private Dictionary<int, SystemEvent> SystemEventsSet = new Dictionary<int, SystemEvent>();

    }

    //This class actually acts as linker !!!
    public class PageDesc
    {
        //Page size in indexes
        const int PapgeSize = 1;
        public PageDesc(int virtualAddress, int count, PageType type)
        { 

            addr = virtualAddress;
            varset = new Dictionary<int, string>();
            hwMemPage = new hwMemoryPage[count];

            DescriptorLock = false;
            DescType = type;
            for (int i = 0; i < count; i++) 
            {
                hwMemPage[i] = new hwMemoryPage(type);
            }

        }

        private readonly int addr;
        private readonly PageType DescType;
        private hwMemoryPage[] hwMemPage;
        private bool DescriptorLock;

        public void AddData(string data, string var_name)
        {
            varset[lastIdx] = var_name;

            int offset = lastIdx++;

            //Assuming all data will fit one page
            hwMemPage[0].StoreData(offset, data);
        }
        public string GetData(string param0, PageType type)
        {
            if (type != DescType) { return null; }
            int idx = GetMemoryAddress(param0);
            return hwMemPage[0].GetData(addr, idx);
        }

        public void SetData(DataLocation loc, string var_name, string data)
        {
            int idx = GetMemoryAddress(var_name);
            hwMemPage[0].SetData(idx, data);
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
        public PageType GetDescriptorType()
        {
            return DescType;
        }

        public void LockPage(int idx)
        {
            hwMemPage[idx].LockPage();

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
