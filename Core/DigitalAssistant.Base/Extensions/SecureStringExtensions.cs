using System.Runtime.InteropServices;
using System.Security;

namespace DigitalAssistant.Base.Extensions;

public static class SecureStringExtension
{
    public static string? ToInsecureString(this SecureString input)
    {
        IntPtr valuePtr = IntPtr.Zero;
        try
        {
            valuePtr = SecureStringMarshal.SecureStringToGlobalAllocUnicode(input);
            return Marshal.PtrToStringUni(valuePtr);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }
}