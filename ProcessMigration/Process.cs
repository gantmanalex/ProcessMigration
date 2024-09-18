using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProcessMigration
{
   
    public class DataElement
    {
        private int physicalAddress;
        public DataElement(int _physicalAddress)
        {  physicalAddress = _physicalAddress; }
        public int GetPhisicalAddress() { return physicalAddress; }
       
    }
    public class pseudoInstruction: DataElement
    {
        public pseudoInstruction(int _physicalAddress) : base(_physicalAddress)
        {
        }

        public string Name { get; set; }
        public virtual bool Execute(Thread thread, PageDesc desc)  
        { return false;}
        
    }
    public class OsInvokeMethod : pseudoInstruction
    {
        public OsInvokeMethod(int _physicalAddress) : base(_physicalAddress)
        {
        }

        public string Param0 { get; set; }

        public string Param1 { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }

        public new virtual bool Execute(Thread thread, PageDesc desc)
        {
            Os os = Os.GetSingleton();
            os.CallApi(Name, Param0, Size);
            return true; 
        }
    }

    public class Exit: pseudoInstruction
    {
        public Exit(int _physicalAddress) : base(_physicalAddress)
        {
        }

        public int Code { get; set; }

        public new virtual bool Execute(Thread thread, PageDesc desc)
        {
            Os os = Os.GetSingleton();
            os.CallApi(Name, Code);
            return true;
        }
    }

    public class cpuInstruction : pseudoInstruction
    {
        public cpuInstruction(int _physicalAddress) : base(_physicalAddress)
        {
        }

        public string Param0 { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }

        public string Operand { get; set; }
        public string Type { get; set; }
        public int Size { get; set; }

        public new virtual bool Execute(Thread thread, PageDesc desc)
        {
            thread.RunInstruction(this);
            return true;
        }
    }

    public class memoryLocatedData : DataElement
    {
        public string Name;
        public string Source;
        public int Size;

        public memoryLocatedData(int _physicalAddress) : base(_physicalAddress)
        {
        }
    }

    public class pseudocode_MainThread
    {
        
    }
    public class Process
    {

        public Process(string name, int pid, string binary, emMemory.hwMemory _physicalMemory)
        {
            Name = name;
            Pid = pid;
            Console.WriteLine("Process \"{0}\" created ", name);


            orderedInstructionList = new Dictionary<string, pseudoInstruction>();
            threads = new List<Thread>();
            DataList = new List<memoryLocatedData>();
            BssList = new List<memoryLocatedData>();
            CodeList = new List<pseudoInstruction>();

            //Currently process will get 1Kb virtual address space
            ProcessHeapPhy = head_ProcessHeapPhy;
            ProcessCodePhy = head_ProcessCodePhy;
            ProcessBSSPhy = head_ProcessBSSPhy;
            ProcessStackPhy = head_ProcessStackPhy;

            ProcessBSS = new Tuple<int, int>(ProcessBSSPhy, 100);
            ProcessHeap = new Tuple<int, int>(ProcessHeapPhy, 1023);
            ProcessStack = new Tuple<int, int>(ProcessStackPhy, 1000);
            ProcessCode = new Tuple<int, int>(ProcessCodePhy, 1024);


            emMemory.hwMemory physicalMemory = _physicalMemory;
            physicalMemorycurrPtr = 0;

            Os os = Os.GetSingleton();

            /*            PageDesc currMemPage = os.GetNextFreeMemoryDescriptor(PageType.DATA);
                        pages[LastFreePageIdx] = currMemPage;
                        LastFreePageIdx++;

                        PageDesc currBssPage = os.GetNextFreeMemoryDescriptor(PageType.BSS);
                        pages[LastFreePageIdx] = currBssPage;
                        LastFreePageIdx++;
            */

            //mainThread = new Thread("System", Pid);
            //mainThread.AssingMemory(pages);

            dynamic worker = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(binary))[Name];

            foreach (var segments in worker)
            {
                foreach (var segment in segments)
                {
                    foreach (var range in segment)
                        foreach (var offsets in range)
                            foreach (var opCode in offsets)
                                foreach (var instruction in opCode)
                                {
                                    switch (segment.Name)
                                    {
                                        case "CS":
                                            switch (instruction.opCode.ToString())
                                            {
                                                case "OS":
                                                    {
                                                        OsInvokeMethod method = new OsInvokeMethod(ProcessCodePhy);
                                                        physicalMemory.Write(ProcessCodePhy++, method);
                                                        switch (instruction.Name.ToString())
                                                        {

                                                            case "os_ConsolePrint":

                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Param0;
                                                                method.Type = instruction.Type;
                                                                method.Size = instruction.Size;

                                                                break;
                                                            case "os_CreateProcess":
                                                            case "os_MonitorLoad":
                                                            case "os_CreateEvent":
                                                            case "os_WaitEvent":

                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Param0;
                                                                method.Param1 = instruction.Param1;
                                                                method.Type = "Memory";
                                                                method.Size = instruction.Size;

                                                                break;
                                                            case "Exit":
                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Code;
                                                                break;
                                                            case "MainLoop":
                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Code;
                                                                break;

                                                        }
                                                        orderedInstructionList[opCode.Name] = method;
                                                        CodeList.Add(method);
                                                        break;
                                                    }
                                                case "CPU":
                                                    {
                                                        cpuInstruction method = new cpuInstruction(ProcessCodePhy);
                                                        physicalMemory.Write(ProcessCodePhy++, method);
                                                        switch (instruction.Name.ToString())
                                                        {
                                                            case "mov":
                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Param0;
                                                                method.Param1 = instruction.Param1;
                                                                method.Operand = instruction.Operand;
                                                                method.Type = instruction.Type;
                                                                method.Size = instruction.Size;
                                                                break;

                                                            case "cmp":
                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Param0;
                                                                method.Param1 = instruction.Param1;
                                                                method.Param2 = instruction.Param2;
                                                                method.Operand = instruction.Operand;
                                                                method.Type = instruction.Type;
                                                                break;

                                                        }
                                                        orderedInstructionList[opCode.Name] = method;
                                                        CodeList.Add(method);
                                                        break;
                                                    }
                                            }
                                            break;
                                        case "BSS":
                                            memoryLocatedData data = new memoryLocatedData(ProcessBSSPhy);
                                            physicalMemory.Write(ProcessBSSPhy++, data);
                                            data.Name = instruction.Name;
                                            data.Source = instruction.Source;
                                            data.Size = instruction.Size;
                                            BssList.Add(data);
                                            break;
                                        case "Memory":
                                            memoryLocatedData heapData = new memoryLocatedData(ProcessHeapPhy);
                                            physicalMemory.Write(ProcessHeapPhy++, heapData);
                                            heapData.Name = instruction.Name;
                                            heapData.Source = instruction.Source;
                                            heapData.Size = instruction.Size;
                                            DataList.Add(heapData);
                                            break;

                                    }
                                    //Console.WriteLine("Segment {0} Instruction {1}", segment.Name, instruction.Name);
                                }
                }

            }
            // Create Descriptors
            codeHiveDesc = new PageDesc(head_ProcessCodePhy, ProcessCodePhy - head_ProcessCodePhy, PageDesc.DescType.Code);
            dataHiveDesc = new PageDesc(head_ProcessCodePhy, ProcessCodePhy - head_ProcessCodePhy, PageDesc.DescType.Heap);
            bssHiveDesc = new PageDesc(head_ProcessCodePhy, ProcessCodePhy - head_ProcessCodePhy, PageDesc.DescType.BSS);

            int virtualAddress = head_ProcessCodePhy;
            //Map virutal Addess space
            foreach (pseudoInstruction instruction in CodeList)
            {
                codeHiveDesc.AddData(virtualAddress++, instruction);
            }

            virtualAddress = head_ProcessHeapPhy;
            foreach (memoryLocatedData data in DataList)
            {
                dataHiveDesc.AddData(virtualAddress++, data);
            }

            virtualAddress = head_ProcessBSSPhy;
            foreach (memoryLocatedData data in BssList)
            {
                bssHiveDesc.AddData(virtualAddress++, data);
            }


        }

        public void MonitorLoad(int offloadProcessId, string systemEventVar) 
        {
            //Attaching Profiler
            Console.WriteLine("Attaching heavy load proffiler ...");

            ProfilerEnabled = offloadProcessId;
            ProfilerSyncEventVar = systemEventVar;

        }

        public void CreateMainThread(Thread thread)
        {
            Os os = Os.GetSingleton();

            mainThread = thread;
            threads.Add(mainThread);
            mainThread.AssingMemory(pages);

        }

        public async Task<int> EntryPoint(Thread thread, hwCPU _cpu)
        {
            const int MAX_LOAD_VALUE = 5;

            thread.AssingCPU(_cpu);
            //Proccess Entry point
            _cpu.SetIP(head_ProcessCodePhy);

            await Task.Delay(1000);

            int Threshold = 0;
            Os os = Os.GetSingleton();

            do
            {
                
                if (thread.GetState() == ThreadState.WAIT)
                {
                    Console.WriteLine("[{0}(pid:{1})]: Entering wait state", Name, Pid, thread.tid);
                    thread.WaitForEvent();
                    Console.WriteLine("[{0}(pid:{1})]: Exiting wait state", Name, Pid, thread.tid);
                }

                if (thread.GetState() == ThreadState.SUSPENDED)
                {
                    Console.WriteLine("[{0}(pid:{1})]: Entering suspend state", Name, Pid, thread.tid);
                    thread.SuspendForEvent(thread);
                    Console.WriteLine("[{0}(pid:{1})]: Exiting suspend state", Name, Pid, thread.tid);
                }

                //currentState
                //string nextIp = mainThread.RunInstruction(orderedInstructionList["Offset#" + idxCurrentInstruction]);
                int previousInstruction = _cpu.GetIP();

                string nextIp = mainThread.RunInstruction((pseudoInstruction)_cpu.Fetch());

                switch (nextIp)
                {
                    case "MainLoop":
                        while (true)
                        {
                            Task.Delay(1000);
                        }
                    case "Halt":
                        goto _Out;
                    case "Fetch":
                        break;
                    default:
                        _cpu.SetIP(FindNextEntryPoint(nextIp));
                        break;
                }
                //TODO: Profiler to detect heavy load
                if (ProfilerEnabled != 0)
                    if (_cpu.GetIP() < previousInstruction)
                    {
                        Threshold++;
                        if (Threshold == MAX_LOAD_VALUE)
                        {
                            Console.WriteLine("Process[{0}]:Profiler - Worker #{1} intensive CPU usage detected", Pid, mainThread.tid);

                            Process heavyLoader = os.GetProcessById(ProfilerEnabled);
                            Thread loaderMainThread = heavyLoader.GetMainThread();

                            
                            //Check if MainThread waing for Load 
                            if (loaderMainThread == null || loaderMainThread.GetState() != ThreadState.WAIT)
                            {
                                Console.WriteLine("Process[{0}]:Profiler - Loader process not read yet,", Pid, mainThread.tid);
                                do
                                {
                                    loaderMainThread = heavyLoader.GetMainThread();
                                    if (loaderMainThread != null && loaderMainThread.GetState() == ThreadState.WAIT) 
                                    { 
                                        break; 
                                    }
                                    Console.WriteLine("Process[{0}]:Profiler - wating for HeavyLoader process", Pid, mainThread.tid);
                                    await Task.Delay(1000);
                                } while (true);

                            }

                            Console.WriteLine("Process[{0}]:Profiler - offloading Thread[{3}] to Process[{2}],", Pid, mainThread.tid, heavyLoader.Pid, mainThread.tid);


                            int virtualAddress = heavyLoader.GetHive("Memory").GetDataVirtualAddress(ProfilerSyncEventVar);
                            os.GetEvent(Int32.Parse(loaderMainThread.GetCurrentCPU().LoadData(virtualAddress))).Reset();
                        }
                    }
            } while (true) ;
  _Out:          
            return 0;
        }

        public PageDesc GetHive(string pageType)
        {
            switch (pageType)
            {
                case "BSS":
                    return bssHiveDesc;
                case "Memory":
                    return dataHiveDesc;
                case "CS":
                    return codeHiveDesc;

            }
            return null;
        }

        public Thread GetMainThread()
        {
            return mainThread;
        }

        internal int FindNextEntryPoint(string lable)
        {
            int i = 0;

            foreach (var inst in orderedInstructionList)
            {
                if (inst.Key == lable)
                {
                    return head_ProcessCodePhy + i;
                }
                i++;
            }
            return -1;
        }

        internal string GetProcessName()
        {
            return Name;
        }

        internal int GetPId()
        {
            return Pid;
        }
        internal hwCPU AssignCpu()
        {
            hwCPU cpu =  Os.GetSingleton().FindFreeCPU(Pid);

            foreach (var inst in codeHiveDesc.GetEntries())
            {
                cpu.AddMmuEntry(inst.Key, inst.Value);
            }

            foreach (var inst in dataHiveDesc.GetEntries())
            {
                cpu.AddMmuEntry(inst.Key, inst.Value);
            }

            foreach (var inst in bssHiveDesc.GetEntries())
            {
                cpu.AddMmuEntry(inst.Key, inst.Value);
            }

            return cpu;
        }

        internal List<Thread> GetThreads()
        {
            return threads;
        }

        public string Name { get; set; }
        public string Description { get; set; }

        //CPU pseudo language
        private const int GetOsReference = 1;
        private const int MemWrite = 2;
        private const int MemWriteFromBSS = 3;
        private const int AllocMem = 4;

        private Dictionary<string, pseudoInstruction> orderedInstructionList { get; set; }
        private List<memoryLocatedData> DataList { get; set; }
        private List<memoryLocatedData> BssList { get; set; }
        private List<pseudoInstruction> CodeList { get; set; }

        private Tuple<int, int> ProcessHeap;
        private Tuple<int, int> ProcessCode;
        private Tuple<int, int> ProcessBSS;
        private Tuple<int, int> ProcessStack;

        private int ProcessHeapPhy;
        private int ProcessCodePhy;
        private int ProcessBSSPhy;
        private int ProcessStackPhy;

        private const int head_ProcessHeapPhy = 100;
        private const int head_ProcessCodePhy = 400;
        private const int head_ProcessBSSPhy = 0;
        private const int head_ProcessStackPhy = 1024;

        private PageDesc codeHiveDesc;
        private PageDesc dataHiveDesc;
        private PageDesc bssHiveDesc;

        private int physicalMemorycurrPtr;

        private object processLock;

        private int Pid;

        internal Dictionary<int, PageDesc> pages;
        internal int LastFreePageIdx = 1;
        internal List<Thread> threads;

        Thread mainThread;
        private volatile int ProfilerEnabled = 0;
        private volatile string ProfilerSyncEventVar;
    }
}
