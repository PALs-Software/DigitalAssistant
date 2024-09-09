using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace DigitalAssistant.Base.Extensions;

public static class StringExtension
{
    public static SecureString ToSecureString(this string input)
    {
        var secure = new SecureString();

        foreach (char c in input)
            secure.AppendChar(c);

        secure.MakeReadOnly();
        return secure;
    }

    public static string CreateSHA512Hash(this string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        using var hash = SHA512.Create();
        var hashedInputBytes = hash.ComputeHash(bytes);

        // Convert to text
        // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
        var hashedInputStringBuilder = new StringBuilder(128);
        foreach (var b in hashedInputBytes)
            hashedInputStringBuilder.Append(b.ToString("X2"));
        return hashedInputStringBuilder.ToString();
    }
}