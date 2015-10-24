using System;

namespace Engine
{
    public class InvalidMoveException : ApplicationException
    {
        public InvalidMoveException(string message) : base(message) { }
    }

    public class DatabaseException : ApplicationException
    {
        public DatabaseException(string message) : base(message) { }
    }
}
