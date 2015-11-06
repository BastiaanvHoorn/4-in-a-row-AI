namespace Server
{
    public class DatabaseLocation
    {
        private readonly string Path;
        public readonly string FieldPath;
        public readonly string FieldDataPath;
        public readonly int FieldLength;
        public readonly int Location;

        /// <summary>
        /// Creates a new database location object.
        /// </summary>
        /// <param name="path">Path in the database</param>
        /// <param name="fieldLength"></param>
        /// <param name="location">Field location in Fields.db</param>
        public DatabaseLocation(string path, int fieldLength, int location)
        {
            this.Path = path;
            this.FieldPath = path + "\\Fields.db";
            this.FieldDataPath = path + "\\FieldData.db";
            this.FieldLength = fieldLength;
            this.Location = location;
        }

        /// <summary>
        /// Returns the byte position of the location.
        /// </summary>
        /// <returns>Seek position</returns>
        public int getSeekPosition()
        {
            return Location * FieldLength;
        }

        public override string ToString()
        {
            return $"Path = {Path}; Location = {Location}";
        }
    }
}
