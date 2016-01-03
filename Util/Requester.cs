using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using NLog;
using Logger = NLog.Logger;

namespace Utility
{
    public static class Requester
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Send a byte array to the specified address at the specified port
        /// </summary>
        /// <param name="data">The data that will be sent</param>
        /// <param name="type">The type of header that needs te be added to the data</param>
        /// <param name="ip_address">The address to which it will be sent</param>
        /// <param name="port">The port at which it will be sent</param>
        /// <param name="catch_socket_exc">If socket exceptions need to be caught, give false if they will be handled elsewhere</param>
        /// <returns></returns>
        public static byte[] send(byte[] data, byte type, IPAddress ip_address, ushort port, bool catch_socket_exc = true)
        {
            // Data buffer for incoming data.
            byte[] bytes = new byte[1024];

            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // This example uses port 11000 on the local computer.
                //IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                //ip_address = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ip_address, port);

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
                    bytes = bytes.TakeWhile(b => b != Network_codes.end_of_stream).ToArray();
                    return bytes;

                }
                catch (ArgumentNullException ane)
                {
                    logger.Error($"ArgumentNullException : {ane}");
                }
                catch (SocketException se)
                {
                    if (!catch_socket_exc)
                    {
                        throw se;
                    }
                    else
                    {
                        logger.Error($"SocketException : {se}");
                    }
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

        internal static byte[] add_header_footer(byte[] data, byte type)
        {
            //Create a byte-array that is 2 larger then the data that must be send so there is room for a header and a footer
            byte[] msg = new byte[data.Length + 2];
            //Add a header based on the given signal-type
            msg[0] = type;

            //Add a footer witn an end-of-stream token
            msg[msg.Length - 1] = Network_codes.end_of_stream;

            //Copy all the data to the middle part of the array
            data.CopyTo(msg, 1);
            return msg;
        }

        public static bool ping(IPAddress address, ushort port, out string s)
        {
            Ping ping_sender = new Ping();
            PingReply reply = ping_sender.Send(address);
            if (reply.Status == IPStatus.Success)
            {
                try
                {
                    byte[] data = send(new byte[0], Network_codes.ping, address, port, false);
                    if (data.Length == 0)
                    {
                        s = $"The address that was specified is valid but there is no server listening to this port";
                        logger.Info(s);
                        return false;
                    }
                    byte b = data[0];
                    if (b == Network_codes.ping_respond)
                    {
                        s = $"Server is online. Ping took {reply.RoundtripTime} ms";
                        logger.Info(s);
                        return true;
                    }

                    s = $"Something responded the ping but not with the correct code. Ping took Ping took {reply.RoundtripTime} ms";
                    logger.Info(s);
                    return false;

                }
                catch (SocketException)
                {
                    s = $"The address that was specified is valid but the request was rejected at the specified port";
                    logger.Info(s);
                    return false;
                }
            }
            s = $"Something went wrong. Are you connected to the internet and is the IP correct?";
            logger.Info(s);
            return false;

        }
    }
}
