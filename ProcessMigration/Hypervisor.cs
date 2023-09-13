using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMigration
{
    internal class Hypervisor
    {
        private Hypervisor()
        {
            Console.WriteLine("Hypervisor start...");

        }

        private static readonly Hypervisor hyperV= new Hypervisor();

        public static Hypervisor GetSingleton()
        {
            return hyperV;
        }
    }
}
