using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class BitReader
    {
        private static byte[] Bitmask = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128 };

        /// <summary>
        /// Returns a byte value representing the bit at the give position.
        /// </summary>
        /// <param name="toRead">Byte to read bit from</param>
        /// <param name="bitPos">Bit (position in byte) to read</param>
        /// <returns>Byte value 0 or 1</returns>
        public static byte[] getBits(byte b)
        {
            byte[] bits = new byte[8];
            byte toRead = b;

            for (byte i = 0; i < 8; i++)
            {
                bits[i] = (byte)(toRead & 1);
                toRead >>= 1;
            }

            return bits;
        }
    }
}
