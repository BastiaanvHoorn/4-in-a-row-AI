using System;
using System.Linq;

namespace Server
{
    public class FieldData
    {
        public uint[] TotalCounts;
        public uint[] WinningCounts;

        /// <summary>
        /// Creates a new FieldData object with empty storage.
        /// </summary>
        public FieldData()
        {
            TotalCounts = new uint[7];
            WinningCounts = new uint[7];
        }

        /// <summary>
        /// Creates a new FieldData object based on the given uint array.
        /// </summary>
        /// <param name="storage"></param>
        public FieldData(uint[] storage) : this()
        {
            Array.Copy(storage, 0, TotalCounts, 0, 7);
            Array.Copy(storage, 7, WinningCounts, 0, 7);
        }

        /// <summary>
        /// Returns the winning chance for the specified column.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public float getWinningChance(byte column)
        {
            float chance = (float)WinningCounts[column] / (float)TotalCounts[column];
            return chance;
        }

        /// <summary>
        /// Returns the winning chance for the field. (Average of chances of all columns)
        /// </summary>
        /// <returns></returns>
        public float getTotalWinningChance()
        {
            float[] chances = new float[TotalCounts.Length];

            for (byte i = 0; i < TotalCounts.Length; i++)
            {
                chances[i] = getWinningChance(i);
            }

            return chances.Average();
        }

        /// <summary>
        /// Returns how many times the field has occured.
        /// </summary>
        /// <returns></returns>
        public long getOccuranceCount()
        {
            long occurances = 0;

            foreach (uint count in TotalCounts)
            {
                occurances += count;
            }

            return occurances;
        }

        /// <summary>
        /// Returns the uint storage of the FieldData in the same way as the database stores it.
        /// </summary>
        /// <returns></returns>
        public uint[] getStorage()
        {
            uint[] result = new uint[14];
            TotalCounts.CopyTo(result, 0);
            WinningCounts.CopyTo(result, 7);
            return result;
        }
    }
}
