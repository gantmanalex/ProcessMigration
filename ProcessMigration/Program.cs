using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMigration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Hypervisor hypervisor = Hypervisor.GetSingleton();
            emSystem system = emSystem.GetSingleton();
        }
    }
}
