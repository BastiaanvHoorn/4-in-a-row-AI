using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Engine;

namespace Server
{
    public class Database
    {
        private DatabaseProperties DbProperties;

        /// <summary>
        /// Creates a new database instance from the given path.
        /// </summary>
        /// <param name="path">Database storage path</param>
        public Database(string path)
        {
            if (!Directory.Exists(path))
                throw new DatabaseException($"Database doesn't exist. The given database directory doesn´t exist ({path})");

            string propertiesPath = path + "\\Properties";

            if (!File.Exists(propertiesPath))
                throw new DatabaseException($"Database incomplete. Properties file not found in: {path}");

            DbProperties = new DatabaseProperties(path);
            
            for (byte i = 1; i <= DbProperties.MaxFieldStorageSize; i++)
            {
                string dirPath = DbProperties.getFieldDirPath(i);
                if (!Directory.Exists(dirPath))
                    throw new DatabaseException($"Database incomplete. Directory {dirPath} doesn't exist.");
            }
        }

        /// <summary>
        /// Creates a new database at the given path and creates an instance of this new database.
        /// </summary>
        /// <param name="dbProperties"></param>
        public Database(DatabaseProperties dbProperties)
        {
            if (Directory.Exists(dbProperties.Path))
                throw new DatabaseException("Database path already exists. Make sure you use a database path that doesn't exist already.");

            this.DbProperties = dbProperties;

            Directory.CreateDirectory(dbProperties.Path);

            byte maxSize = dbProperties.MaxFieldStorageSize;

            for (byte i = 1; i <= maxSize; i++)
            {
                Directory.CreateDirectory(dbProperties.getFieldDirPath(i));
            }

            dbProperties.writeProperties();
        }

        /// <summary>
        /// Returns whether the given location is an existing (valid to use) location, depending on the field data database Stream. WARNING: Do NOT use the field database Stream (Fields.db), only the field DATA database Stream (Fielddata.db).
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <param name="fieldDataStream">The field data database stream</param>
        /// <returns></returns>
        public bool locationExists(DatabaseLocation dbLocation)
        {
            return dbLocation.Location >= 0 && dbLocation.Location < DbProperties.getLength(dbLocation.FieldLength);
        }
        
        /// <summary>
        /// Returns the position (in bytes) in the field data database where the data corresponding to the given field location is stored.
        /// </summary>
        /// <param name="fieldLocation">The location of the field</param>
        /// <param name="fieldDataStream">The field data database stream</param>
        /// <returns></returns>
        public int getSeekPosition(DatabaseLocation dbLocation)
        {
            if (!locationExists(dbLocation))
                throw new DatabaseException($"Can't calculate seek position for field location {dbLocation.ToString()}, because this location doesn't exist");

            return dbLocation.Location * DbProperties.FieldWidth * 8;
        }
        
        /// <summary>
        /// Reads the field data from of the given field the database.
        /// </summary>
        /// <param name="field">Field to read the data from</param>
        /// <returns></returns>
        public FieldData readFieldData(Field field)
        {
            DatabaseLocation location = findField(field);
            return readFieldData(location);
        }

        /// <summary>
        /// Reads the field data from the database at the specified (field)location. (Not the location in bytes)
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <returns></returns>
        public FieldData readFieldData(DatabaseLocation dbLocation)
        {
            if (!locationExists(dbLocation))
                throw new DatabaseException($"Can't read field data at field location {dbLocation}, because this location doesn't exist.");

            using (FileStream fieldDataStream = new FileStream(dbLocation.FieldDataPath, FileMode.OpenOrCreate, FileAccess.Read)) // Opens the field data database Stream in read mode.
            {
                int seekPosition = getSeekPosition(dbLocation);

                uint[] storage = new uint[DbProperties.FieldWidth * 2];
                using (BinaryReader br = new BinaryReader(fieldDataStream))
                {
                    fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the reading position to the wanted byte (uint in our case) database.

                    for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)   // We read the uints one by one from the database.
                    {
                        storage[i] = br.ReadUInt32();
                    }
                }

                return new FieldData(storage);
            }
        }

        /// <summary>
        /// Writes the given field data to the database for the specified field.
        /// </summary>
        /// <param name="field">The field to save the data for</param>
        /// <param name="fieldData">The field data to write</param>
        public void writeFieldData(Field field, FieldData fieldData)
        {
            DatabaseLocation location = findField(field);
            writeFieldData(location, fieldData);
        }

