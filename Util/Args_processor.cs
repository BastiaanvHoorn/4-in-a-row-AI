using System;
using System.Collections.Generic;
using NLog;
using Logger = NLog.Logger;

namespace Utility
{
    public static class Args_parser
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
        public static uint? parse_int_arg(List<string> args, string cmd_char, string arg_name, uint min, uint max, uint? default_value)
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
    }
}
