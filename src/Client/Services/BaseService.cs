using DevInstance.LogScope;
using System;

namespace DevInstance.DevCoreApp.Client.Services
{
    public delegate void ResultHandler<T>(T result);

    public class BaseService
    {
        public IScopeLog Log { get; protected set; }

    }
}
