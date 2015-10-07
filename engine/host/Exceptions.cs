using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4_in_a_row_
{
    public class InvalidMoveException : ApplicationException
    {
        public InvalidMoveException(string message) : base(message)
        {

        }
    }
}
