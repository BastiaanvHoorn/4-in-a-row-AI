using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class CompressedCompare : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                int compared = x[i].CompareTo(y[i]);

                if (compared != 0)
                    return compared;
            }

            return 0;
        }
    }
}
