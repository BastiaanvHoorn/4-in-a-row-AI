using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Logger = NLog.Logger;

namespace Utility
{
    public static class Util
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Initialize 1 option from a list of command-line arguments and return the value.
        /// </summary>
        /// <param name="args">List of all arguments and options</param>
        /// <param name="cmd_char">The character which indicates the option</param>
        /// <param name="arg_name">A readable name of the option which is printed on the screen</param>
        /// <param name="min">The minimum value of the argument</param>
        /// <param name="max">The maximum value of the argument</param>
        /// <param name="default_value">The default value of the argument. This value will be returned if the argument was invalid</param>
        /// <returns></returns>
        public static uint parse_int_arg(List<string> args, string cmd_char, string arg_name, uint min, uint max, uint default_value)
        {
            try
            {
                uint option = uint.Parse(parse_arg(args, cmd_char, arg_name, default_value.ToString())); //As the output of this relies on user input, this can give errors.
                if (option >= min && option <= max)
                {
                    return option;
                }
                logger.Info($"{arg_name} was given outside of the boundaries {min} and {max}");
            }
            catch (FormatException) //Formatexception for the uint.parse
            {
                logger.Info($"{arg_name} was given in the wrong format");
            }
            //Return the default value when we had an exception or the found parameter was outside of the given boundaries
            logger.Info($"{arg_name} defaulted to {default_value}");
            return default_value;
        }

        public static string parse_arg(List<string> args, string cmd_char, string arg_name, string default_value)
        {
            int index = args.IndexOf("-" + cmd_char);
            if (index == -1) return default_value;
            string option = args[index + 1];
            return option;
        }
        public static bool ping(IPAddress address, ushort port, out string s)
        {
            System.Net.NetworkInformation.Ping ping_sender = new System.Net.NetworkInformation.Ping();
            PingReply reply = ping_sender.Send(address);
            if (reply.Status == IPStatus.Success)
            {
                try
                {
                    byte[] data = Requester.send(new byte[0], Network_codes.ping, address, port);
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
