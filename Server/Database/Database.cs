using System.IO;
using Engine;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

namespace Server
{
    public class Database : IDisposable
    {
        public DatabaseProperties DbProperties;
        private FileStream[][] FieldStream;
        private FileStream[][] FieldDataStream;

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
            loadStreams();
        }

        /// <summary>
        /// Creates a new database at the given path.
        /// </summary>
        /// <param name="dbProperties"></param>
        public static Database createNew(DatabaseProperties dbProperties)
        {
            prepareNew(dbProperties);
            Database db = new Database(dbProperties.Path);
            return db;
        }

        /// <summary>
        /// Creates a new database at the given path and returns an instance of this new database.
        /// </summary>
        /// <param name="dbProperties"></param>
        public static void prepareNew(DatabaseProperties dbProperties)
        {
            if (Directory.Exists(dbProperties.Path))
                throw new DatabaseException("Database path already exists. Make sure you use a database path that doesn't exist already.");
            
            Directory.CreateDirectory(dbProperties.Path);

            byte maxSize = dbProperties.MaxFieldStorageSize;

            for (byte i = 1; i <= maxSize; i++)
            {
                string dirPath = dbProperties.getFieldDirPath(i);
                Directory.CreateDirectory(dirPath);
                File.Create(DatabaseLocation.getFieldPath(dirPath, 0)).Dispose();
                File.Create(DatabaseLocation.getFieldDataPath(dirPath, 0)).Dispose();
            }

            dbProperties.writeProperties();
        }

