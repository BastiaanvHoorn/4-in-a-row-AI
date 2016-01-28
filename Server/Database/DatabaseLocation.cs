using Engine;
using System;
using System.IO;

namespace Server
{
    public class DatabaseLocation
    {
        public static DatabaseLocation NonExisting = new DatabaseLocation();
        
        private DatabaseSegment DbSegment;
        public readonly int Location;

        private DatabaseLocation() { Location = -1; }

        /// <summary>
        /// Creates a new database location object.
        /// </summary>
        /// <param name="path">Path in the database</param>
        /// <param name="fieldLength"></param>
        /// <param name="location">Field location in Fields.db</param>
        public DatabaseLocation(DatabaseSegment dbSegment, int location)
        {
            this.DbSegment = dbSegment;
            this.Location = location;
        }

        /// <summary>
        /// Returns whether the given location is an existing (valid to use) location.
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <returns></returns>
        public bool locationExists()
        {
            return DbSegment.locationExists(Location);
        }

        public DatabaseSegment getDatabaseSegment()
        {
            return DbSegment;
        }

        public int getFieldLength()
        {
            return DbSegment.FieldLength;
        }

        /// <summary>
        /// Returns the filepath of the file which contains the field belonging to the DatabaseLocation.
        /// </summary>
        /// <returns>Filepath</returns>
        public string getFieldsPath()
        {
            return DbSegment.getFieldsPath();
        }

        /// <summary>
        /// Returns the filepath of the file which contains the fielddata belonging to the DatabaseLocation.
        /// </summary>
        /// <returns>Filepath</returns>
        public string getFieldDataPath()
        {
            return DbSegment.getFieldDataPath();
        }

        public static string getFieldPath(DatabaseSegment dbSegment)
        {
            return dbSegment.getFieldsPath();
        }

        public static string getFieldDataPath(DatabaseSegment dbSegment)
        {
            return dbSegment.getFieldDataPath();
        }
        
        public static DatabaseLocation operator +(DatabaseLocation dbLoc, int i)
        {
            int location = dbLoc.Location + i;

            return new DatabaseLocation(dbLoc.DbSegment, location);
        }
        
        public override string ToString()
        {
            if (DbSegment != null)
                return $"FieldLength = {DbSegment.FieldLength}; Location = {Location}";
            else
                return $"Location = {Location}";
        }
    }
}
