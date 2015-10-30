using System;
using System.Net;
using System.Net.Sockets;
using Engine;
using System.Text;

namespace Botclient
{
    public class Bot : IPlayer
    {
        public players player { get; }
        public log_modes log_mode { get; }
        public Bot(players player, log_modes log_mode)
        {
            this.player = player;
            this.log_mode = log_mode;
        }

        private byte get_column_from_server(Field field)
        {
            // Data buffer for incoming data.
            byte[] bytes = new byte[1024];

            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // This example uses port 11000 on the local computer.
                IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.
                Socket sender = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    sender.Connect(remoteEP);
                    if(log_mode >= log_modes.debug)
                        Console.WriteLine($"Socket connected to {sender.RemoteEndPoint.ToString()}");

                    // Encode the data string into a byte array.
                    byte[] _field = field.getStorage(); //Get the byte-array for the field
                    byte[] ending = Encoding.ASCII.GetBytes("<EOF>"); //Get the byte-array for the ending of the message
                    int length = _field.Length + ending.Length; //Get the total length of the message array
                    byte[] msg = new byte[length]; //Initialize the message array
                    Array.Copy(_field, msg, _field.Length);
                    Array.Copy(ending, 0, msg, _field.Length, ending.Length);



                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);
                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);
                    if(log_mode >= log_modes.debug)
                        Console.WriteLine($"Recieved from server = {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");

                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                    return byte.Parse(Encoding.ASCII.GetString(bytes, 0, bytesRec));

                }
                catch (ArgumentNullException ane)
                {
                    if(log_mode >= log_modes.only_errors)
                        Console.WriteLine($"ArgumentNullException : {ane}");
                }
                catch (SocketException se)
                {
                    if(log_mode >= log_modes.only_errors)
                        Console.WriteLine($"SocketException : {se}");
                }
                catch (Exception e)
                {
                    if (log_mode >= log_modes.only_errors)
                      Console.WriteLine($"Unexpected exception : {e}");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 1;
        }

        public byte get_turn(Field field)
        {
            byte column = get_column_from_server(field);
            if (log_mode >= log_modes.debug)
                Console.WriteLine($"Tried to drop a stone in colmun {column}");
            return column;
        }
    }
}
