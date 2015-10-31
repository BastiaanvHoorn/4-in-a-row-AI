using System.IO;

namespace Server
{
    public class DatabaseProperties
    {
        private string PropertiesPath;

        public readonly byte Width;
        public readonly byte Height;
        public int Length
        {
            get { return Length; }

            set
            {
                writeProperties();
                Length = value;
            }
        }

        public DatabaseProperties() { }

        /// <summary>
        /// Reading constructor for DatabaseProperties. Reads an existing properties file.
        /// </summary>
        /// <param name="propertiesPath"></param>
        public DatabaseProperties(string propertiesPath)
        {
            this.PropertiesPath = propertiesPath;

            using (FileStream properties = new FileStream(propertiesPath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(properties))
                {
                    Width = br.ReadByte();
                    Height = br.ReadByte();
                    Length = br.ReadInt32();
                }
            }
        }

        /// <summary>
        /// Writing constructor for DatabaseProperties. Creates a new properties file.
        /// </summary>
        /// <param name="propertiesPath"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public DatabaseProperties(string propertiesPath, byte width, byte height)
        {
            this.PropertiesPath = propertiesPath;
            this.Width = width;
            this.Height = height;
            this.Length = 0;

            writeProperties();
        }

        private void writeProperties()
        {
            using (FileStream properties = new FileStream(PropertiesPath, FileMode.Open, FileAccess.Write))
            {
                using (BinaryWriter br = new BinaryWriter(properties))
                {
                    br.Write(this.Width);
                    br.Write(this.Height);
                    br.Write(this.Length);
                }
            }
        }
    }
}
