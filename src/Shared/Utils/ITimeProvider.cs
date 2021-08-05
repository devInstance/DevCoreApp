using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Shared.Utils
{
    /// <summary>
    /// Timer provider abstracts DateTime.Now and offset functions
    /// </summary>
    public interface ITimeProvider
    {
        DateTime CurrentTime { get; }

        int UtcTimeOffset { get; }
    }
}
