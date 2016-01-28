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
using System.Threading;

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
        //public StatsManager StatsMgr;
        //private FileStream[] FieldStream;
        //private FileStream[] FieldDataStream;
        //private Dictionary<Field, int>[] Fields;    // Used to store database fields in RAM for faster access.
        internal DatabaseSegment[] Segments;
        
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
            //StatsMgr = new StatsManager(this);
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
                DatabaseSegment dbSegment = new DatabaseSegment(dbProperties.getFieldDirPath(i), dbProperties, i, false);
                DatabaseSegment.prepareNew(dbSegment);
                dbSegment.Dispose();
                //File.Create(DatabaseLocation.getFieldPath(dbProperties, i, 0)).Dispose();
                //File.Create(DatabaseLocation.getFieldDataPath(dbProperties, i, 0)).Dispose();
            }

            dbProperties.writeProperties();     // Writes the database properties to the Properties file.
        }

        /// <summary>
        /// Opens all database files to access them later on. Als loads all fields from the database into memory.
        /// </summary>
        private void loadStreams()
        {
            int maxStorageSize = DbProperties.MaxFieldStorageSize;
            //FieldStream = new FileStream[maxStorageSize];     // Creates an array of lists of filestreams in which all fields are stored.
            //FieldDataStream = new FileStream[maxStorageSize]; // Creates an array of lists of filestreams in which all field data is stored.
            //Fields = new Dictionary<Field, int>[maxStorageSize];    // Creates an array of dictionaries in which all fields are stored.
            Segments = new DatabaseSegment[maxStorageSize];

            for (byte i = 1; i <= DbProperties.MaxFieldStorageSize; i++)
            {
                string dirPath = DbProperties.getFieldDirPath(i);
                if (!Directory.Exists(dirPath)) // We can only proceed if the directories for all fieldlengths exist.
                    throw new DatabaseException($"Database incomplete. Directory {DbProperties.getFieldDirPath(i)} doesn't exist.");

                //Fields[i - 1] = new Dictionary<Field, int>(DbProperties.getLength(i));
                Segments[i - 1] = new DatabaseSegment(dirPath, DbProperties, true);
                //for (int k = 0; k < bytes.Length / i; k++)
                //{
                //Field f = fStorage.decompressField();           // Decompresses the byte array into a field.
                //DatabaseLocation dbLoc = new DatabaseLocation(DbProperties, i, j, k);
                //Fields[i - 1].Add(f, dbLoc.GlobalLocation);     // Adds the field with its corresponding global location to the dictionary that represents the fields fieldlength.
                //}

                //FieldStream[i - 1] = fStream;        // Adds the filestream to the list (and array) of the right fieldlength.
                //FieldDataStream[i - 1] = fdStream;   // Adds the filestream to the list (and array) of the right fieldlength.
            }
        }

        public int getSegmentLength(int fieldLength)
        {
            return Segments[fieldLength - 1].FieldCount;
        }

        public int getDatabaseLength()
        {
            return Segments.Sum(s => s.FieldCount);
        }
        
        public long getDatabaseSize()
        {
            long size = 0;

            int directories = DbProperties.MaxFieldStorageSize;

            for (int i = 1; i <= directories; i++)
            {
                string fieldDirPath = DbProperties.getFieldDirPath(i);
                string[] files = Directory.GetFiles(fieldDirPath);

                foreach (string file in files)
                {
                    size += new FileInfo(file).Length;
                }
            }

            return size;
        }

        public DatabaseLocation findField(Field field)
        {
            byte[] compressed = field.compressField();
            int fieldLength = compressed.Length;

            waitForProcessing(fieldLength);
            waitForAccess(fieldLength);
            setReading(fieldLength, true);

            DatabaseLocation result = Segments[fieldLength - 1].findField(compressed);

            setReading(fieldLength, false);

            return result;
        }

        /// <summary>
        /// Returns whether the given field is included in the database.
        /// </summary>
        /// <param name="field">The field to check the existance for</param>
        /// <returns>Existance of field</returns>
        public bool fieldExists(Field field)
        {
            byte[] compressed = field.compressField();
            int fieldLength = compressed.Length;
            DatabaseLocation dbLocation = findField(field);
            return Segments[fieldLength - 1].locationExists(dbLocation.Location);
        }

        /// <summary>
        /// Returns whether the given field is included in the database.
        /// </summary>
        /// <param name="field">The field to check the existance of</param>
        /// <param name="dbLocation">The location of the field within the database</param>
        /// <returns>Existance of field</returns>
        public bool fieldExists(Field field, out DatabaseLocation dbLocation)
        {
            byte[] compressed = field.compressField();
            int fieldLength = compressed.Length;
            dbLocation = findField(field);
            return Segments[fieldLength - 1].locationExists(dbLocation.Location);
        }

        /// <summary>
        /// Returns the content of a specified range (of fields) in the database for the given fieldLength. (Last item of range included)
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <param name="beginRange">Startpoint of the range</param>
        /// <param name="endRange">Endpoint of the range</param>
        /// <returns>Range of compressed fields</returns>
        public byte[] getCompressedFieldRange(int fieldLength, int beginRange, int endRange)
        {
            waitForAccess(fieldLength);
            setReading(fieldLength, true);

            byte[] result = Segments[fieldLength - 1].getCompressedFieldRange(beginRange, endRange);

            setReading(fieldLength, false);

            return result;
        }

        /// <summary>
        /// Returns the field stored at the specified database location.
        /// </summary>
        /// <param name="dbLocation"></param>
        /// <returns>The requested field</returns>
        public Field readField(DatabaseLocation dbLocation)
        {
            DatabaseSegment dbSeg = dbLocation.getDatabaseSegment();

            dbSeg.setReading(true);

            Field field = dbSeg.readField(dbLocation.Location);

            dbSeg.setReading(false);

            return field;
        }

        /// <summary>
        /// Reads the field data of the given field from the database.
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
            DatabaseSegment dbSeg = dbLocation.getDatabaseSegment();

            waitForAccess(dbLocation.getFieldLength());
            dbSeg.setReading(true);

            FieldData fd = dbSeg.readFieldData(dbLocation.Location);

            dbSeg.setReading(false);

            return fd;
        }
        /*
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

        public void writeFieldData(DatabaseLocation dbLocation, FieldData fieldData)
        {
            DatabaseSegment dbSeg = dbLocation.getDatabaseSegment();
            dbSeg.writeFieldData(dbLocation.Location, fieldData);
        }*/
        
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
            throw new NotImplementedException();
            /*byte[] compressed = field.compressField();  // Gets the compressed field.
            int fieldLength = compressed.Length;
            return Segments[fieldLength - 1].addItem(field, fieldData);*/
        }
        
        public void mergeWithBuffers(params DatabaseSegment[] bufferSegs)
        {
            int fieldLength = bufferSegs[0].FieldLength;
            DatabaseSegment oldSegment = Segments[fieldLength - 1];

            waitForProcessing(fieldLength);
            oldSegment.setProcessing(true);

            waitForAccess(fieldLength);
            
            int bufferCount = bufferSegs.Length;

            DatabaseSegment[] allSegs = new DatabaseSegment[bufferCount + 1];

            for (int i = 0; i < bufferSegs.Length; i++)
                allSegs[i] = bufferSegs[i];
            
            allSegs[bufferCount] = oldSegment;

            string resultPath = DbProperties.getFieldDirPath(fieldLength) + "-new";
            DatabaseSegment resultSeg = new DatabaseSegment(resultPath, DbProperties, fieldLength, false);
            DatabaseSegment.prepareNew(resultSeg);

            SegmentMerger.merge(resultSeg, allSegs);

            string newPath = DbProperties.getFieldDirPath(fieldLength);

            oldSegment.Dispose();
            resultSeg.Dispose();

            Directory.Delete(newPath, true);
            Directory.Move(resultPath, newPath);

            DatabaseSegment newSeg = new DatabaseSegment(newPath, DbProperties, true);
            Segments[fieldLength - 1] = newSeg;
        }

        public bool isReading(int fieldLength)
        {
            return Segments[fieldLength - 1].isReading();
        }

        public bool isProcessing(int fieldLength)
        {
            return Segments[fieldLength - 1].isProcessing();
        }

        public void setReading(int fieldLength, bool status)
        {
            Segments[fieldLength - 1].setReading(status);
        }

        public void setProcessing(int fieldLength, bool status)
        {
            Segments[fieldLength - 1].setProcessing(status);
        }

        public void waitForAccess(int fieldLength)
        {
            if (Segments[fieldLength - 1].isReading())
            {
                logger.Debug($"The database segment of field length {fieldLength} is reading and can't be accessed right now. Waiting for access");
                while (Segments[fieldLength - 1].isReading())
                    Thread.Sleep(100);
            }
        }

        public void waitForProcessing(int fieldLength)
        {
            if (Segments[fieldLength - 1].isProcessing())
            {
                logger.Debug($"The database segment of field length {fieldLength} is processing and can't be accessed right now. Waiting for processing operation to finish");
                while (Segments[fieldLength - 1].isProcessing())
                    Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Disposes all filestreams that have been opened by the server.
        /// </summary>
        public void Dispose()
        {
            foreach (DatabaseSegment dbSeg in Segments)
                dbSeg.Dispose();
        }
    }
}
