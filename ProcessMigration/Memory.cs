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
    public enum PageType
    {
        CODE,
        DATA,
        BSS,
        NOSPEC
    }

    internal class DataHolder
    {
        public DataHolder(string _data) { update(_data); }

        private string intData;
        public string data   // property
        {
            get { return intData; }
        }
        public void update(string _data)
        { intData = _data; }
    }
    internal class hwMemoryPage
    {
        public hwMemoryPage(PageType segment)
        {
            dataBank = new Dictionary<int, DataHolder>();

            //TODO: Hypervisor register page
            pages_address = 1;

            Segment = segment;
        }

        private PageType Segment;

        private readonly int pages_address;

        private bool Locked = false;

        public int GetPageAddress()
        {
            return pages_address;
        }

        public PageType GetPageType()
        {
            return Segment;
        }

        public string GetData(int pageIdx, int offset)
        {
            return dataBank[offset].data;
        }

        public void StoreData(int offset, string data)
        {
            dataBank[offset] = new DataHolder(data);
            //Todo: Trigger Memory Access interrupt
        }

   
        internal void SetData(int offset, string data)
        {
            dataBank[offset].update(data);
        }

        internal void LockPage()
        {
            Locked = true;
        }

        internal Dictionary<int, DataHolder> dataBank;
        internal int LastFreePageIdx = 0;
    }
}
