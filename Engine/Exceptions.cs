using System;

namespace Engine
{
    public class InvalidMoveException : ApplicationException
    {
        public InvalidMoveException(string message) : base(message) { }
    }
}
