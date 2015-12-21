using System.IO;
using Engine;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Diagnostics;
using NLog;

namespace Server
{
    /// <summary>
    /// A database object used to read and write the database format.
    /// </summary>
    public class Database : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public DatabaseProperties DbProperties;
        public BufferManager BufferMgr;
        private List<FileStream>[] FieldStream;
        private List<FileStream>[] FieldDataStream;
        private Dictionary<Field, int>[] Fields;    // Used to store database fields in RAM for faster access.

        private bool Busy;  // Indicates whether the database is busy or not. Mainly used by RequestHandler to know if it's possible to start a database operation.

        /// <summary>
        /// Creates a new database instance from the given path.
        /// </summary>
        /// <param name="path">Database storage path</param>
        public Database(string path)
        {
            if (!Directory.Exists(path))
                throw new DatabaseException($"Database doesn't exist. The given database directory doesn´t exist ({path})");
            
            string[] files = Directory.GetFiles(path);

            if (files.Length > 0)
            {
                string propertiesPath = files[0];
                if (!propertiesPath.EndsWith("Properties") || !File.Exists(propertiesPath)) // If the Properties file is in the database root directory we can proceed.
                    throw new DatabaseException($"Database incomplete. Properties file not found in: {path}");
            }
            else
                throw new DatabaseException($"Database incomplete. Properties file not found in: {path}");
            
            DbProperties = new DatabaseProperties(path);
            loadStreams();

            BufferMgr = new BufferManager(this);
        }

        /// <summary>
        /// Creates a new database at the given path.
        /// </summary>
        /// <param name="dbProperties"></param>
        public static Database createNew(DatabaseProperties dbProperties)
        {
            prepareNew(dbProperties);   // Prepares the directories and files for a new database.
            Database db = new Database(dbProperties.Path);
            return db;  // Returns the newly made database.
        }

        /// <summary>
        /// Creates a new database at the given path and returns an instance of this new database.
        /// </summary>
        /// <param name="dbProperties"></param>
        public static void prepareNew(DatabaseProperties dbProperties)
        {
            if (Directory.Exists(dbProperties.Path))
                throw new DatabaseException("Database path already exists. Make sure you use a database path that doesn't exist already.");

            Directory.CreateDirectory(dbProperties.Path);   // Creates the root directory of the database.

            byte maxSize = dbProperties.MaxFieldStorageSize;
            string dbPath = dbProperties.Path;

            for (byte i = 1; i <= maxSize; i++) // Creates new directories for all possible fieldlengths and the first Fields and FieldData file.
            {
                Directory.CreateDirectory(dbProperties.getFieldDirPath(i));
                File.Create(DatabaseLocation.getFieldPath(dbProperties, i, 0)).Dispose();
                File.Create(DatabaseLocation.getFieldDataPath(dbProperties, i, 0)).Dispose();
            }

            dbProperties.writeProperties();     // Writes the database properties to the Properties file.
        }

