namespace Server
{
    public class DatabaseLocation
    {
        public static DatabaseLocation NonExisting = new DatabaseLocation();

        private readonly string Path;
        private readonly int GlobalLocation;
        private DatabaseProperties DbProperties;
        public readonly int FileIndex;
        public readonly int FieldLength;
        public readonly int Location;

        private DatabaseLocation() { Location = -1; }

        /// <summary>
        /// Creates a new database location object.
        /// </summary>
        /// <param name="path">Path in the database</param>
        /// <param name="fieldLength"></param>
        /// <param name="location">Field location in Fields.db</param>
        public DatabaseLocation(DatabaseProperties dbProperties, int fieldLength, int fileIndex, int location)
        {
            this.DbProperties = dbProperties;
            this.Path = dbProperties.getFieldDirPath(fieldLength);
            this.GlobalLocation = fileIndex * dbProperties.getMaxFieldsInFile(fieldLength) + location;
            this.FieldLength = fieldLength;
            this.FileIndex = fileIndex;
            this.Location = location;
        }
        
        /// <summary>
        /// Returns whether the given location is an existing (valid to use) location.
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <returns></returns>
        public bool locationExists()
        {
            return Location >= 0 && Location < DbProperties.getMaxFieldsInFile(FieldLength);
        }
        
        /// <summary>
        /// Returns the filepath of the file which contains the field belonging to the DatabaseLocation.
        /// </summary>
        /// <returns>Filepath</returns>
        public string getFieldPath()
        {
            return getFieldPath(Path, FileIndex);
        }

        /// <summary>
        /// Returns the filepath of the file which contains the fielddata belonging to the DatabaseLocation.
        /// </summary>
        /// <returns>Filepath</returns>
        public string getFieldDataPath()
        {
            return getFieldDataPath(Path, FileIndex);
        }

        /// <summary>
        /// Returns the byte position of the location.
        /// </summary>
        /// <returns>Seek position</returns>
        public int getFieldsSeekPosition()
        {
            return Location * FieldLength;
        }
        
        /// <summary>
        /// Returns the position (in bytes) in the field data database where the data corresponding to the given field location is stored.
        /// </summary>
        /// <param name="fieldLocation">The location of the field</param>
        /// <param name="fieldDataStream">The field data database stream</param>
        /// <returns></returns>
        public int getFieldDataSeekPosition()
        {
            if (!locationExists())
                throw new DatabaseException($"Can't calculate seek position for field location. {ToString()}. This location doesn't exist");

            return Location * DbProperties.FieldWidth * 8;
        }

        public static string getFieldPath(string path, int fileIndex)
        {
            return $"{path}\\Fields{fileIndex}.db";
        }

        public static string getFieldDataPath(string path, int fileIndex)
        {
            return $"{path}\\FieldData{fileIndex}.db";
        }

        public static DatabaseLocation operator +(DatabaseLocation dbLoc, int i)
        {
            int globalLoc = dbLoc.GlobalLocation + i;
            int maxFieldsInFile = dbLoc.DbProperties.getMaxFieldsInFile(i);
            int fileIndex = globalLoc / maxFieldsInFile;
            int location = globalLoc % maxFieldsInFile;

            return new DatabaseLocation(dbLoc.DbProperties, dbLoc.FieldLength, fileIndex, location);
        }

        public override string ToString()
        {
            return $"Path = {Path}; FileIndex = {FileIndex}; GlobalLocation = {GlobalLocation}";
        }
    }
}