        private void loadStreams()
        {
            FieldStream = new FileStream[DbProperties.MaxFieldStorageSize][];
            FieldDataStream = new FileStream[DbProperties.MaxFieldStorageSize][];

            for (byte i = 1; i <= DbProperties.MaxFieldStorageSize; i++)
            {
                string dirPath = DbProperties.getFieldDirPath(i);
                if (!Directory.Exists(dirPath))
                    throw new DatabaseException($"Database incomplete. Directory {dirPath} doesn't exist.");

                int fileCount = Math.Max(1, DbProperties.getFieldFileCount(i));
                FieldStream[i - 1] = new FileStream[fileCount];
                FieldDataStream[i - 1] = new FileStream[fileCount];

                for (int j = 0; j < fileCount; j++)
                {
                    string fPath = DatabaseLocation.getFieldPath(dirPath, j);
                    FieldStream[i - 1][j] = new FileStream(fPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                    string fdPath = DatabaseLocation.getFieldDataPath(dirPath, j);
                    FieldDataStream[i - 1][j] = new FileStream(fdPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }
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

            FileStream fieldDataStream = FieldDataStream[dbLocation.FieldLength - 1][dbLocation.FileIndex];
            int seekPosition = dbLocation.getFieldDataSeekPosition(DbProperties);

            uint[] storage = new uint[DbProperties.FieldWidth * 2];
            BinaryReader br = new BinaryReader(fieldDataStream);
            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the reading position to the wanted byte (uint in our case) database.

            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // We read the uints one by one from the database.
            {
                storage[i] = br.ReadUInt32();
            }

            return new FieldData(storage);
        }

        public void processGameHistory(Dictionary<Field, FieldData> data)
        {
            // Initialization and sorting of data Dictionary.
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

            for (int i = 0; i < maxFS; i++)
            {
                // Processes the fielddata of all fields that are already included in the database.
                List<Field> matches;
                updateExistingFieldData(cData[i], i + 1, out matches);

                // Determination of the fields that need to be added to the database.
                Dictionary<int, List<Field>> newFields = getNewFieldsDictionary(i + 1, cData[i], matches);

                // Addition of new fields.
                addFields(i + 1, cData[i], newFields);
            }

            DbProperties.writeProperties();
        }

        private void addFields(int i, Dictionary<Field, FieldData> dictionary, Dictionary<int, List<Field>> newFields)
        {
            foreach (KeyValuePair<int, List<Field>> pair in newFields)
            {
                List<byte> bytes = new List<byte>();

                FileStream fieldDataStream = FieldDataStream[i - 1][pair.Key];
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

                string fPath = DatabaseLocation.getFieldPath(DbProperties.getFieldDirPath(i), pair.Key);
                FileStream fieldStream = FieldStream[i - 1][pair.Key];
                fieldStream.Seek(0, SeekOrigin.End);
                fieldStream.Write(bytes.ToArray(), 0, bytes.Count);
            }
        }

        private Dictionary<int, List<Field>> getNewFieldsDictionary(int i, Dictionary<Field, FieldData> dictionary, List<Field> matches)
        {
            Dictionary<int, List<Field>> newFields = new Dictionary<int, List<Field>>();
            //List<Field> relevant = dictionary.Keys.AsParallel().Where(f => !matches.Contains(f)).ToList();

            foreach (Field f in dictionary.Keys.AsParallel().Where(f => !matches.Contains(f)))
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

            return newFields;
        }

        private void updateExistingFieldData(Dictionary<Field, FieldData> dictionary, int i, out List<Field> matches)
        {
            ConcurrentBag<Field> fList = new ConcurrentBag<Field>();
            
            int fileCount = DbProperties.getFieldFileCount(i);

            for (int j = 0; j < fileCount; j++)
            {
                FileStream fieldStream = FieldStream[i - 1][j];
                fieldStream.Seek(0, SeekOrigin.Begin);
                FileStream fieldDataStream = FieldDataStream[i - 1][j];

                int totalLength = (int)fieldStream.Length;
                byte[] bytes = new byte[totalLength];
                fieldStream.Read(bytes, 0, totalLength);

                ConcurrentDictionary<int, FieldData> locations = new ConcurrentDictionary<int, FieldData>();

                Parallel.For(0, totalLength / i, k =>
                {
                    byte[] fStorage = new byte[i];
                    Buffer.BlockCopy(bytes, k * i, fStorage, 0, i);
                    Field f = fStorage.decompressField();

                    if (dictionary.ContainsKey(f))
                    {
                        DatabaseLocation dbLoc = new DatabaseLocation(DbProperties, i, j, k / i);
                        //locations.AddOrUpdate(dbLoc.getFieldDataSeekPosition(DbProperties), dictionary[f], );//, processFieldData(dbLoc, dictionary[f], fieldDataStream);
                        locations.GetOrAdd(dbLoc.getFieldDataSeekPosition(DbProperties), dictionary[f]);
                        fList.Add(f);
                    }
                });

                foreach (KeyValuePair<int, FieldData> l in locations)
                {
                    processFieldData(l.Key, l.Value, fieldDataStream);
                }

                /*for (int k = 0; k < totalLength; k += i)
                {
                    //byte[] fStorage = new byte[i];
                    //Buffer.BlockCopy(bytes, k, fStorage, 0, i);
                    //Field f = fStorage.decompressField();

                    //if (dictionary.ContainsKey(f))
                    //{
                    //    DatabaseLocation dbLoc = new DatabaseLocation(DbProperties, i, j, k / i);
                    //    //locations.AddOrUpdate(dbLoc.getFieldDataSeekPosition(DbProperties), dictionary[f], );//, processFieldData(dbLoc, dictionary[f], fieldDataStream);
                    //    locations.GetOrAdd(dbLoc.getFieldDataSeekPosition(DbProperties), dictionary[f]);
                    //    matches.Add(f);
                    //}
                }*/
            }

            matches = fList.ToList();
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

            FileStream fieldDataStream = FieldDataStream[dbLocation.FieldLength - 1][dbLocation.FileIndex];
            int seekPosition = dbLocation.getFieldDataSeekPosition(DbProperties);

            BinaryWriter bw = new BinaryWriter(fieldDataStream);
            uint[] storage = fieldData.getStorage();

            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the writing position to the wanted byte (uint in our case) in the database.
            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // We write each uint of the storage to the database stream.
            {
                bw.Write(storage[i]);
            }
        }

        /// <summary>
        /// Writes the given field data to an existing location in the database (location in bytes).
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <param name="fieldData">Data to be written</param>
        public void processFieldData(int seekPosition, FieldData fieldData, FileStream fieldDataStream)
        {
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

            FileStream fieldStream = FieldStream[dbLoc.FieldLength - 1][dbLoc.FileIndex];
            fieldStream.Seek(0, SeekOrigin.End);                    // Sets the writing position to the end of the database.
            fieldStream.Write(compressed, 0, compressed.Length);    // Writes the bytes of the compressed field to the database.


            FileStream fieldDataStream = FieldDataStream[dbLoc.FieldLength - 1][dbLoc.FileIndex];
            fieldDataStream.Seek(0, SeekOrigin.End);
            fieldDataStream.Write(new byte[DbProperties.FieldWidth * 8], 0, DbProperties.FieldWidth * 8);

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

            FileStream fieldStream = FieldStream[dbLocation.FieldLength - 1][dbLocation.FileIndex];
            fieldStream.Seek(dbLocation.getFieldsSeekPosition(), SeekOrigin.Begin);
            fieldStream.Write(new byte[dbLocation.FieldLength], 0, dbLocation.FieldLength);

            FileStream fieldDataStream = FieldDataStream[dbLocation.FieldLength - 1][dbLocation.FileIndex];
            fieldDataStream.Seek(dbLocation.getFieldDataSeekPosition(DbProperties), SeekOrigin.Begin);
            fieldDataStream.Write(new byte[DbProperties.FieldWidth * 8], 0, DbProperties.FieldWidth * 8);
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
            int fileCount = DbProperties.getFieldFileCount(fieldLength);

            for (int i = 0; i < fileCount; i++)
            {
                int location = findField(compressed, fieldLength, i);
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
        private int findField(byte[] compressed, int fieldLength, int fileIndex)
        {
            FileStream fieldStream = FieldStream[fieldLength - 1][fileIndex];
            return compressed.getFieldLocation(fieldStream);
        }

        public void Dispose()
        {
            for (int i = 0; i < FieldStream.Length; i++)
            {
                for (int j = 0; j < FieldStream[i].Length; j++)
                {
                    FieldStream[i][j].Dispose();
                    FieldDataStream[i][j].Dispose();
                }
            }
        }
    }
}
