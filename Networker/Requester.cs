using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Networker
{
    public static class Requester
    {
        public static byte[] request(byte[] data)
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

                    Console.WriteLine($"Socket connected to {sender.RemoteEndPoint}");

                    // Encode the data string into a byte array.
                    byte[] msg = new byte[data.Length + 2];
                    msg[0] = 200;
                    msg[msg.Length - 1] = (byte)network_codes.end_of_stream;
                    data.CopyTo(msg, 1);

                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);
                    Console.WriteLine("Waiting for response from server");
                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine($"Recieved from server = {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");

                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                    return bytes;

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine($"ArgumentNullException : {ane}");
                }
                catch (SocketException se)
                {
                    Console.WriteLine($"SocketException : {se}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unexpected exception : {e}");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return new byte[0];
        }
    }
}