        /// <summary>
        /// Opens all database files to access them later on. Als loads all fields from the database into memory.
        /// </summary>
        private void loadStreams()
        {
            int maxStorageSize = DbProperties.MaxFieldStorageSize;
            FieldStream = new List<FileStream>[maxStorageSize];     // Creates an array of lists of filestreams in which all fields are stored.
            FieldDataStream = new List<FileStream>[maxStorageSize]; // Creates an array of lists of filestreams in which all field data is stored.
            Fields = new Dictionary<Field, int>[maxStorageSize];    // Creates an array of dictionaries in which all fields are stored.

            for (byte i = 1; i <= DbProperties.MaxFieldStorageSize; i++)
            {
                string dirPath = DbProperties.getFieldDirPath(i);
                if (!Directory.Exists(dirPath)) // We can only proceed if the directories for all fieldlengths exist.
                    throw new DatabaseException($"Database incomplete. Directory {DbProperties.getFieldDirPath(i)} doesn't exist.");

                int fileCount = Math.Max(1, DbProperties.getFieldFileCount(i));
                FieldStream[i - 1] = new List<FileStream>();
                FieldDataStream[i - 1] = new List<FileStream>();
                Fields[i - 1] = new Dictionary<Field, int>();

                for (int j = 0; j < fileCount; j++)
                {
                    string fPath = DatabaseLocation.getFieldPath(DbProperties, i, j);
                    FileStream fStream = new FileStream(fPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);    // Creates a new fields filestream belonging to a certain fieldlength and fileindex.

                    string fdPath = DatabaseLocation.getFieldDataPath(DbProperties, i, j);
                    FileStream fdStream = new FileStream(fdPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);  // Creates a new fields filestream belonging to a certain fieldlength and fileindex.

                    byte[] bytes = new byte[fStream.Length];
                    fStream.Read(bytes, 0, bytes.Length);   // We copy the content of the filestream to a byte array.

                    for (int k = 0; k < bytes.Length / i; k++)
                    {
                        byte[] fStorage = new byte[i];
                        Buffer.BlockCopy(bytes, k * i, fStorage, 0, i); // Takes a part of the byte array in which a compressed field is stored.
                        Field f = fStorage.decompressField();           // Decompresses the byte array into a field.
                        DatabaseLocation dbLoc = new DatabaseLocation(DbProperties, i, j, k);
                        Fields[i - 1].Add(f, dbLoc.GlobalLocation);     // Adds the field with its corresponding global location to the dictionary that represents the fields fieldlength.
                    }

                    FieldStream[i - 1].Add(fStream);        // Adds the filestream to the list (and array) of the right fieldlength.
                    FieldDataStream[i - 1].Add(fdStream);   // Adds the filestream to the list (and array) of the right fieldlength.
                }
            }
        }

        /// <summary>
        /// Returns whether the given field is included in the database.
        /// </summary>
        /// <param name="field">The field to check the existance for</param>
        /// <returns>Existance of field</returns>
        public bool fieldExists(Field field)
        {
            int i = field.compressField().Length;
            return Fields[i - 1].ContainsKey(field);    // Returns whether the dictionary contains the specified field.
        }

        /// <summary>
        /// Returns whether the given field is included in the database.
        /// </summary>
        /// <param name="field">The field to check the existance of</param>
        /// <param name="dbLocation">The location of the field within the database</param>
        /// <returns>Existance of field</returns>
        public bool fieldExists(Field field, out DatabaseLocation dbLocation)
        {
            int i = field.compressField().Length;
            bool exists = Fields[i - 1].ContainsKey(field); // Indicates whether the dictionary contains the specified field.
            if (exists)
                dbLocation = new DatabaseLocation(DbProperties, i, Fields[i - 1][field]);   // If the field exists the database location is returned via the 'out' keyword.
            else
                dbLocation = DatabaseLocation.NonExisting;

            return exists;
        }

        /// <summary>
        /// Returns the content of a specified range (of fields) in the database for the given fieldLength. (Last item of range included)
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <param name="beginRange">Startpoint of the range</param>
        /// <param name="endRange">Endpoint of the range</param>
        /// <returns>Range of compressed fields</returns>
        public byte[] getFieldFileContent(int fieldLength, int beginRange, int endRange)
        {
            int maxLoc = DbProperties.getLength(fieldLength) - 1;   // Gets the last possible location to read.

            if (beginRange > maxLoc || endRange > maxLoc)
                throw new DatabaseException("The given range of fields reaches beyond the end of the database.");

            if (beginRange < 0 || endRange < 0)
                throw new DatabaseException("The given range of fields contains negative locations");

            if (endRange < beginRange)
                throw new DatabaseException("The given end location is smaller than the begin location.");

            byte[] result = new byte[(endRange - beginRange + 1) * fieldLength];    // Creates a new byte array in which all bytes of the given range fit.
            int arrayPos = 0;

            DatabaseLocation beginLoc = new DatabaseLocation(DbProperties, fieldLength, beginRange);    // Begin database location of range.
            DatabaseLocation endLoc = new DatabaseLocation(DbProperties, fieldLength, endRange + 1);    // End database location of range.

            int globalLoc = beginRange;
            int maxFieldsInFile = DbProperties.getMaxFieldsInFile(fieldLength);

            while (globalLoc < endRange)
            {
                DatabaseLocation dbLoc = new DatabaseLocation(DbProperties, fieldLength, globalLoc);

                int start = dbLoc.getFieldsSeekPosition();  // Gets the seek position of the start location.
                int end = 0;

                int fileIndex = dbLoc.FileIndex;
                int endFileIndex = endLoc.FileIndex;

                if (fileIndex < endFileIndex)
                    end = maxFieldsInFile * fieldLength;    // As long as fileIndex is smaller than endFileIndex the end seek position is the end of the file.
                else
                    end = endLoc.getFieldsSeekPosition();   // Else the end seek position is the seek position of the end location.

                // Here we copy the needed bytes from the filestream to the result byte array.
                int count = end - start;

                FieldStream[fieldLength - 1][fileIndex].Seek(start, SeekOrigin.Begin);  // Seeks to the wanted part of the filestream.
                FieldStream[fieldLength - 1][fileIndex].Read(result, arrayPos, count);  // Copies the wanted amount of bytes from the filestream to the result array.

                arrayPos += count;                  // Raises the array write position to the end of the written part of the array.
                globalLoc += count / fieldLength;   // Raises the global location to the begin of the next file.
            }

            return result;
        }

