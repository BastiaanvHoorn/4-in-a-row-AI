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
        public ushort port;
        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener(Database db, ushort port)
        {
            this.db = db;
            this.port = port;
        }
        public void start_listening()
        {
            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            IPHostEntry ip_host_info = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip_address = ip_host_info.AddressList[1];
            IPEndPoint local_end_point = new IPEndPoint(ip_address, port);
            logger.Info($"Starting server at port {ip_address}:{port}");
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(local_end_point);
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
                if (!state.data.Contains(Network_codes.end_of_stream))
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
            db.BufferMgr.justRequested();
            // Echo the data back to the client.
            // If the array is marked as a column_request, respond with a column
            if (data[0] ==  Network_codes.ping)
            {
                send(handler, new byte[] {0});
            }
            else if (data[0] == Network_codes.column_request)
            {
                byte[] _field = data.Skip(1).TakeWhile(b => b != Network_codes.end_of_stream).ToArray();
                Field field = new Field(_field);
                byte[] send_data = new[] { db.get_column(field) };
                send(handler, send_data);
            }
            //If the array is marked as a game-history-array, reply with nothing and process the array.
            else if (data[0] == Network_codes.game_history_array)
            {
                send(handler, new[] { (byte)0 });
                db.receive_game_history(data.ToArray());
            }
            //If the array is marked as a request for a range of games, get the range of games and return them
            else if (data[0] == Network_codes.range_request)
            {
                byte[] _data = data.ToArray();              //Convert the list to an array so it can be passed to the Bitconverter class (which doesnt eat lists)
                int file = BitConverter.ToInt32(_data, 1);  //bytes 1, 2, 3 and 4 are the file indication
                int begin = BitConverter.ToInt32(_data, 5); //bytes 5, 6, 7 and 8 are the indication for the beginning of the range
                int end = BitConverter.ToInt32(_data, 9);   //bytes 9, 10, 11 and 12 are the indication for the end of the range
                send(handler, db.getFieldFileContent(file, begin, end));
            }
            //If the array is marked as a request for details about a game, get the details and return them
            else if (data[0] == Network_codes.details_request)
            {
                Field field = new Field(data.Skip(1).TakeWhile(b=> b != Network_codes.end_of_stream).ToArray());
                FieldData field_data = db.readFieldData(field);
                byte[] send_data = new byte[2*7*4]; //2 arrays of 7 32-bit(=4 bytes) integers
                for (int i = 0; i < 7; i++)
                {
                    BitConverter.GetBytes(field_data.TotalCounts[i]).CopyTo(send_data, i*4);
                }
                for (int i = 0; i < 7; i++)
                {
                    BitConverter.GetBytes(field_data.WinningCounts[i]).CopyTo(send_data, 28 + i*4);
                }
                send(handler, send_data);
            }
            else
            {
                logger.Warn("Found no header in data array, will not process data");
                logger.Trace(data);
                data = null;
                send(handler, new[] { (byte)0 });
            }
        }
        private void send(Socket handler, byte[] data)
        {
            // Begin sending the data to the remote device.
            //Console.WriteLine($"Sent column {data[0]}");
            byte[] _data = new byte[data.Length + 1];
            data.CopyTo(_data, 0);
            _data[_data.Length - 1] = Network_codes.end_of_stream;
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

    /// <summary>
    /// Initializes the simulator
    /// </summary>
    /// <param name="_args">The command-line arguments
    /// -db [path]      The path of the database
    /// -p  [ushort >0] The port the server should be listening to
    /// </param>
    public class server
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void Main(String[] _args)
        {
            List<string> args = _args.ToList();
            Console.Clear();

            string dbDir = Args_processor.parse_arg(args, "db", "Database path", Properties.Settings.Default.DbPath);
            ushort port = (ushort) Args_processor.parse_int_arg(args, "p", "port", 0, ushort.MaxValue, 11000);
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
                AsynchronousSocketListener listener = new AsynchronousSocketListener(db, port);
                listener.start_listening();
            }
        }
    }
}

