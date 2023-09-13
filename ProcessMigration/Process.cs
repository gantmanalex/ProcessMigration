using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProcessMigration
{
    public class pseudoInstruction
    {        
        public string Name { get; set; }
        public virtual bool Execute(Thread thread, PageDesc desc)  
        { return false;}
        
    }
    public class OsInvokeMethod : pseudoInstruction
    {
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

    public class memoryLocatedData
    {
        public string Name;
        public string Source;
        public int Size;
    }

    public class pseudocode_MainThread
    {
        
    }
    public class Process
    {

        public Process(string name, int pid, string binary)
        {
            Name = name;
            Pid = pid;
            Console.WriteLine("Process \"{0}\" created ", name);



            //Parse pseudo Instruction JSON
            int OsExitIdx = 0;
            int BssFiledIdx = 0;
            orderedInstructionList = new Dictionary<string, pseudoInstruction>();
            bssDataList = new List<memoryLocatedData>();
            heapDataList = new List<memoryLocatedData>();
            pages = new Dictionary<int, PageDesc>();

            Os os = Os.GetSingleton();

            PageDesc currMemPage = os.GetNextFreeMemoryDescriptor(PageType.DATA);
            pages[LastFreePageIdx] = currMemPage;
            LastFreePageIdx++;

            PageDesc currBssPage = os.GetNextFreeMemoryDescriptor(PageType.BSS);
            pages[LastFreePageIdx] = currBssPage;
            LastFreePageIdx++;


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
                                                        OsInvokeMethod method = new OsInvokeMethod();
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
                                                                method.Type = instruction.Type;
                                                                method.Size = instruction.Size;

                                                                break;
                                                            case "Exit":
                                                                method.Name = instruction.Name;
                                                                method.Param0 = instruction.Code;
                                                                break;
                                                        }
                                                        orderedInstructionList[opCode.Name] = method;
                                                        break;
                                                    }
                                                case "CPU":
                                                    {
                                                        cpuInstruction method = new cpuInstruction();
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
                                                        break;
                                                    }
                                            }
                                            break;
                                        case "BSS":

                                            memoryLocatedData data = new memoryLocatedData();
                                            data.Name = instruction.Name;
                                            data.Source = instruction.Source;
                                            data.Size = instruction.Size;

                                            bssDataList.Add(data);
                                            currBssPage.AddData(data.Source, data.Name);
                                            break;
                                        case "Memory":
                                            memoryLocatedData heapData = new memoryLocatedData();
                                            heapData.Name = instruction.Name;
                                            heapData.Source = instruction.Source;
                                            heapData.Size = instruction.Size;
                                            heapDataList.Add(heapData);
                                            currMemPage.AddData(heapData.Source, heapData.Name);
                                            break;

                                    }       
                                    //Console.WriteLine("Segment {0} Instruction {1}", segment.Name, instruction.Name);
                                }
                }
               
            }
            

        }

        public void MonitorLoad(int offloadProcessId, string systemEventVar) 
        {
            //Attaching Profiler
            Console.WriteLine("Attaching heavy load proffiler ...");

            ProfilerEnabled = offloadProcessId;
            ProfilerSyncEventVar = systemEventVar;

        }

        public async Task<int> EntryPoint(Thread thread)
        {
            const int MAX_LOAD_VALUE = 5;

            await Task.Delay(1000);

            int Threshold = 0;
            int idxCurrentInstruction = idxEntryPoint;
            Os os = Os.GetSingleton();

            hwCPU hwCpu =  os.FindFreeCPU(Pid);

            mainThread = thread;
            mainThread.AssingMemory(pages);
            mainThread.AssingCPU(hwCpu);

            do
            {

                if (mainThread.GetState() == ThreadState.WAIT)
                {
                    Console.WriteLine ("[{0}(pid:{1})]: Entering wait state", Name, Pid, mainThread.tid);
                    mainThread.WaitForEvent();
                    Console.WriteLine("[{0}(pid:{1})]: Exiting wait state", Name, Pid, mainThread.tid);
                }
                string nextIp = mainThread.RunInstruction(orderedInstructionList["Offset#" + idxCurrentInstruction]);
                if (nextIp == "Halt")
                    break;
                if (nextIp != "Fetch")
                {
                    int previousInstruction = idxCurrentInstruction;

                    idxCurrentInstruction = FindNextEntryPoint(nextIp);

                    //TODO: Profiler to detect heavy load
                    if (ProfilerEnabled != 0)
                        if (idxCurrentInstruction < previousInstruction)
                        {
                            Threshold++;
                            if (Threshold == MAX_LOAD_VALUE)
                            {
                                Console.WriteLine("Process[{0}]:Profiler - Worker #{1} intensive CPU usage detected", Pid, mainThread.tid);

                                Process heavyLoader = os.GetProcessById(ProfilerEnabled);
                                Thread loaderMainThread =  heavyLoader.GetMainThread();

                                //Check if MainThread waing for Load 
                                if (loaderMainThread == null || loaderMainThread.GetState() != ThreadState.WAIT)
                                {
                                    Console.WriteLine("Process[{0}]:Profiler - Loader process not read yet,", Pid, mainThread.tid);
                                    do
                                    {
                                        loaderMainThread = heavyLoader.GetMainThread();
                                        if (loaderMainThread != null && loaderMainThread.GetState() == ThreadState.WAIT) { break; }
                                        Console.WriteLine("Process[{0}]:Profiler - wating for HeavyLoader process", Pid, mainThread.tid);
                                        await Task.Delay(1000);
                                    } while (true);

                                }
                                Console.WriteLine("Process[{0}]:Profiler - offloading Thread[{3}] to Process[{2}],", Pid, mainThread.tid, heavyLoader.Pid, mainThread.tid);

                                Tuple<PageDesc, string> var =  loaderMainThread.GetDataDesc(ProfilerSyncEventVar, PageType.DATA);
                                os.GetEvent(Int32.Parse(var.Item2)).Reset();

                            }
                        }
                    } else
                        idxCurrentInstruction ++;
            } while (true);

            
            return 0;
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
                    return i;
                }
                i++;
            }
            return -1;
        }

        internal string GetProcessName()
        {
            return Name;
        }

        public string Name { get; set; }
        public string Description { get; set; }

        //CPU pseudo language
        private const int GetOsReference = 1;
        private const int MemWrite = 2;
        private const int MemWriteFromBSS = 3;
        private const int AllocMem = 4;

        private Dictionary<string, pseudoInstruction> orderedInstructionList { get; set; }
        private List<memoryLocatedData> heapDataList { get; set; }
        private List<memoryLocatedData> bssDataList{ get; set; }

        private int idxEntryPoint = 0;

        private int Pid;

        internal Dictionary<int, PageDesc> pages;
        internal int LastFreePageIdx = 1;

        Thread mainThread;
        private volatile int ProfilerEnabled = 0;
        private volatile string ProfilerSyncEventVar;
    }
}
