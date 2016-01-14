using Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class DatabaseSegment : IDisposable
    {
        public readonly string Path;
        private DatabaseProperties DbProperties;
        public readonly int FieldLength;
        public readonly bool KeepInMemory;
        public int FieldCount;
        private byte[] Fields;
        private MemoryStream FieldsBuffer;
        private BinaryWriter FieldDataBuffer;
        private const int MaxBufferSize = 10000;

        private FileStream FieldStream;
        private FileStream FieldDataStream;

        private bool Reading;
        private bool Processing;

        public DatabaseSegment(string path, DatabaseProperties dbProperties, bool keepInMemory = false)
        {
            this.Path = path;
            this.DbProperties = dbProperties;

            this.KeepInMemory = keepInMemory;

            string propertiesPath = getPropertiesPath();

            if (Directory.Exists(path))
            {
                if (!File.Exists(propertiesPath))
                    throw new DatabaseException("Segment properties file not found!");
            }
            else
                throw new DatabaseException("Segment directory doesn't exist!");

            using (FileStream propertiesStream = new FileStream(propertiesPath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader br = new BinaryReader(propertiesStream);
                FieldLength = br.ReadInt32();
                FieldCount = br.ReadInt32();
            }

            FieldStream = new FileStream(getFieldsPath(), FileMode.Open, FileAccess.ReadWrite);
            FieldDataStream = new FileStream(getFieldDataPath(), FileMode.Open, FileAccess.ReadWrite);

            if (keepInMemory)
            {
                Fields = new byte[FieldStream.Length];
                FieldStream.Read(Fields, 0, Fields.Length);
            }

            FieldsBuffer = new MemoryStream(getFieldsSeekPosition(MaxBufferSize));
            MemoryStream fdFileStream = new MemoryStream(getFieldDataSeekPosition(MaxBufferSize));
            FieldDataBuffer = new BinaryWriter(fdFileStream);

            setReading(false);
            setProcessing(false);
        }

        public DatabaseSegment(string path, DatabaseProperties dbProperties, int fieldLength, bool keepInMemory = false)
        {
            this.Path = path;
            this.DbProperties = dbProperties;

            this.FieldLength = fieldLength;
            this.FieldCount = 0;
            this.KeepInMemory = keepInMemory;

            if (keepInMemory)
                this.Fields = new byte[0];

            FieldsBuffer = new MemoryStream(getFieldsSeekPosition(MaxBufferSize));
            MemoryStream fdFileStream = new MemoryStream(getFieldDataSeekPosition(MaxBufferSize));
            FieldDataBuffer = new BinaryWriter(fdFileStream);

            setReading(false);
            setProcessing(false);
        }

        public static void prepareNew(DatabaseSegment dbSegment, Dictionary<Field, FieldData> content = null)
        {
            if (Directory.Exists(dbSegment.Path))
                throw new DatabaseException($"Can't create a new database segment at \"{dbSegment.Path}\", because the directory already exists.");

            Directory.CreateDirectory(dbSegment.Path);

            dbSegment.FieldStream = new FileStream(dbSegment.getFieldsPath(), FileMode.OpenOrCreate, FileAccess.ReadWrite);
            dbSegment.FieldDataStream = new FileStream(dbSegment.getFieldDataPath(), FileMode.OpenOrCreate, FileAccess.ReadWrite);

            if (content != null)
            {
                Dictionary<byte[], uint[]> buffer = new Dictionary<byte[], uint[]>();

                foreach (KeyValuePair<Field, FieldData> pair in content)
                {
                    byte[] fComp = pair.Key.compressField();
                    uint[] fdStorage = pair.Value.getStorage();
                    buffer.Add(fComp, fdStorage);
                }

                int fdSize = 2 * dbSegment.getFieldWidth();

                byte[] fieldsBuffer = new byte[buffer.Count * dbSegment.FieldLength];

                BinaryWriter bw = new BinaryWriter(dbSegment.FieldDataStream);

                int i = 0;
                foreach (KeyValuePair<byte[], uint[]> pair in buffer.OrderBy(p => p.Key, new CompressedCompare()))
                {
                    Buffer.BlockCopy(pair.Key, 0, fieldsBuffer, i * dbSegment.FieldLength, dbSegment.FieldLength);

                    foreach (uint u in pair.Value)
                        bw.Write(u);
                    i++;
                }

                dbSegment.FieldStream.Write(fieldsBuffer, 0, fieldsBuffer.Length);
                dbSegment.FieldCount = content.Count;
            }

            dbSegment.writeProperties();
        }

        private string getPropertiesPath()
        {
            return Path + DbProperties.PathSeparator + "Properties";
        }

        public string getFieldsPath()
        {
            return Path + DbProperties.PathSeparator + "Fields.db";
        }

        public string getFieldDataPath()
        {
            return Path + DbProperties.PathSeparator + "FieldData.db";
        }

        public byte getFieldWidth()
        {
            return DbProperties.FieldWidth;
        }

        /// <summary>
        /// Returns whether the given location is an existing (valid to use) location.
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <returns></returns>
        public bool locationExists(int location)
        {
            return location >= 0 && location < FieldCount;
        }

        internal DatabaseLocation findField(Field field)
        {
            return findField(field.compressField());
        }

        internal DatabaseLocation findField(byte[] compressed)
        {
            int leftRange = 0;
            int rightRange = FieldCount;

            if (leftRange == rightRange)
                return DatabaseLocation.NonExisting;

            while (true)
            {
                int checkLoc = (leftRange + rightRange) / 2;
                bool found = true;

                byte[] checkArray = readCompressedField(checkLoc);

                for (int i = 0; i < FieldLength; i++)
                {
                    if (compressed[i] < checkArray[i])
                    {
                        if (rightRange == checkLoc)
                            return DatabaseLocation.NonExisting;
                        rightRange = checkLoc;
                        found = false;
                        break;
                    }
                    else if (compressed[i] > checkArray[i])
                    {
                        if (leftRange == checkLoc)
                            return DatabaseLocation.NonExisting;
                        leftRange = checkLoc;
                        found = false;
                        break;
                    }
                }

                if (found)
                    return new DatabaseLocation(this, checkLoc);
            }
        }

        /// <summary>
        /// Returns the content of a specified range (of fields) in the database for the given fieldLength. (Last item of range included)
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <param name="beginRange">Startpoint of the range</param>
        /// <param name="endRange">Endpoint of the range</param>
        /// <returns>Range of compressed fields</returns>
        public byte[] getCompressedFieldRange(int beginRange, int endRange)
        {
            int maxLoc = FieldCount - 1;   // Gets the last possible location to read.

            if (beginRange > maxLoc || endRange > maxLoc)
                throw new DatabaseException("The given range of fields reaches beyond the end of the database.");

            if (beginRange < 0 || endRange < 0)
                throw new DatabaseException("The given range of fields contains negative locations");

            if (endRange < beginRange)
                throw new DatabaseException("The given end location is smaller than the begin location.");

            byte[] result = new byte[(endRange - beginRange + 1) * FieldLength];    // Creates a new byte array in which all bytes of the given range fit.

            if (KeepInMemory)
            {
                Buffer.BlockCopy(Fields, beginRange, result, 0, result.Length);
            }
            else
            {
                FieldStream.Seek(0, SeekOrigin.Begin);
                FieldStream.Read(result, 0, result.Length);
            }

            return result;
        }

        /// <summary>
        /// Returns the byte position of the location.
        /// </summary>
        /// <returns>Seek position</returns>
        public int getFieldsSeekPosition(int location)
        {
            return location * FieldLength;
        }

        /// <summary>
        /// Returns the position (in bytes) in the field data database where the data corresponding to the given field location is stored.
        /// </summary>
        /// <param name="fieldLocation">The location of the field</param>
        /// <param name="fieldDataStream">The field data database stream</param>
        /// <returns></returns>
        public int getFieldDataSeekPosition(int location)
        {
            return location * getFieldWidth() * 8;
        }

        public byte[] readCompressedField(int location)
        {
            int seekPosition = getFieldsSeekPosition(location);

            byte[] fCompressed = new byte[FieldLength];

            if (KeepInMemory)
            {
                Buffer.BlockCopy(Fields, seekPosition, fCompressed, 0, FieldLength);
            }
            else
            {
                FieldStream.Seek(seekPosition, SeekOrigin.Begin);
                FieldStream.Read(fCompressed, 0, FieldLength);
            }

            return fCompressed;
        }

        public Field readField(int location)
        {
            return readCompressedField(location).decompressField();
        }

        public FieldData readFieldData(int location)
        {
            int seekPosition = getFieldDataSeekPosition(location);   // Gets the seekposition for the specified database location.

            uint[] storage = new uint[DbProperties.FieldWidth * 2];
            BinaryReader br = new BinaryReader(FieldDataStream);
            FieldDataStream.Seek(seekPosition, SeekOrigin.Begin);       // Sets the reading position to the wanted byte (uint in our case) database.

            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)      // We read the uints one by one from the database.
            {
                storage[i] = br.ReadUInt32();
            }

            return new FieldData(storage);      // We return the read field data as a field data object.
        }

        /// <summary>
        /// Writes the given field data to the database at the specified database location.
        /// </summary>
        /// <param name="dbLocation">Field location</param>
        /// <param name="fieldData">Data to be written</param>
        public void writeFieldData(int location, FieldData fieldData)
        {
            FileStream fieldDataStream = FieldDataStream; // Gets the needed filestream.
            int seekPosition = getFieldDataSeekPosition(location);   // Gets the seekposition for the specified database location.

            BinaryWriter bw = new BinaryWriter(fieldDataStream);
            uint[] storage = fieldData.getStorage();                    // Gets the storage of the specifield field data object to write it to the fieldDataStream.

            fieldDataStream.Seek(seekPosition, SeekOrigin.Begin);       // Sets the writing position to the wanted byte (uint in our case) in the database.
            for (byte i = 0; i < DbProperties.FieldWidth * 2; i++)      // We write each uint of the storage to the database stream.
            {
                bw.Write(storage[i]);
            }
        }

        public void appendItem(Field field, FieldData fieldData)
        {
            byte[] fComp = field.compressField();
            FieldsBuffer.Write(fComp, 0, FieldLength);

            uint[] fdStorage = fieldData.getStorage();
            foreach (uint u in fdStorage)
                FieldDataBuffer.Write(u);

            if (FieldsBuffer.Position >= getFieldsSeekPosition(MaxBufferSize))
                writeBuffer();
        }

        public void writeBuffer()
        {
            byte[] fieldArray = new byte[FieldsBuffer.Length];
            FieldsBuffer.Seek(0, SeekOrigin.Begin);
            FieldsBuffer.Read(fieldArray, 0, fieldArray.Length);

            FieldStream.Seek(0, SeekOrigin.End);
            FieldStream.Write(fieldArray, 0, fieldArray.Length);

            if (KeepInMemory)
            {
                int arrayPos = Fields.Length;
                Array.Resize(ref Fields, Fields.Length + fieldArray.Length);
                Buffer.BlockCopy(fieldArray, 0, Fields, arrayPos, fieldArray.Length);
            }

            FieldCount += fieldArray.Length / FieldLength;

            byte[] fdArray = new byte[FieldDataBuffer.BaseStream.Length];
            FieldDataBuffer.BaseStream.Seek(0, SeekOrigin.Begin);
            FieldDataBuffer.BaseStream.Read(fdArray, 0, fdArray.Length);

            FieldDataStream.Seek(0, SeekOrigin.End);
            FieldDataStream.Write(fdArray, 0, fdArray.Length);

            FieldsBuffer.Dispose();
            FieldsBuffer = new MemoryStream(getFieldsSeekPosition(MaxBufferSize));

            FieldDataBuffer.Dispose();
            MemoryStream fdStream = new MemoryStream(getFieldDataSeekPosition(MaxBufferSize));
            FieldDataBuffer = new BinaryWriter(fdStream);
        }

        public void writeProperties()
        {
            using (FileStream propertiesStream = new FileStream(getPropertiesPath(), FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryWriter bw = new BinaryWriter(propertiesStream);
                bw.Write(FieldLength);
                bw.Write(FieldCount);
            }
        }

        public bool isReading()
        {
            return Reading;
        }

        public bool isProcessing()
        {
            return Processing;
        }

        public void setReading(bool status)
        {
            Reading = status;
        }

        public void setProcessing(bool status)
        {
            Processing = status;
        }

        public void Dispose()
        {
            writeProperties();
            writeBuffer();
            FieldStream.Dispose();
            FieldDataStream.Dispose();
        }
    }
}
