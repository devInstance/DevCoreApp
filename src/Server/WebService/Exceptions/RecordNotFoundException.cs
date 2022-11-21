using System;

namespace DevInstance.DevCoreApp.Server.Exceptions
{
    public class RecordNotFoundException : Exception
    {
        public RecordNotFoundException()
        {
        }

        public RecordNotFoundException(string message) : base($"Record not found: {message}")
        {
        }
    }
}
