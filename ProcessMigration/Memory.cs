using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProcessMigration
{

    public class emMemory
    {
        private static emMemory signleTone = new emMemory();
        public static emMemory GetSingleTone()
        {
            return signleTone;
        }

        public hwMemory AllocatePageHive(int size)
        {
            return new hwMemory(size);
        }

        public class hwMemory
        {
            public hwMemory(int size)
            {
                dataBank = new Dictionary<int, DataElement>(size);
            }

            public DataElement Read(int _physicalAddress)
            {
                return dataBank[_physicalAddress];
            }

            public void Write(int _physicalAddress, DataElement _dataElement)
            {
                dataBank[_physicalAddress] = _dataElement;
            }

            internal Dictionary<int, DataElement> dataBank;
        }
    }
}
