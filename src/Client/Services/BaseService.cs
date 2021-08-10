using DevInstance.LogScope;
using System;

namespace DevInstance.SampleWebApp.Client.Services
{
    public delegate void ResultHandler<T>(T result);

    public class BaseService
    {
        public IScopeLog Log { get; protected set; }

    }
}
