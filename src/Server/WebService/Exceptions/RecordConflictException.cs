using System;

namespace DevInstance.DevCoreApp.Server.Exceptions
{
    public class RecordConflictException : Exception
    {
        public RecordConflictException()
        {
        }

        public RecordConflictException(string message) : base($"Record not found: {message}")
        {
        }
    }
}
