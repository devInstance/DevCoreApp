using System;

namespace DevInstance.SampleWebApp.Server.Exceptions
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
