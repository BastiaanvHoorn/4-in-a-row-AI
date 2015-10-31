using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// A simple class which makes it easier to store separate bits into a byte array
    /// </summary>
    public class BitWriter
    {
        private byte[] Storage;
        private int WritePosition;

        /// <summary>
        /// Creates a new instance of BitWriter
        /// </summary>
        public BitWriter()
        {
            Storage = new byte[1];
            WritePosition = 0;
        }

        /// <summary>
        /// Appends a bit (0 or 1, depending on value) to the Storage array
        /// </summary>
        /// <param name="value">An integer that has a value of 0 or 1</param>
        public void append(int value)
        {
            int byteIndex = WritePosition >> 3; //WritePosition / 8
            int bitIndex = WritePosition % 8;   //WritePosition % 8

            if (Storage.Length == byteIndex)    //In this situation we need to add a byte to the array.
            {
                Array.Resize(ref Storage, byteIndex + 1);
            }

            Storage[byteIndex] |= (byte)(value << bitIndex);    //We store the value at the right place in Storage.

            WritePosition++;    //By incrementing WritePosition, we know where to store the next bit.
        }

        /// <summary>
        /// Returns the storage array that has been built up. If the last byte has a value of 0, it will be removed from the array.
        /// </summary>
        /// <returns>Storage</returns>
        public byte[] getStorage()
        {
            return Storage;
        }
    }
}
