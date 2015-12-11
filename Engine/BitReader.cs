using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class BitReader
    {
        private byte[] Bits;
        private int ReadPos;
        //private static byte[] Bitmask = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128 };

        /// <summary>
        /// Returns a byte value representing the bit at the give position.
        /// </summary>
        /// <param name="toRead">Byte to read bit from</param>
        /// <param name="bitPos">Bit (position in byte) to read</param>
        /// <returns>Byte value 0 or 1</returns>
        public BitReader(byte b)
        {
            byte[] Bits = new byte[8];
            byte toRead = b;

            for (byte i = 0; i < 8; i++)
            {
                Bits[i] = (byte)(toRead & 1);
                toRead >>= 1;
            }

            ReadPos = 0;
        }

        public BitReader(byte[] b)
        {
            Bits = new byte[8 * b.Length];

            for (int i = 0; i < b.Length; i++)
            {
                byte toRead = b[i];

                for (byte j = 0; j < 8; j++)
                {
                    Bits[i * 8 + j] = (byte)(toRead & 1);
                    toRead >>= 1;
                }
            }

            ReadPos = 0;
        }

        public int readBit()
        {
            if (ReadPos < Bits.Length)
            {
                byte b = Bits[ReadPos];
                ReadPos++;
                return b;
            }
            else
            {
                return -1;
            }
        }
    }
}
