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
        public readonly byte MaxFieldStorageSize;
        public readonly char PathSeparator;

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
                    MaxFieldStorageSize = calculateMaxFieldStorageSize();
                }
            }
        }

        /// <summary>
        /// Creates a new object that can be used to create a new database. (Instead of reading the properties from a file)
        /// </summary>
        /// <param name="path">Database path</param>
        /// <param name="fieldWidth">Width of the fields to store</param>
        /// <param name="height">Height of the fields to store</param>
        public DatabaseProperties(string path, byte fieldWidth, byte fieldHeight)
        {
            if (path.Contains("\\"))
                PathSeparator = '\\';
            else
                PathSeparator = '/';
            this.Path = path;
            this.FieldWidth = fieldWidth;
            this.FieldHeight = fieldHeight;
            this.MaxFieldStorageSize = calculateMaxFieldStorageSize();
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
                }
            }
        }

        /// <summary>
        /// Returns the maximum storage size (in bytes) that could be needed according to the given database properties.
        /// </summary>
        /// <returns>Max bytes needed per field</returns>
        private byte calculateMaxFieldStorageSize()
        {
            return Extensions.getMaxStorageSize(FieldWidth, FieldHeight);
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
    }
}
