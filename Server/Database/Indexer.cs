using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Indexer
    {
        /*public static List<DatabaseLocation> getLeastOccured(Database db, int storageLength, float sectionSize)
        {
            List<long> items = new List<long>();
            List<long> result = new List<long>();
            long max = 0;

            byte[] bleh = new byte[5];
            uint[] numbers = Array.ConvertAll(bleh, c => (uint)c);
            
            foreach (long i in items)
            {
                if (i < max)
                {
                    result.Remove(max);
                    result.Add(i);
                    max = result.Max();
                }
            }

            return result;
        }*/
    }
}
