using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NLog;
using Logger = NLog.Logger;

namespace Util
{
    public static class Requester
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static byte[] send(byte[] data, network_codes type)
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

                    logger.Debug($"Socket connected to {sender.RemoteEndPoint}");

                    byte[] msg = add_header_footer(data, type);
                    // Send the data through the socket.
                    int bytesSent = sender.Send(msg);
                    logger.Debug("Waiting for response from server");
                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);
                    logger.Debug($"Recieved from server = {Encoding.ASCII.GetString(bytes, 0, bytesRec)}");

                    // Release the socket.
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                    return bytes;

                }
                catch (ArgumentNullException ane)
                {
                    logger.Error($"ArgumentNullException : {ane}");
                }
                catch (SocketException se)
                {
                    logger.Error($"SocketException : {se}");
                }
                catch (Exception e)
                {
                    logger.Error($"Unexpected exception : {e}");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return new byte[0];
        }

        internal static byte[] add_header_footer(byte[] data, network_codes type)
        {
            //Create a byte-array that is 2 larger then the data that must be send so there is room for a header and a footer
            byte[] msg = new byte[data.Length + 2];
            //Add a header based on the given signal-type
            msg[0] = (byte)type;

            //Add a footer witn an end-of-stream token
            msg[msg.Length - 1] = (byte)network_codes.end_of_stream;
            
            //Copy all the data to the middle part of the array
            data.CopyTo(msg, 1);
            return msg;
        }
    }
}