        /// <summary>
        /// Writes the given field data to the database at the specified (field)location. (Not the location in bytes)
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <param name="fieldData">Data to be written</param>
        public void writeFieldData(DatabaseLocation dbLocation, FieldData fieldData)
        {
            if (!locationExists(dbLocation))
                throw new DatabaseException($"Can't write field data at field location. {dbLocation}. This location doesn't exist.");

            using (FileStream fieldDataStream = new FileStream(dbLocation.FieldDataPath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database Stream in write mode.
            {
                int seekPosition = getSeekPosition(dbLocation);

                using (BinaryWriter bw = new BinaryWriter(fieldDataStream))  // We use a BinaryWriter to be able to write uints directly to the stream.
                {
                    uint[] storage = fieldData.getStorage();

                    fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the writing position to the wanted byte (uint in our case) in the database.
                    for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // We write each uint of the storage to the database stream.
                    {
                        bw.Write(storage[i]);
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds the given field to the database. WARNING: It's your own responsibility to check for the existance of a field in the database. Always AVOID adding fields that are already included in the database.
        /// </summary>
        /// <param name="field">Field to be added</param>
        public void addDatabaseItem(Field field)
        {
            byte[] compressed = field.compressField();                  // Gets the compressed field.
            string fieldPath = DbProperties.getFieldDirPath(compressed) + "\\Fields.db";

            using (FileStream fieldStream = new FileStream(fieldPath, FileMode.OpenOrCreate, FileAccess.Write))     // Opens the field database Stream in write mode.
            {
                fieldStream.Seek(0, SeekOrigin.End);                    // Sets the writing position to the end of the database.
                fieldStream.Write(compressed, 0, compressed.Length);    // Writes the bytes of the compressed field to the database.
            }

            string fieldDataPath = DbProperties.getFieldDirPath(compressed) + "\\FieldData.db";
            using (FileStream fieldDataStream = new FileStream(fieldDataPath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database Stream in write mode.
            {
                fieldDataStream.Seek(0, SeekOrigin.End);
                fieldDataStream.Write(new byte[56], 0, 56);
            }

            DbProperties.fieldAdded((byte)compressed.Length);
        }

        /// <summary>
        /// Clears the database item (the field and its corresponding data) of the specified field from the database(s). This function doesn't delete the item, but sets all its values to zero.
        /// </summary>
        /// <param name="field">The field to be cleared</param>
        public void clearDataBaseItem(Field field)
        {
            DatabaseLocation dbLocation = findField(field);

            if (!locationExists(dbLocation))
                throw new DatabaseException($"Can't clear database item at database location ({dbLocation}), because this location doesn't exist.");

            using (FileStream fieldStream = new FileStream(dbLocation.FieldPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fieldStream.Seek(dbLocation.getSeekPosition(), SeekOrigin.Begin);
                fieldStream.Write(new byte[dbLocation.FieldLength], 0, dbLocation.FieldLength);
            }

            using (FileStream fieldDataStream = new FileStream(dbLocation.FieldDataPath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database Stream in write mode.
            {
                fieldDataStream.Seek(getSeekPosition(dbLocation), SeekOrigin.Begin);
                fieldDataStream.Write(new byte[56], 0, 56);
            }
        }

        /// <summary>
        /// Returns whether it's possible to add the given field to the stream. WARNING: In most cases it's better to do this check yourself, because the findField function could be quite expensive as the database grows.
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <param name="s">Stream to get the data from</param>
        public bool fieldExists(Field field)
        {
            DatabaseLocation dbLocation = findField(field);

            using (FileStream fieldStream = new FileStream(dbLocation.FieldPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                return locationExists(dbLocation);
            }
        }

        /// <summary>
        /// Returns whether the given field exists in the field database stream. Also stores the (field)location of specified field within the database in location. 
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <param name="s">Stream to get the data from</param>
        public bool fieldExists(Field field, out DatabaseLocation dbLocation)
        {
            dbLocation = findField(field);

            using (FileStream fieldStream = new FileStream(dbLocation.FieldPath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                return locationExists(dbLocation);
            }
        }

        /// <summary>
        /// Returns where the given field is located in the field database. Return value -1 means the specified field is not included in the database.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public DatabaseLocation findField(Field field)
        {
            byte[] compressed = field.compressField();
            byte fieldLength = (byte)compressed.Length;

            string path = DbProperties.getFieldDirPath(fieldLength);

            using (FileStream fieldStream = new FileStream(path + "\\Fields.db", FileMode.OpenOrCreate, FileAccess.Read)) //  Gets the stream from the database file in read mode.
            {
                int location = compressed.getFieldLocation(fieldStream);
                return new DatabaseLocation(path, fieldLength, location);
            }
        }
    }
}
