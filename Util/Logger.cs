using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class Logger
    {
        public log_modes log_mode { get; }
        public Logger(log_modes log_mode)
        {
            this.log_mode = log_mode;
        }

        public void log(string s, log_modes log_mode, log_types log_type = log_types.message)
        {
            if (log_mode > this.log_mode) return;
            string message;
            switch (log_type)
            {
                case log_types.error:
                    message = $"[ERROR,   ";
                    break;
                case log_types.warning:
                    message = $"[WARNING, ";
                    break;
                case log_types.message:
                default:
                    message = $"[MESSAGE, ";
                    break;
            }
            message += $"{DateTime.Now.ToString("hh:mm:ss")}]: {s}";
            Console.WriteLine(message);
        }
    }
}
