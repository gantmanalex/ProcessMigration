using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMigration
{
    public class hwCPU
    {
        private hwCPU()
        {

            Console.WriteLine("CPU assigned ...");
        }

        private static readonly hwCPU cpu= new hwCPU();

        public static hwCPU GetSingleton()
        {
            return cpu;
        }

    }
}
