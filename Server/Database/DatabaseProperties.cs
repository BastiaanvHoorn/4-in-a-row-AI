using System;
using System.IO;
using Engine;

namespace Server
{
    public class DatabaseProperties
    {
        public readonly string Path;
        public readonly byte FieldWidth;
        public readonly byte FieldHeight;
        public readonly long MaxFileSize;
        public readonly byte MaxFieldStorageSize;
        public readonly char PathSeparator;
        private int[] Lengths;

        /// <summary>
        /// Reading constructor for DatabaseProperties. Reads the properties file of an existing database.
        /// </summary>
        /// <param name="path">Database path</param>
        public DatabaseProperties(string path)
        {
            this.Path = path;

            if (path.Contains("\\"))
                PathSeparator = '\\';
            else
                PathSeparator = '/';

            using (FileStream properties = new FileStream(path + PathSeparator + "Properties", FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(properties))
                {
                    FieldWidth = br.ReadByte();
                    FieldHeight = br.ReadByte();
                    MaxFileSize = br.ReadInt64();
                    MaxFieldStorageSize = br.ReadByte();
                    Lengths = new int[MaxFieldStorageSize];

                    for (byte i = 0; i < MaxFieldStorageSize; i++)
                    {
                        Lengths[i] = br.ReadInt32();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new object that can be used to create a new database. (Instead of reading the properties from a file)
        /// </summary>
        /// <param name="path">Database path</param>
        /// <param name="fieldWidth">Width of the fields to store</param>
        /// <param name="height">Height of the fields to store</param>
        public DatabaseProperties(string path, byte fieldWidth, byte fieldHeight, long maxFileSize)
        {
            if (path.Contains("\\"))
                PathSeparator = '\\';
            else
                PathSeparator = '/';
            this.Path = path;
            this.FieldWidth = fieldWidth;
            this.FieldHeight = fieldHeight;
            this.MaxFileSize = maxFileSize;
            this.MaxFieldStorageSize = calculateMaxFieldStorageSize();
            this.Lengths = new int[MaxFieldStorageSize];
        }

        /// <summary>
        /// Writes the properties to the file
        /// </summary>
        public void writeProperties()
        {
            using (FileStream properties = new FileStream(Path + PathSeparator + "Properties", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter br = new BinaryWriter(properties))
                {
                    br.Write(this.FieldWidth);
                    br.Write(this.FieldHeight);
                    br.Write(this.MaxFileSize);
                    br.Write(this.MaxFieldStorageSize);

                    for (byte i = 0; i < MaxFieldStorageSize; i++)
                    {
                        br.Write(this.Lengths[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Increments the length (in fields) of the given database file by one. (Called after adding a field)
        /// </summary>
        public void increaseLength(int i, bool writeToDisk = true)
        {
            if (i < 1 || i > MaxFieldStorageSize)
                throw new DatabaseException($"Argument i is not in range of the database files. i = {i}; min = {1}; max = {MaxFieldStorageSize}");

            Lengths[i - 1]++;
            if (writeToDisk)
                writeProperties();
        }

        /// <summary>
        /// Returns the current length (in fields) of the given database file.
        /// </summary>
        /// <returns></returns>
        public int getLength(int i)
        {
            if (i < 1 || i > MaxFieldStorageSize)
                throw new DatabaseException($"Argument i is not in range of the database files. i = {i}; min = {1}; max = {MaxFieldStorageSize}");

            return this.Lengths[i - 1];
        }

        public int getTotalLength()
        {
            int result = 0;

            for (byte i = 1; i <= MaxFieldStorageSize; i++)
            {
                result += getLength(i);
            }

            return result;
        }

        /// <summary>
        /// Returns the maximum storage size (in bytes) that could be needed according to the given database properties.
        /// </summary>
        /// <returns>Max bytes needed per field</returns>
        public byte calculateMaxFieldStorageSize()
        {
            return (byte)Extensions.getMaxStorageSize(FieldWidth, FieldHeight);
        }
        
        /// <summary>
        /// Returns the path of the directory in which the given field (as a byte array) could be stored.
        /// </summary>
        /// <param name="storageLength">The storage length</param>
        /// <returns>Directory path in database</returns>
        public string getFieldDirPath(int storageLength)
        {
            string path = this.Path;
            string subDir = "FieldLength" + storageLength;
            return path + PathSeparator + subDir;
        }

        /// <summary>
        /// Returns the path of the directory in which the given field (as a byte array) could be stored.
        /// </summary>
        /// <param name="fieldStorage">The compressed field storage</param>
        /// <returns>Directory path in database</returns>
        public string getFieldDirPath(byte[] fieldStorage)
        {
            return getFieldDirPath(fieldStorage.Length);
        }

        /// <summary>
        /// Returns the maximum amount of fields that can be stored in one file in the database, based on the fieldlength.
        /// </summary>
        /// <param name="fieldLength">The fieldlength to check this value for</param>
        /// <returns>Max amount of fields</returns>
        public int getMaxFieldsInFile(int fieldLength)
        {
            return (int)(MaxFileSize / fieldLength);
        }

        /// <summary>
        /// Returns the amount of files in the database that contain fields with the given fieldLength.
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <returns></returns>
        public int getFieldFileCount(int fieldLength)
        {
            double maxFieldsInFile = getMaxFieldsInFile(fieldLength);
            return (int)Math.Ceiling(getLength(fieldLength) / maxFieldsInFile);
        }
    }
}
