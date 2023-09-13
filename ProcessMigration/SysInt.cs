using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMigration
{
    public class SysInt
    {
        public SysInt(int vector, string description) 
        {
            Vector = vector;
            Description = description;
        }
        private readonly int Vector;
        private readonly string Description;
        private IAsyncResult Callback;

        internal bool RegisterCallback(IAsyncResult intCB)
        {
            if (Callback != null) { return false; }
            
            Callback = intCB;
            return true;
        }

        internal int GetIntVector()
        {
            return Vector;
        }

        internal string GetIntDescription()
        {
            return Description;
        }
    }
}
