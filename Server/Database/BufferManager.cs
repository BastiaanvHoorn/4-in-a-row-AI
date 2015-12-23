using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Server
{
    public class BufferManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private Database Db;
        private string BufferPath;
        private string TempBufferPath;
        private Timer Timer;
        private DateTime LastRequest;

        private const int IdleTimeout = 120000; // 2 minutes
        private const int UpdateInterval = 30000;
        private const int MaxBufferCount = 8;

        private bool Processing;

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

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now > LastRequest.AddMilliseconds(IdleTimeout) && !Processing)
            {
                int fileCount = getBufferCount();

                if (fileCount > 0)
                {
                    logger.Info("Database in idle mode");

                    processBuffers();
                }
            }
        }

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

        public byte[] getBufferContent(string bufferPath)
        {
            using (FileStream fs = new FileStream(bufferPath, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, (int)fs.Length);
                return bytes;
            }
        }

        public bool processBuffers()
        {
            if (Processing)
                return false;

            Processing = true;
            
            string[] queueMembers = Directory.GetFiles(BufferPath);

            logger.Info($"Starting processing of {queueMembers.Length} buffer items...");

            foreach (string member in queueMembers)
            {
                RequestHandler.process_game_history(member, Db);

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
        }

        public int getBufferCount()
        {
            return Directory.GetFiles(BufferPath).Length;
        }

        public void justRequested()
        {
            LastRequest = DateTime.Now;
        }

        public bool isProcessing()
        {
            return Processing;
        }
    }
}
