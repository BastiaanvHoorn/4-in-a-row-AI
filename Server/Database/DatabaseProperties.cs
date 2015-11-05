using System.IO;

namespace Server
{
    public class DatabaseProperties
    {
        public readonly string Path;
        public readonly byte FieldWidth;
        public readonly byte FieldHeight;
        public readonly byte MaxFieldStorageSize;
        private int[] Lengths;

        /// <summary>
        /// Reading constructor for DatabaseProperties. Reads the properties file of an existing database.
        /// </summary>
        /// <param name="path">Database path</param>
        public DatabaseProperties(string path)
        {
            this.Path = path;

            using (FileStream properties = new FileStream(path + "\\Properties", FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(properties))
                {
                    FieldWidth = br.ReadByte();
                    FieldHeight = br.ReadByte();
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
        public DatabaseProperties(string path, byte fieldWidth, byte fieldHeight)
        {
            this.Path = path;
            this.FieldWidth = fieldWidth;
            this.FieldHeight = fieldHeight;
            this.MaxFieldStorageSize = calculateMaxFieldStorageSize();
            this.Lengths = new int[MaxFieldStorageSize];
        }

        /// <summary>
        /// Writes the properties to the file
        /// </summary>
        public void writeProperties()
        {
            using (FileStream properties = new FileStream(Path + "\\Properties", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter br = new BinaryWriter(properties))
                {
                    br.Write(this.FieldWidth);
                    br.Write(this.FieldHeight);
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
        public void fieldAdded(byte i)
        {
            if (i < 1 || i > MaxFieldStorageSize)
                throw new DatabaseException($"Argument i is not in range of the database files. i = {i}; min = {1}; max = {MaxFieldStorageSize}");

            Lengths[i - 1]++;
            writeProperties();
        }

        /// <summary>
        /// Returns the current length (in fields) of the given database file.
        /// </summary>
        /// <returns></returns>
        public int getLength(byte i)
        {
            if (i < 1 || i > MaxFieldStorageSize)
                throw new DatabaseException($"Argument i is not in range of the database files. i = {i}; min = {1}; max = {MaxFieldStorageSize}");

            return this.Lengths[i - 1];
        }

        /// <summary>
        /// Returns the maximum storage size (in bytes) that could be needed according to the given database properties.
        /// </summary>
        /// <returns>Max bytes needed per field</returns>
        private byte calculateMaxFieldStorageSize()
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
            string subDir = "Field length = " + storageLength;
            return path + "\\" + subDir;
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
