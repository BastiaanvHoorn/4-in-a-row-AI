using System;

namespace GUI
{
    public class InvalidMoveException : ApplicationException
    {
        public InvalidMoveException(string message) : base(message)
        {

        }
    }
}
