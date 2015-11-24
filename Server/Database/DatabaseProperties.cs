using System.IO;

namespace Server
{
    public class DatabaseProperties
    {
        public readonly string Path;
        public readonly byte FieldWidth;
        public readonly byte FieldHeight;
        private int Length;

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
                    Length = br.ReadInt32();
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
            this.Length = 0;
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
                    br.Write(this.Length);
                }
            }
        }

        /// <summary>
        /// Increments the length of the database by one. (After adding a field )
        /// </summary>
        public void fieldAdded()
        {
            this.Length++;
            writeProperties();
        }

        /// <summary>
        /// Returns the current length of the database (In fields)
        /// </summary>
        /// <returns></returns>
        public int getLength()
        {
            return this.Length;
        }
    }
}
