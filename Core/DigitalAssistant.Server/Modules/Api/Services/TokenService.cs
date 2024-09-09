using System.Security.Cryptography;
using System.Text;

namespace DigitalAssistant.Server.Modules.Api.Services;

public static class TokenService
{
    public static string GenerateRandomToken(int tokenLength)
    {
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@(){}[]*$-=/".ToCharArray();
        byte[] data = new byte[tokenLength];

        using var crypto = RandomNumberGenerator.Create();
        crypto.GetBytes(data);

        var result = new StringBuilder();
        foreach (byte b in data)
            result.Append(chars[b % chars.Length]);

        return result.ToString();
    }
}
