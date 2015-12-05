using System.IO;
using Engine;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Server
{
    public class Database
    {
        public DatabaseProperties DbProperties;

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
        /// Returns whether it's possible to add the given field to the stream. WARNING: In most cases it's better to do this check yourself, because the findField function could be quite expensive as the database grows.
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <param name="s">Stream to get the data from</param>
        public bool fieldExists(Field field)
        {
            DatabaseLocation dbLocation = findField(field);
            return dbLocation.locationExists(DbProperties);
        }

        /// <summary>
        /// Returns whether the given field exists in the field database stream. Also stores the (field)location of specified field within the database in location. 
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <param name="s">Stream to get the data from</param>
        public bool fieldExists(Field field, out DatabaseLocation dbLocation)
        {
            dbLocation = findField(field);
            return dbLocation.locationExists(DbProperties);
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
            if (!dbLocation.locationExists(DbProperties))
                throw new DatabaseException($"Can't read field data at field location {dbLocation}, because this location doesn't exist.");

            using (FileStream fieldDataStream = new FileStream(dbLocation.getFieldDataPath(), FileMode.OpenOrCreate, FileAccess.Read)) // Opens the field data database stream in read mode.
            {
                int seekPosition = dbLocation.getFieldDataSeekPosition(DbProperties);

                uint[] storage = new uint[DbProperties.FieldWidth * 2];
                using (BinaryReader br = new BinaryReader(fieldDataStream))
                {
                    fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the reading position to the wanted byte (uint in our case) database.

                    for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // We read the uints one by one from the database.
                    {
                        storage[i] = br.ReadUInt32();
                    }
                }

                return new FieldData(storage);
            }
        }

        public void processGameHistory(Dictionary<Field, FieldData> data)
        {
            #region Initialization and sorting of data Dictionary.
            byte maxFS = DbProperties.calculateMaxFieldStorageSize();
            Dictionary<Field, FieldData>[] cData = new Dictionary<Field, FieldData>[maxFS];
            for (byte i = 0; i < maxFS; i++)
            {
                cData[i] = new Dictionary<Field, FieldData>();
            }

            foreach (KeyValuePair<Field, FieldData> pair in data)
            {
                int length = pair.Key.compressField().Length;
                cData[length - 1].Add(pair.Key, pair.Value);
            }
            #endregion

            for (byte i = 1; i <= maxFS; i++)
            {
                #region Processes the fielddata of all fields that are already included in the database.

                Dictionary<Field, FieldData> dictionary = cData[i - 1];
                List<Field> matches = new List<Field>();

                string dirPath = DbProperties.getFieldDirPath(i);
                int fileCount = DbProperties.getFieldFileCount(i);
                
                for (int j = 0; j < fileCount; j++)
                {
                    string fPath = DatabaseLocation.getFieldPath(dirPath, j);
                    string fdPath = DatabaseLocation.getFieldDataPath(dirPath, j);

                    using (FileStream fieldStream = new FileStream(fPath, FileMode.OpenOrCreate, FileAccess.Read))
                    {
                        using (FileStream fieldDataStream = new FileStream(fdPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            int totalLength = (int)fieldStream.Length;
                            byte[] bytes = new byte[totalLength];
                            fieldStream.Read(bytes, 0, totalLength);

                            for (int k = 0; k < totalLength; k += i)
                            {
                                byte[] fStorage = new byte[i];
                                Buffer.BlockCopy(bytes, k, fStorage, 0, i);
                                Field f = fStorage.decompressField();

                                if (dictionary.ContainsKey(f))
                                {
                                    DatabaseLocation dbLoc = new DatabaseLocation(DbProperties, i, j, k / i);
                                    processFieldData(dbLoc, dictionary[f], fieldDataStream);
                                    matches.Add(f);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Determination of the fields that need to be added to the database.
                Dictionary<int, List<Field>> newFields = new Dictionary<int, List<Field>>();

                foreach (Field f in dictionary.Keys.Where(f => !matches.Contains(f)))
                {
                    DatabaseLocation dbLoc = allocateNextDatabaseLocation(i);
                    int fileIndex = dbLoc.FileIndex;

                    List<Field> l = null;

                    if (newFields.ContainsKey(fileIndex))
                    {
                        l = newFields[fileIndex];
                    }
                    else
                    {
                        l = new List<Field>();
                        newFields.Add(fileIndex, l);
                    }

                    l.Add(f);
                }
                #endregion

                #region Addition of new fields.
                foreach (KeyValuePair<int, List<Field>> pair in newFields)
                {
                    List<byte> bytes = new List<byte>();

                    string fdPath = DatabaseLocation.getFieldDataPath(DbProperties.getFieldDirPath(i), pair.Key);
                    using (FileStream fieldDataStream = new FileStream(fdPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        BinaryWriter bw = new BinaryWriter(fieldDataStream);
                        foreach (Field f in pair.Value)
                        {
                            bytes.AddRange(f.compressField());

                            fieldDataStream.Seek(0, SeekOrigin.End);

                            uint[] fdStorage = dictionary[f].getStorage();
                            
                            for (int k = 0; k < fdStorage.Length; k++)
                            {
                                bw.Write(fdStorage[k]);
                            }
                        }
                    }

                    string fPath = DatabaseLocation.getFieldPath(DbProperties.getFieldDirPath(i), pair.Key);
                    using (FileStream fieldStream = new FileStream(fPath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fieldStream.Seek(0, SeekOrigin.End);
                        fieldStream.Write(bytes.ToArray(), 0, bytes.Count);
                    }
                }
                #endregion
            }

            DbProperties.writeProperties();
        }

        private DatabaseLocation allocateNextDatabaseLocation(Field f)
        {
            return allocateNextDatabaseLocation(f.compressField().Length);
        }

        private DatabaseLocation allocateNextDatabaseLocation(int fieldLength)
        {
            DbProperties.increaseLength(fieldLength, false);
            int fileIndex = DbProperties.getFieldFileCount(fieldLength) - 1;
            return new DatabaseLocation(DbProperties, fieldLength, fileIndex, DbProperties.getLength(fieldLength) % DbProperties.getMaxFieldsInFile(fieldLength) - 1);
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
            if (!dbLocation.locationExists(DbProperties))
                throw new DatabaseException($"Can't write field data at field location. {dbLocation}. This location doesn't exist.");

            using (FileStream fieldDataStream = new FileStream(dbLocation.getFieldDataPath(), FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database Stream in write mode.
            {
                int seekPosition = dbLocation.getFieldDataSeekPosition(DbProperties);

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
        /// Writes the given field data to the database at the specified (field)location. (Not the location in bytes)
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <param name="fieldData">Data to be written</param>
        public void processFieldData(DatabaseLocation dbLocation, FieldData fieldData, FileStream fieldDataStream)
        {
            if (!dbLocation.locationExists(DbProperties))
                throw new DatabaseException($"Can't write field data at field location. {dbLocation}. This location doesn't exist.");
            
            int seekPosition = dbLocation.getFieldDataSeekPosition(DbProperties);
            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);

            BinaryWriter bw = new BinaryWriter(fieldDataStream);
            BinaryReader br = new BinaryReader(fieldDataStream);

            uint[] storage = fieldData.getStorage();

            if (seekPosition < fieldDataStream.Length)
            {
                for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)
                {
                    storage[i] += br.ReadUInt32();
                }
            }

            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the writing position to the wanted byte (uint in our case) in the database.

            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // We write each uint of the storage to the database stream.
            {
                bw.Write(storage[i]);
            }
        }

        /*private void addDatabaseItems(List<Field>[] fields)
        {
            for (int i = 0; i < DbProperties.calculateMaxFieldStorageSize(); i++)
            {
                foreach (Field f in fields[i])
                {
                    string fieldDirPath = DbProperties.getFieldDirPath(i);
                    using (FileStream fieldStream = new FileStream(fieldPath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field database Stream in write mode.
                    {

                    }
                }
            }
        }*/

        /// <summary>
        /// Adds the given field to the database. WARNING: It's your own responsibility to check for the existance of a field in the database. Always AVOID adding fields that are already included in the database.
        /// </summary>
        /// <param name="field">Field to be added</param>
        /// <returns>The DatabaseLocation of the added field</returns>
        public DatabaseLocation addDatabaseItem(Field field)
        {
            byte[] compressed = field.compressField();                  // Gets the compressed field.
            DatabaseLocation dbLoc = allocateNextDatabaseLocation(compressed.Length);
            
            int fileIndex = dbLoc.FileIndex;
            string fieldPath = dbLoc.getFieldPath();

            using (FileStream fieldStream = new FileStream(fieldPath, FileMode.OpenOrCreate, FileAccess.Write))     // Opens the field database Stream in write mode.
            {
                fieldStream.Seek(0, SeekOrigin.End);                    // Sets the writing position to the end of the database.
                fieldStream.Write(compressed, 0, compressed.Length);    // Writes the bytes of the compressed field to the database.
            }

            string fieldDataPath = dbLoc.getFieldDataPath();
            using (FileStream fieldDataStream = new FileStream(fieldDataPath, FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database Stream in write mode.
            {
                fieldDataStream.Seek(0, SeekOrigin.End);
                fieldDataStream.Write(new byte[DbProperties.FieldWidth * 8], 0, DbProperties.FieldWidth * 8);
            }

            return dbLoc;
        }

        /// <summary>
        /// Clears the database item (the field and its corresponding data) of the specified field from the database(s). This function doesn't delete the item, but sets all its values to zero.
        /// </summary>
        /// <param name="field">The field to be cleared</param>
        public void clearDataBaseItem(Field field)
        {
            DatabaseLocation dbLocation;
            if (!fieldExists(field, out dbLocation))
                throw new DatabaseException($"Can't clear database item at database location. {dbLocation}. This location doesn't exist.");

            using (FileStream fieldStream = new FileStream(dbLocation.getFieldPath(), FileMode.OpenOrCreate, FileAccess.Write))
            {
                fieldStream.Seek(dbLocation.getFieldsSeekPosition(), SeekOrigin.Begin);
                fieldStream.Write(new byte[dbLocation.FieldLength], 0, dbLocation.FieldLength);
            }

            using (FileStream fieldDataStream = new FileStream(dbLocation.getFieldDataPath(), FileMode.OpenOrCreate, FileAccess.Write)) // Opens the field data database Stream in write mode.
            {
                fieldDataStream.Seek(dbLocation.getFieldDataSeekPosition(DbProperties), SeekOrigin.Begin);
                fieldDataStream.Write(new byte[56], 0, 56);
            }
        }
        
        /// <summary>
        /// Returns where the given field is located in the database. Return value -1 means the specified field is not included in the database.
        /// </summary>
        /// <param name="field">The field to find</param>
        /// <returns>DatabaseLocation of the field</returns>
        public DatabaseLocation findField(Field field)
        {
            byte[] compressed = field.compressField();
            int fieldLength = compressed.Length;

            string dirPath = DbProperties.getFieldDirPath(fieldLength);
            int fileCount = DbProperties.getFieldFileCount(fieldLength);

            for (int i = 0; i < fileCount; i++)
            {
                int location = findField(compressed, DatabaseLocation.getFieldPath(dirPath, i));
                if (location != -1)
                {
                    return new DatabaseLocation(DbProperties, fieldLength, i, location);
                }
            }
            
            return DatabaseLocation.NonExisting;
        }

        /// <summary>
        /// Returns where the given field is located in the specified file. Return value -1 means the specified field is not included in the file.
        /// </summary>
        /// <param name="compressed"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private int findField(byte[] compressed, string filePath)
        {
            using (FileStream fieldStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read))   // Gets the stream from the database file in read mode.
            {
                return compressed.getFieldLocation(fieldStream);
            }
        }
    }
}
