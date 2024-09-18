using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMigration
{

    public class emSystem 
    {
        private const int numOfCPUs = 10;
        private const int numOfInts = 256;
        //Memory describibed by Pages
        private const int numOfMemoryPages= 1024;
        private const int _PAGE_SIZE = 4096;

        private static readonly hwCPU[] cpu = new hwCPU[numOfCPUs];

        //Interrupt vector
        private static readonly SysInt[] sysInt = { new SysInt(1, "Scheduler"), new SysInt(2, "Page fault") };
        private emSystem()
        {
            Console.WriteLine("System Boot...");

            Os os = Os.GetSingleton();

            os.MainThread(this);

            while (true) {
                System.Threading.Thread.Sleep(50);

            };

        }

        private static readonly emSystem _system= new emSystem();

        public static emSystem GetSingleton()
        {
            return _system;
        }

        public int getCPUNumber()
        {
            return numOfCPUs;
        }

        public bool registerInterruptVector(SysInt sysInt, IAsyncResult intCB)
        {
            Console.WriteLine ("OS registered callback in Int vector %1:%2 ", sysInt.GetIntVector(), sysInt.GetIntDescription());
            return sysInt.RegisterCallback(intCB);
        }

        internal int getMemorySizeInPages()
        {
            return numOfMemoryPages;
        }

        internal hwCPU getCPU(int idx)
        {
            return cpu[idx];
        }
    }
}
