using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class FieldData
    {
        public uint[] totalCounts;
        public uint[] winningCounts;

        public FieldData()
        {
            totalCounts = new uint[7];
            winningCounts = new uint[7];
        }

        public uint[] getStorage()
        {
            uint[] result = new uint[14];
            totalCounts.CopyTo(result, 0);
            winningCounts.CopyTo(result, 7);
            return result;
        }
    }
}
