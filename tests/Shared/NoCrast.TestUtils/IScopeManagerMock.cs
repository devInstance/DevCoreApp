using DevInstance.LogScope;
using System;

namespace DevInstance.DevCoreApp.Shared.TestUtils
{
    public class IScopeManagerMock : IScopeManager
    {
        public ILogProvider Provider => null;

        public LogLevel BaseLevel => LogLevel.DEBUG;

        public IScopeLog CreateLogger(string scope)
        {
            return new IScopeLogMock();
        }

        public IScopeLog CreateLogger(string scope, LogLevel levelOverride)
        {
            return new IScopeLogMock();
        }
    }

    public class IScopeLogMock : IScopeLog
    {
        public string Name { get { return ""; } }

        public LogLevel Level => throw new NotImplementedException();

        public void Dispose()
        {
        }

        public void Line(LogLevel level, string message)
        {
        }

        public void Line(string message)
        {
        }

        public IScopeLog Scope(LogLevel level, string scope)
        {
            return new IScopeLogMock();
        }

        public IScopeLog Scope(string scope)
        {
            return new IScopeLogMock();
        }
    }
}
