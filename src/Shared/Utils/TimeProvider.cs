using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Shared.Utils
{
    public class TimeProvider : ITimeProvider
    {
        public DateTime CurrentTime
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public int UtcTimeOffset => (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
    }
}
