using System;

namespace DevInstance.DevCoreApp.Shared.Utils;

/// <summary>
/// Transforms Guid into a case sensitive string representation
/// using letters and numbers
/// </summary>
public class IdGenerator
{
    private static char EncodeByte(byte val)
    {
        if (val >= 0 && val <= 9) //0-9
        {
            return (char)(val + 0x30);
        }
        else if (val > 9 && val <= 0x23) // A-Z
        {
            return (char)(val + (0x41 - 0x0a));
        }
        //a - z
        return (char)(val + (0x61 - 0x23));
    }

    public static string New()
    {
        return FromGuid(Guid.NewGuid());
    }

    public static string FromGuid(Guid guid)
    {
        string result = "";
        byte[] bts = guid.ToByteArray();
        foreach (byte bt in bts)
        {
            byte valLeft = (byte)((int)bt & 0x1f);
            byte valRight = (byte)(((int)bt & 0xf8) >> 4);
            result += EncodeByte(valLeft);
            result += EncodeByte(valRight);
        }
        return result.ToLower();
    }
}
