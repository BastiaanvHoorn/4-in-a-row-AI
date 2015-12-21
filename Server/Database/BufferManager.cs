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
        private Timer Timer;
        private DateTime LastRequest;

        private const int IdleTimeout = 120000; // 2 minutes
        private const int UpdateInterval = 30000;
        private const int MaxBufferCount = 20;

        private bool Processing;

        public BufferManager(Database db)
        {
            Db = db;
            BufferPath = db.DbProperties.Path + db.DbProperties.PathSeparator + "Buffer";
            LastRequest = DateTime.Now;
            Timer = new Timer(UpdateInterval);
            Timer.Start();
            Timer.Elapsed += TimerElapsed;

            Processing = false;

            if (!Directory.Exists(BufferPath))
                Directory.CreateDirectory(BufferPath);
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

        public void addToBuffer(byte[] gameHistory)
        {
            string fileName = DateTime.Now.ToFileTime().ToString();
            string filePath = BufferPath + Db.DbProperties.PathSeparator + fileName;

            using (FileStream fs = File.Create(filePath))
                fs.Write(gameHistory, 0, gameHistory.Length);

            int bufferCount = Directory.GetFiles(BufferPath).Length;

            if (bufferCount >= MaxBufferCount && !Processing)
                processBuffers();
        }

        public bool processBuffers()
        {
            if (Processing)
                return false;

            Processing = true;
            
            string[] queueMembers = Directory.GetFiles(BufferPath);

            logger.Info($"Processing {queueMembers.Length} buffer items...");

            foreach (string member in queueMembers)
            {
                using (FileStream fs = new FileStream(member, FileMode.Open, FileAccess.Read))
                {
                    byte[] content = new byte[fs.Length];
                    fs.Read(content, 0, (int)fs.Length);
                    RequestHandler.process_game_history(content, Db);
                }

                File.Delete(member);
            }

            Processing = false;

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
