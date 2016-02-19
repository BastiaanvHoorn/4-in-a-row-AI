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

        private const int IdleTimeout = 120000;     // 2 minutes
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
        /// Adds the given dictionary of fields to the database buffer for the specified fieldlength.
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

        /// <summary>
        /// Processes all stored buffers, by merging them into the existing database segments.
        /// </summary>
        private void processAllBuffers()
        {
            Processing = true;

            string[] dirPaths = Directory.GetDirectories(BufferPath);
            List<DatabaseSegment> bufferSegs = new List<DatabaseSegment>(dirPaths.Length);

            logger.Info($"Processing all buffers ({dirPaths.Length})");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Opens all buffer segments.
            foreach (string path in dirPaths)
            {
                DatabaseSegment dbSeg = new DatabaseSegment(path, Db.DbProperties, true);
                bufferSegs.Add(dbSeg);
            }

            // Merges all buffers with the right database segments. (Multi-threaded)
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

            // Adds new data to stats.
            Db.Stats.addCurrentMeasurement(sw.ElapsedTicks);
            logger.Info("New data logged to stats file");
            
            Processing = false;

            logger.Info($"Processing done in {sw.Elapsed.Minutes}m and {sw.Elapsed.Seconds}s");
        }
        
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
