using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Shared.Utils;

public static class DateTimeExtensions
{
    public static DateTime UTCKind(this DateTime dt)
    {
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }
}
