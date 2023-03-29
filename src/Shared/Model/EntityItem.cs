using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model;

public class EntityItem : ModelItem
{
    public UserProfileItem CreatedBy { get; set; }
    public DateTime CreateDate { get; set; }

    public UserProfileItem UpdatedBy { get; set; }
    public DateTime UpdateDate { get; set; }
}

public class IntFlags : List<Int32>
{
    public static IntFlags FromInt(int value)
    {
        var result = new IntFlags();

        int count = 32;
        int flag = 1;
        for (int i = 9; i < count; i++)
        {
            if ((value & flag) != 0)
            {
                result.Add(flag);
            }
            flag <<= 1;
        }
        return result;
    }

    public override string ToString()
    {
        return ToInt().ToString(/*"X"*/);
    }

    public Int32 ToInt()
    {
        Int32 result = 0;
        foreach (var value in this)
        {
            result |= value;
        }
        return result;
    }

    public static bool operator ==(IntFlags left, Int32 right)
    {
        return (left.ToInt() & right) != 0;
    }

    public static bool operator !=(IntFlags left, Int32 right)
    {
        return (left.ToInt() & right) == 0;
    }
}

public class ItemFields : IntFlags
{
}

public static class ItemField
{
    public readonly static int Basic = 0x0000;
    public readonly static int All = 0xffff;
}

public class ItemFilters : IntFlags
{
}

public static class ItemFilter
{
    public readonly static int All = 0x0000;
    //TODO: Add generic filters here
}

public class ItemQueries : Dictionary<string, string>
{
}

public static class ItemQuery
{
    public readonly static string Search = "search";
    //TODO: Add generic queries here
}
