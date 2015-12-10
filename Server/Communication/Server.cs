using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Engine;
using Networker;
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
        static Random r = new Random();
        public log_modes log_mode;
        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public void StartListening(log_modes log_mode)
        {
            this.log_mode = log_mode;
            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

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
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
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

        public void AcceptCallback(IAsyncResult ar)
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
                new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
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
                        new AsyncCallback(ReadCallback), state);

                }
                else
                {
                    process_data(handler, state.data);
                }
            }
        }

        private void process_data(Socket handler, List<byte> data)
        {
            if (data[0] != (byte)network_codes.column_request &&
                data[0] != (byte)network_codes.game_history_array)
            {

                if (log_mode >= log_modes.only_errors)
                    Console.WriteLine("WARNING: Found no header in data array, will not process data");
                data = null;
                Send(handler, new[] { (byte)0 });
            }
            else
            {
                // Echo the data back to the client.
                // If the array is marked as a column_request, respond with a column
                if (data[0] == (byte)network_codes.column_request)
                {
                    byte[] _field = data.Skip(1).TakeWhile(b => b != (byte)network_codes.end_of_stream).ToArray();
                    Field field = new Field(_field);
                    byte[] send_data = new[] { RequestHandler.get_column(field) };
                    Send(handler, send_data);
                }
                //If the array is marked as a game-history-array, process the array.
                else if (data[0] == (byte)network_codes.game_history_array)
                {
                    if (log_mode >= log_modes.essential)
                        Console.WriteLine("Recieved game_history");
                    Send(handler, new[] { (byte)0 });
                    byte[][] game_history = linear_to_parrallel_game_history(data);
                    RequestHandler.receive_game_history(game_history);
                }

                //Clear the data array
                data = null;
            }
        }

        internal static byte[][] linear_to_parrallel_game_history(List<byte> list)
        {

            byte[] arr = list.TakeWhile(b => b != (byte)network_codes.end_of_stream).ToArray();
            //Count the amount of games that is in this byte-array
            
            int game_counter = arr.Count(b => b == (byte)network_codes.game_history_alice || b == (byte)network_codes.game_history_bob);
            //Create an array of arrays with the count of games
            byte[][] game_history = new byte[game_counter][];
            int game = -1;
            int turn = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                switch (arr[i])
                {
                    case (byte)network_codes.game_history_array:    //If the header is encountered,
                        continue;                                   //then continue immedeatly
                    case (byte)network_codes.end_of_stream:     //If the footer is encountered, then we finished looping through everything.
                        return game_history;                    //Now we can return the array
                    case (byte)network_codes.game_history_alice:    //If a player tag is encountered, thenwe finished looping through the current game.
                    case (byte)network_codes.game_history_bob:      //We must then create a new array for the next game.
                        game++;     //Increase the game-counter the array is indexed properly
                        turn = 0;   //Reset the turn count since this is a new game

                        for (int j = i + 1; j < arr.Length; j++)    //Start looping through arr with a temporary variable where we left
                        {
                            switch (arr[j])
                            {
                                case (byte)network_codes.end_of_stream:             //If the next player tag or footer is encountered.
                                case (byte)network_codes.game_history_alice:        //then we have counted all elements in this game and we can initialize a new game.
                                case (byte)network_codes.game_history_bob:
                                    game_history[game] = new byte[j - i];             // The found game is as long as the difference between the global counter and the temporary one
                                    goto new_game;                                  // Ext the loop after that (break will not work for the for loop since we're inside a switch)
                            }
                        }
                        new_game:
                        break;
                }
                game_history[game][turn] = arr[i];  //Add the current arr element to the new game-history at the right position
                turn++;
            }
            return game_history;
        }
        private void Send(Socket handler, byte[] data)
        {
            // Begin sending the data to the remote device.
            //Console.WriteLine($"Sent column {data[0]}");
            handler.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
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
        public static void Main(String[] args)
        {
            if (!System.IO.Directory.Exists(Properties.Settings.Default.DbPath))    // Checks if the database already exists
            {
                DatabaseProperties dbProps = new DatabaseProperties(Properties.Settings.Default.DbPath, 7, 6, Properties.Settings.Default.DbMaxFileSize);
                Database.prepareNew(dbProps);  // Creates a new database
            }
            AsynchronousSocketListener listener = new AsynchronousSocketListener();
            listener.StartListening(log_modes.essential);
        }
    }
}
