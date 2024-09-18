using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMigration
{
    public class hwCPU
    {
        public hwCPU(int id)
        {
            Id = id;
            Console.WriteLine("CPU assigned ...#{0}", Id);
            mmu = new MMU(this);
        }

        public bool AddMmuEntry(int VirtualAddress, DataElement dataElement)
        {
            return mmu.AddEntry(VirtualAddress, dataElement);
        }
        public bool DeleteMmuEntry(int VirtualAddress)
        {
            return mmu.DeleteEntry(VirtualAddress);
        }

        internal void SetIP(int virtualAddress)
        {
            IPreg= virtualAddress;
        }

        public DataElement Fetch()
        {
            return mmu.Translate(IPreg++);
        }

        public string LoadData(int virtualAddress)
        {
            memoryLocatedData data = (memoryLocatedData)mmu.Translate(virtualAddress);
            return data.Source;

        }

        internal void StoreData(int virtualAddress, string v)
        {
            memoryLocatedData data = (memoryLocatedData)mmu.Translate(virtualAddress);
            data.Source = v;
        }

        internal int GetIP()
        {
            return IPreg;
        }

        internal object GetTid()
        {
            return Id;
        }

        private MMU mmu;
        private int IPreg;
        private int Id;

        internal class MMU
        {
            private Dictionary<int, DataElement> Entry;
            public MMU(hwCPU _cpu)
            {
                Entry = new Dictionary<int, DataElement>();
                cpu = _cpu;
            }
            public bool AddEntry(int VirtualAddress, DataElement dataElement)
            {
                if (Entry.ContainsKey(VirtualAddress))
                {
                    return false;
                }
                Entry[VirtualAddress] = dataElement;
                return true;
            }

            public bool DeleteEntry(int VirtualAddress)
            {
                if (Entry.ContainsKey(VirtualAddress))
                {
                    Entry.Remove(VirtualAddress);
                    return true;
                }
                return false;
            }

            public DataElement Translate(int virutalAddress)
            {
                if (!Entry.ContainsKey(virutalAddress)) 
                {
                    //cpu.GeneeratePageFault(virutalAddress);
                    return null; 
                }
                return Entry[virutalAddress];
            }

            private hwCPU cpu;
        }
    }
}
