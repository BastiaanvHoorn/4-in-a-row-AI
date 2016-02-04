using Engine;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Server
{
    /// <summary>
    /// The buffermanager is part of the Database. It's used to manage incoming game histories and the processing of these game histories.
    /// </summary>
    public class BufferManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private Database Db;
        private string BufferPath;      // Folder to store buffer items
        private string TempBufferPath;  // Folder to store buffer items that are received while the database is processing.
        private Timer Timer;            // Timer to check if the database is in idle mode (and is able to start processing spare buffers if present).
        private DateTime LastRequest;   // Holds the last time at which the server (database) received a request.

        private const int IdleTimeout = 30000;     // 2 minutes
        private const int UpdateInterval = 30000;   // The interval to check for database idle mode.
        private const int MaxBufferCount = 220;     // Maximum amount of buffer directories in the Buffer directory. (If amount passes this constant the DatabaseManager will force the database to process the buffers)
        
        private bool Processing;        // Indicates whether the database is processing.

        /// <summary>
        /// Creates a new BufferManager.
        /// </summary>
        /// <param name="db"></param>
        public BufferManager(Database db)
        {
            Db = db;
            BufferPath = db.DbProperties.Path + db.DbProperties.PathSeparator + "Buffer";
            TempBufferPath = BufferPath + "Temp";
            LastRequest = DateTime.Now;
            Timer = new Timer(UpdateInterval);
            Timer.Start();
            Timer.Elapsed += TimerElapsed;

            Processing = false;

            if (!Directory.Exists(BufferPath))
                Directory.CreateDirectory(BufferPath);

            if (!Directory.Exists(TempBufferPath))
                Directory.CreateDirectory(TempBufferPath);
        }

        /// <summary>
        /// When the timer elapses the DatabaseManager checks if the database in idle mode. (If so, the database will process the spare buffers)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now > LastRequest.AddMilliseconds(IdleTimeout) && !Processing)
            {
                int fileCount = getBufferCount();

                if (fileCount > 0)
                {
                    logger.Info("Database in idle mode");

                    processAllBuffers();
                }
            }
        }

        /// <summary>
        /// Adds the given dictionary of fields to the buffer of database for the specified fieldlength.
        /// </summary>
        /// <param name="fieldLength"></param>
        /// <param name="bufferContent">Dictionary of fields to add</param>
        public string addBuffer(int fieldLength, Dictionary<Field, FieldData> bufferContent)
        {
            string bufferPath = Processing ? TempBufferPath : BufferPath;
            string bufferName = DateTime.Now.ToFileTime().ToString() + fieldLength.ToString();
            string bufferDir = bufferPath + Db.DbProperties.PathSeparator + bufferName;
            
            DatabaseSegment dbSeg = new DatabaseSegment(bufferDir, Db.DbProperties, fieldLength);
            DatabaseSegment.prepareNew(dbSeg, bufferContent);
            dbSeg.Dispose();

            if (getBufferCount() >= MaxBufferCount && !Processing)
                processAllBuffers();
            
            return bufferName;
        }

        private void processAllBuffers()
        {
            Processing = true;

            string[] dirPaths = Directory.GetDirectories(BufferPath);
            List<DatabaseSegment> bufferSegs = new List<DatabaseSegment>(dirPaths.Length);

            logger.Info($"Processing all buffers ({dirPaths.Length})");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (string path in dirPaths)
            {
                DatabaseSegment dbSeg = new DatabaseSegment(path, Db.DbProperties, true);
                bufferSegs.Add(dbSeg);
            }

            Parallel.For(1, Db.DbProperties.MaxFieldStorageSize + 1, new ParallelOptions() { MaxDegreeOfParallelism = 3 }, i =>
            {
                DatabaseSegment[] toMerge = bufferSegs.Where(s => s.FieldLength == i).ToArray();

                Db.mergeWithBuffers(toMerge);

                foreach (DatabaseSegment dbSeg in toMerge)
                {
                    string segPath = dbSeg.Path;
                    dbSeg.Dispose();
                    Directory.Delete(segPath, true);
                }
            });

            if (Directory.GetDirectories(TempBufferPath).Length > 0)
            {
                Directory.Delete(BufferPath);
                Directory.Move(TempBufferPath, BufferPath);
                Directory.CreateDirectory(TempBufferPath);
            }

            sw.Stop();

            logger.Info($"Processing done in {sw.Elapsed.Minutes}m and {sw.Elapsed.Seconds}s");

            logger.Info("Logging data to stats file");
            Db.Stats.addCurrentMeasurement(sw.ElapsedTicks);

            Processing = false;
        }

        /*private void processBuffer(string bufferName)
        {
            string bufferDir = BufferPath + Db.DbProperties.PathSeparator + bufferName;

            
        }*/
        /*/// <summary>
        /// Adds a game history (packet) to the buffer folder, and starts processing the buffers if MaxBufferCount is passed.
        /// </summary>
        /// <param name="gameHistory">The gamehistory to store in the buffer</param>
        /// <returns>The filepath of the new buffer</returns>
        public string addToBuffer(byte[] gameHistory)
        {
            string bufferPath = Processing ? TempBufferPath : BufferPath;

            string fileName = DateTime.Now.ToFileTime().ToString();
            string filePath = bufferPath + Db.DbProperties.PathSeparator + fileName;

            File.WriteAllBytes(filePath, gameHistory);

            if (!Processing)
            {
                logger.Info($"Added game history to buffer ({getBufferCount()} items)");
                int bufferCount = Directory.GetFiles(BufferPath).Length;

                if (bufferCount >= MaxBufferCount)
                    processBuffers();
            }
            else
            {
                logger.Info("New game history added to temporary buffer (Database is processing)");
            }

            return filePath;
        }

        /// <summary>
        /// Returns the content of the buffer (The game history stored in the buffer file).
        /// </summary>
        /// <param name="bufferPath"></param>
        /// <returns>A game history array</returns>
        public byte[] getBufferContent(string bufferPath)
        {
            using (FileStream fs = new FileStream(bufferPath, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, (int)fs.Length);
                return bytes;
            }
        }

        /// <summary>
        /// Commands the database to process all buffers in the Buffer folder.
        /// </summary>
        /// <returns>A bool indicating whether the task succeeded</returns>
        public bool processBuffers()
        {
            if (Processing)
                return false;

            Processing = true;
            
            string[] queueMembers = Directory.GetFiles(BufferPath);

            logger.Info($"Starting processing of {queueMembers.Length} buffer items...");

            foreach (string member in queueMembers)
            {
                Db.process_game_history(member);

                File.Delete(member);
            }

            Processing = false;

            logger.Info($"Buffer is empty");

            string[] tempBuffers = Directory.GetFiles(TempBufferPath);

            if (tempBuffers.Length > 0)
            {
                logger.Info($"Moving {tempBuffers.Length} buffers from temporary folder to buffer folder");

                foreach (string file in tempBuffers)
                {
                    string newFileName = Path.GetFileName(file);
                    string newPath = BufferPath + Db.DbProperties.PathSeparator + newFileName;
                    File.Move(file, newPath);
                }
            }

            return true;
        }*/

        /// <summary>
        /// Returns the amount of buffers stored in the Buffer folder
        /// </summary>
        /// <returns></returns>
        public int getBufferCount()
        {
            return Directory.GetDirectories(BufferPath).Length;
        }

        /// <summary>
        /// This void is used to set the LastRequest time to DateTime.Now, so it can be used by the Timer to determine if the Database is in idle mode.
        /// </summary>
        public void justRequested()
        {
            LastRequest = DateTime.Now;
        }

        /// <summary>
        /// Returns whether the database is processing buffers (game histories).
        /// </summary>
        /// <returns></returns>
        public bool isProcessing()
        {
            return Processing;
        }
    }
}