        /// <summary>
        /// Reads the field data from of the given field the database.
        /// </summary>
        /// <param name="field">Field to read the data from</param>
        /// <returns></returns>
        public FieldData readFieldData(Field field)
        {
            DatabaseLocation location;
            if (fieldExists(field, out location))   // Checks whether the field is included in the database.
                return readFieldData(location);     // If so, returns the field data belonging to the given field.
            else
                throw new DatabaseException($"Can't read field data, because the given field doesn't exist.");
        }

        /// <summary>
        /// Reads the field data from the database at the specified database location.
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <returns>The requested field data</returns>
        public FieldData readFieldData(DatabaseLocation dbLocation)
        {
            FileStream fieldDataStream = FieldDataStream[dbLocation.FieldLength - 1][dbLocation.FileIndex]; // Gets the needed filestream.
            int seekPosition = dbLocation.getFieldDataSeekPosition();   // Gets the seekposition for the specified database location.

            uint[] storage = new uint[DbProperties.FieldWidth * 2];
            BinaryReader br = new BinaryReader(fieldDataStream);
            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);       // Sets the reading position to the wanted byte (uint in our case) database.

            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)      // We read the uints one by one from the database.
            {
                storage[i] = br.ReadUInt32();
            }

            return new FieldData(storage);      // We return the read field data as a field data object.
        }

        /// <summary>
        /// Writes the given field data to the database for the specified field.
        /// </summary>
        /// <param name="field">The field to save the data for</param>
        /// <param name="fieldData">The field data to write</param>
        public void writeFieldData(Field field, FieldData fieldData)
        {
            DatabaseLocation location;
            if (fieldExists(field, out location))       // Checks whether the field is included in the database.
                writeFieldData(location, fieldData);    // If so, returns the field data belonging to the given field.
            else
                throw new DatabaseException($"Can't write field data, because the given field doesn't exist.");
        }

        /// <summary>
        /// Writes the given field data to the database at the specified database location.
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <param name="fieldData">Data to be written</param>
        public void writeFieldData(DatabaseLocation dbLocation, FieldData fieldData)
        {
            FileStream fieldDataStream = FieldDataStream[dbLocation.FieldLength - 1][dbLocation.FileIndex]; // Gets the needed filestream.
            int seekPosition = dbLocation.getFieldDataSeekPosition();   // Gets the seekposition for the specified database location.

            BinaryWriter bw = new BinaryWriter(fieldDataStream);
            uint[] storage = fieldData.getStorage();                    // Gets the storage of the specifield field data object to write it to the fieldDataStream.

            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);       // Sets the writing position to the wanted byte (uint in our case) in the database.
            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)      // We write each uint of the storage to the database stream.
            {
                bw.Write(storage[i]);
            }
        }

        /// <summary>
        /// Allocates a new database location for the specified field by increasing the length of the database.
        /// </summary>
        /// <param name="f">The field that's going to be added</param>
        /// <returns>The database location for the new field</returns>
        private DatabaseLocation allocateNextDatabaseLocation(Field f)
        {
            return allocateNextDatabaseLocation(f.compressField().Length);
        }

        /// <summary>
        /// Allocates a new database location for the specified field length by increasing the length of the database.
        /// </summary>
        /// <param name="fieldLength">The length of the field that's going to be added</param>
        /// <returns>The database location for the new field</returns>
        private DatabaseLocation allocateNextDatabaseLocation(int fieldLength)
        {
            DbProperties.increaseLength(fieldLength, false);    // Increases the length for the specified fieldlength, without writing it to the Properties file immediately.
            int fileIndex = DbProperties.getFieldFileCount(fieldLength) - 1;
            return new DatabaseLocation(DbProperties, fieldLength, fileIndex, DbProperties.getLength(fieldLength) % DbProperties.getMaxFieldsInFile(fieldLength) - 1);  // Returns the location that has been allocated.
        }

        /// <summary>
        /// Writes the given field data to an existing location in the database (location in bytes).
        /// </summary>
        /// <param name="seekPosition">Location in field data file in bytes</param>
        /// <param name="fieldData">Data that needs to be merged with the existing field data</param>
        /// <param name="fieldDataStream"></param>
        public void processFieldData(int fieldLength, int globalLocation, FieldData fieldData)
        {
            DatabaseLocation loc = new DatabaseLocation(DbProperties, fieldLength, globalLocation); // Gets the database location belonging to the given database location.
            int seekPosition = loc.getFieldDataSeekPosition();  // Gets the seek position of the fieldDataStream to write the given field data at.

            FileStream fieldDataStream = FieldDataStream[fieldLength - 1][loc.FileIndex];   // Gets the right fieldDataStream.
            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Seeks to the calculated seek position of the fieldDataStream.

            BinaryReader br = new BinaryReader(fieldDataStream);

            uint[] storage = fieldData.getStorage();                // Gets the uint array storage of the field data object.

            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // Adds the field data from the database to the corresponding given field data.
            {
                storage[i] += br.ReadUInt32();
            }

            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);   // Sets the writing position to the wanted byte (uint in our case) in the database.

            BinaryWriter bw = new BinaryWriter(fieldDataStream);

            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)  // Writes each uint of the storage to the database stream.
            {
                bw.Write(storage[i]);
            }
        }

        /// <summary>
        /// Adds the given field to the database with empty field data.
        /// </summary>
        /// <param name="field">field to be added</param>
        /// <returns>The DatabaseLocation of the new field</returns>
        public DatabaseLocation addDatabaseItem(Field field)
        {
            return addDatabaseItem(field, new FieldData());
        }

        /// <summary>
        /// Adds the given field and its corresponding field data to the database.
        /// </summary>
        /// <param name="field">Field to be added</param>
        /// <param name="fieldData">Field data corresponding to the field</param>
        /// <returns>The DatabaseLocation of the new field</returns>
        public DatabaseLocation addDatabaseItem(Field field, FieldData fieldData)
        {
            byte[] compressed = field.compressField();  // Gets the compressed field.
            int length = compressed.Length;
            DatabaseLocation dbLoc = allocateNextDatabaseLocation(length);  // Gets a new database location for the right fieldlength.

            Fields[length - 1].Add(field, dbLoc.GlobalLocation);    // Adds the field with its corresponding global location to the right fields dictionary.

            FileStream fieldStream = FieldStream[length - 1][dbLoc.FileIndex];  // Gets the right fieldStream.
            fieldStream.Seek(0, SeekOrigin.End);        // Seeks to the end of the database.
            fieldStream.Write(compressed, 0, length);   // Writes the bytes of the compressed field to the database.

            uint[] fdStorage = fieldData.getStorage();  // Gets the uint storage from the specified field data object.

            FileStream fieldDataStream = FieldDataStream[dbLoc.FieldLength - 1][dbLoc.FileIndex];   // Gets the right fieldDataStream.
            fieldDataStream.Seek(0, SeekOrigin.End);    // Seeks to the end of the database.

            BinaryWriter bw = new BinaryWriter(fieldDataStream);

            for (int i = 0; i < fdStorage.Length; i++)  // Writes each uint of the uint storage array to the end of the database.
            {
                bw.Write(fdStorage[i]);
            }
            
            return dbLoc;   // Returns the database location of the added field.
        }

        /// <summary>
        /// Processes a dictionary of game history with help of updateExistingFieldData, addFields and processFieldData.
        /// </summary>
        /// <param name="data">Game history as a dictionary</param>
        public void processGameHistory(Dictionary<Field, FieldData> data)
        {
            // Initialization and sorting of data Dictionary.
            byte maxFS = DbProperties.calculateMaxFieldStorageSize();
            Dictionary<Field, FieldData>[] cData = new Dictionary<Field, FieldData>[maxFS];
            for (byte i = 0; i < maxFS; i++)
            {
                cData[i] = new Dictionary<Field, FieldData>();
            }

            foreach (KeyValuePair<Field, FieldData> pair in data)   // Sorts the items of the data dictionary by fieldlength.
            {
                int length = pair.Key.compressField().Length;
                cData[length - 1].Add(pair.Key, pair.Value);
            }

            for (int i = 1; i <= maxFS; i++)    // Loops through all possible fieldlengths to update existing field data and add new field data if needed.
            {
                // Processes the fielddata of all fields that are already included in the database.
                Dictionary<Field, FieldData> newFields;
                updateExistingFieldData(i, cData[i - 1], out newFields);

                // Addition of new fields.
                if (newFields.Any())
                    addFields(i, newFields);
            }

            DbProperties.writeProperties(); // Saves the Properties to the properties file.
            flushFileStreams();             // Flushes all the filestreams.
        }

        /// <summary>
        /// Adds the given dictionary of fields to the database for the specified fieldlength.
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <param name="newFields">Dictionary of fields to add</param>
        private void addFields(int fieldLength, Dictionary<Field, FieldData> newFields)
        {
            int fileIndex = FieldStream[fieldLength - 1].Count - 1; // Gets the fileIndex of the filestreams to add the new fields and its data to.

            List<byte> fields = new List<byte>();
            List<uint> fieldData = new List<uint>();

            foreach (KeyValuePair<Field, FieldData> pair in newFields)
            {
                DatabaseLocation newLoc = allocateNextDatabaseLocation(pair.Key);   // Allocates a new database location.

                Fields[fieldLength - 1].Add(pair.Key, newLoc.GlobalLocation);       // Adds a new field with its corresponding global location to the right fields dictionary.

                if (fileIndex < newLoc.FileIndex)   // If this is true the buffered fields and its field data need to be written to the right files.
                {
                    writeBuffers(FieldStream[fieldLength - 1][fileIndex], fields, FieldDataStream[fieldLength - 1][fileIndex], fieldData);  // Writes the buffers to the right filestreams.

                    fields.Clear();     // Clears the fields buffer.
                    fieldData.Clear();  // Clears the field data buffer.

                    addFileStreams(fieldLength);    // Adds a new fieldStream and a corresponding fieldDataStream to the database to write the next buffer in.
                    fileIndex = newLoc.FileIndex;   // Updates the fileIndex to the fileIndex of the new location.
                }

                fields.AddRange(pair.Key.compressField());  // Adds the compressed field to the fields buffer.

                uint[] fdStorage = pair.Value.getStorage();
                fieldData.AddRange(fdStorage); // Adds the field data to the field data buffer.
            }

            if (fields.Count > 0)   // If there are fields and field data that hasn't been buffered yet, it's buffer right now.
            {
                writeBuffers(FieldStream[fieldLength - 1][fileIndex], fields, FieldDataStream[fieldLength - 1][fileIndex], fieldData);  // Writes the buffers to the right filestreams.
            }
        }

        /// <summary>
        /// Writes field buffers and field data buffers to the given (corresponding filestreams).
        /// </summary>
        /// <param name="fieldStream"></param>
        /// <param name="fieldsBuffer"></param>
        /// <param name="fieldDataStream"></param>
        /// <param name="fieldDataBuffer"></param>
        private void writeBuffers(FileStream fieldStream,List<byte> fieldsBuffer, FileStream fieldDataStream, List<uint> fieldDataBuffer)
        {
            fieldStream.Seek(0, SeekOrigin.End);    // Seeks to the end of the fieldStream.
            fieldStream.Write(fieldsBuffer.ToArray(), 0, fieldsBuffer.Count);   // Writes the buffered fields to the right fieldStream.

            fieldDataStream.Seek(0, SeekOrigin.End);    // Seeks to the end of the fieldDataStream.
            BinaryWriter bw1 = new BinaryWriter(fieldDataStream);   // Writes the buffered field data to the right fieldDataStream.

            foreach (uint u in fieldDataBuffer)   // Writes each buffered uint to the right fieldDataStream.
            {
                bw1.Write(u);
            }
        }
        
        /// <summary>
        /// Updates the field data of all existing fields in the database with help of processFieldData. Returns a dictionary of fields that aren't included in the database yet.
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <param name="dictionary">Fields of which to update field data</param>
        /// <param name="newFields">Returns the fields that don't exist</param>
        private void updateExistingFieldData(int fieldLength, Dictionary<Field, FieldData> dictionary, out Dictionary<Field, FieldData> newFields)
        {
            newFields = new Dictionary<Field, FieldData>();

            foreach (KeyValuePair<Field, FieldData> p in dictionary)    // Copies the content of dictionary to the newFields dictionary.
                newFields.Add(p.Key, p.Value);

            int fileCount = DbProperties.getFieldFileCount(fieldLength);

            for (int j = 0; j < fileCount; j++) // Searches through all field dictionaries to find fields to update.
            {
                foreach (KeyValuePair<Field, FieldData> p in dictionary.AsParallel().Where(f => Fields[fieldLength - 1].ContainsKey(f.Key)))
                {
                    int loc = Fields[fieldLength - 1][p.Key];       // Gets the global location of the field to update.
                    processFieldData(fieldLength, loc, p.Value);    // Updates the field data.
                    newFields.Remove(p.Key);    // Removes the existing field from the newFields dictionary, because it doesn't need to be added.
                }
            }
        }

        /// <summary>
        /// Sets the 'busy' indicator to the given value.
        /// </summary>
        /// <param name="busy"></param>
        public void setBusy(bool busy)
        {
            Busy = busy;
        }

        /// <summary>
        /// Returns whether the database is busy at the moment. (According to the 'busy' indicator)
        /// </summary>
        /// <returns></returns>
        public bool isBusy()
        {
            return Busy;
        }
        
        /// <summary>
        /// Adds new filestreams for fields and field data for the specified fieldlength.
        /// </summary>
        /// <param name="fieldLength"></param>
        private void addFileStreams(int fieldLength)
        {
            int fileIndex = FieldStream[fieldLength - 1].Count;

            string newFieldPath = DatabaseLocation.getFieldPath(DbProperties, fieldLength, fileIndex);
            FileStream fStream = new FileStream(newFieldPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            FieldStream[fieldLength - 1].Add(fStream);

            string newDataPath = DatabaseLocation.getFieldDataPath(DbProperties, fieldLength, fileIndex);
            FileStream fdStream = new FileStream(newDataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            FieldDataStream[fieldLength - 1].Add(fdStream);
        }

        /// <summary>
        /// Flushes the buffered data of all filestreams belonging to the database.
        /// </summary>
        private void flushFileStreams()
        {
            for (int i = 0; i < FieldStream.Length; i++)
            {
                foreach (FileStream fs in FieldStream[i])
                {
                    fs.Flush();
                }

                foreach (FileStream fs in FieldDataStream[i])
                {
                    fs.Flush();
                }
            }
        }

        /// <summary>
        /// Flushes the buffered data of all filestreams belonging to the database with the specified fieldlength.
        /// </summary>
        /// <param name="fieldLength"></param>
        private void flushFileStreams(byte fieldLength)
        {
            foreach (FileStream fs in FieldStream[fieldLength - 1])
            {
                fs.Flush();
            }

            foreach (FileStream fs in FieldDataStream[fieldLength - 1])
            {
                fs.Flush();
            }
        }

        /// <summary>
        /// Disposes all filestreams that have been opened by the server.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < FieldStream.Length; i++)
            {
                foreach (FileStream fs in FieldStream[i])
                {
                    fs.Dispose();
                }

                foreach (FileStream fs in FieldDataStream[i])
                {
                    fs.Dispose();
                }
            }
        }
    }
}
