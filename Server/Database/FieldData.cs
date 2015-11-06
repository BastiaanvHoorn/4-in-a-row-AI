using System;

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

        public FieldData(uint[] storage) : this()
        {
            Array.Copy(storage, 0, totalCounts, 0, 7);
            Array.Copy(storage, 7, winningCounts, 0, 7);
        }

        public float getWinningChance(byte column)
        {
            uint total = totalCounts[column];
            uint winning = winningCounts[column];
            float chance = winning / total;
            return chance;
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
