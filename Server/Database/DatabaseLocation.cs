namespace Server
{
    public class DatabaseLocation
    {
        private readonly string Path;
        public readonly string FieldPath;
        public readonly string FieldDataPath;
        public readonly byte FieldLength;
        public readonly int Location;

        public DatabaseLocation(string path, byte fieldLength, int location)
        {
            this.Path = path;
            this.FieldPath = path + "\\Fields.db";
            this.FieldDataPath = path + "\\FieldData.db";
            this.FieldLength = fieldLength;
            this.Location = location;
        }

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
