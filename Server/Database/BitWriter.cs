using System;

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
        public BitWriter(int maxLength)
        {
            Storage = new byte[maxLength];
            WritePosition = 0;
        }

        /// <summary>
        /// Appends a bit (0 or 1, depending on value) to the Storage array
        /// </summary>
        /// <param name="value">An integer that has a value of 0 or 1</param>
        public void append(int value)
        {
            if (value == 1)
            {
                int byteIndex = WritePosition >> 3; //WritePosition / 8
                int bitIndex = WritePosition & 7;   //WritePosition % 8
                
                Storage[byteIndex] |= (byte)(1 << bitIndex);    //We store the value at the right place in Storage.
            }

            WritePosition++;    //By incrementing WritePosition, we know where to store the next bit.
        }

        /// <summary>
        /// Returns the storage array that has been built up.
        /// </summary>
        /// <returns>Storage</returns>
        public byte[] getStorage()
        {
            int size = (WritePosition >> 3) + 1;
            byte[] result = new byte[size];
            Buffer.BlockCopy(Storage, 0, result, 0, size);
            return result;
        }
    }
}
