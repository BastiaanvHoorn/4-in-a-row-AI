using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using Engine;
using NLog;
using Logger = NLog.Logger;
using Util;

namespace Server
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 64;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public List<byte> data = new List<byte>();
    }

    public class AsynchronousSocketListener
    {

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Database db;
        public int port;
        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener(Database db, int port)
        {
            this.db = db;
            this.port = port;
        }
        public void start_listening()
        {
            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            logger.Info($"Starting server at port {port}");
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    logger.Debug("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(accept_callback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }
        private void accept_callback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(read_callback), state);
        }
        private void read_callback(IAsyncResult ar)
        {

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // Read the next bit of data to the data_buffer
                state.data.AddRange(state.buffer);

                // Check for end-of-file tag. If it is not there, read more data.
                if (!state.data.Contains((byte)network_codes.end_of_stream))
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(read_callback), state);

                }
                else
                {
                    process_callback(handler, state.data);
                }
            }
        }
        private void process_callback(Socket handler, List<byte> data)
        {
            if (data[0] != (byte)network_codes.column_request &&
                data[0] != (byte)network_codes.game_history_array &&
                data[0] != (byte)network_codes.range_request &&
                data[0] != (byte)network_codes.details_request)
            {

                logger.Warn("Found no header in data array, will not process data");
                logger.Trace(data);
                data = null;
                send(handler, new[] { (byte)0 });
                return;
            }

            db.BufferMgr.justRequested();

            // Echo the data back to the client.
            // If the array is marked as a column_request, respond with a column
            if (data[0] == (byte)network_codes.column_request)
            {
                byte[] _field = data.Skip(1).TakeWhile(b => b != (byte)network_codes.end_of_stream).ToArray();
                Field field = new Field(_field);
                byte[] send_data = new[] { RequestHandler.get_column(field, db) };
                send(handler, send_data);
            }
            //If the array is marked as a game-history-array, process the array.
            else if (data[0] == (byte)network_codes.game_history_array)
            {
                send(handler, new[] { (byte)0 });
                RequestHandler.receive_game_history(data.ToArray(), db);
            }

            else if (data[0] == (byte) network_codes.range_request)
            {
                byte[] _data = data.ToArray();
                int file = BitConverter.ToInt32(_data, 1);  //1,2,3,4
                int begin = BitConverter.ToInt32(_data, 5); //5,6,7,8
                int end = BitConverter.ToInt32(_data, 9);   //9,10,11,12
                send(handler, db.getFieldFileContent(file, begin, end));
            }
            else if (data[0] == (byte) network_codes.details_request)
            {
                throw new NotImplementedException();
            }
        }
        private void send(Socket handler, byte[] data)
        {
            // Begin sending the data to the remote device.
            //Console.WriteLine($"Sent column {data[0]}");
            byte[] _data = new byte[data.Length+1];
            data.CopyTo(_data, 0);
            _data[_data.Length - 1] = (byte) network_codes.end_of_stream;
            handler.BeginSend(_data, 0, _data.Length, 0,
                new AsyncCallback(send_callback), handler);
        }
        private void send_callback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                int bytesSent = handler.EndSend(ar);
                // Complete sending the data to the remote device.
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public class server
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void Main(String[] args)
        {
            Console.Clear();

            string dbDir = string.Empty;

            if (args.Length > 0)
                dbDir = args[0];
            else
                dbDir = Properties.Settings.Default.DbPath;

            if (!System.IO.Directory.Exists(dbDir))    // Checks if the database already exists
            {
                logger.Info($"No database found in {dbDir}!");
                Console.Write("Do you want to create a new database in that folder? [Y/N] ");
                if (Console.ReadKey().KeyChar == 'y')
                {
                    Console.WriteLine();
                    DatabaseProperties dbProps = new DatabaseProperties(dbDir, 7, 6, Properties.Settings.Default.DbMaxFileSize);
                    Database.prepareNew(dbProps);
                }
                else
                {
                    Console.WriteLine();
                    Environment.Exit(0);
                }

                logger.Info("Succesfully created a new database");
            }

            logger.Info($"Initializing database at => {dbDir}");

            using (Database db = new Database(dbDir))
            {
                AsynchronousSocketListener listener = new AsynchronousSocketListener(db, 11000);
                listener.start_listening();
            }
        }
    }
}

