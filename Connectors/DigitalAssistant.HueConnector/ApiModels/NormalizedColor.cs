using System.Globalization;

namespace DigitalAssistant.HueConnector.ApiModels;

/// <summary>
/// Represents a color with red, green and blue components.
/// All values are between 0.0 and 1.0.
/// </summary>
public struct NormalizedColor
{
    public double R;
    public double G;
    public double B;

    /// <summary>
    /// RGB Color
    /// </summary>
    /// <param name="red">Between 0.0 and 1.0</param>
    /// <param name="green">Between 0.0 and 1.0</param>
    /// <param name="blue">Between 0.0 and 1.0</param>
    public NormalizedColor(double red, double green, double blue)
    {
        if (red < 0)
            red = 0;
        else if (red > 1)
            red = 1;

        if (green < 0)
            green = 0;
        else if (green > 1)
            green = 1;

        if (blue < 0)
            blue = 0;
        else if (blue > 1)
            blue = 1;

        R = red;
        G = green;
        B = blue;
    }

    public NormalizedColor(int red, int green, int blue)
    {
        red = red > 255 ? 255 : red;
        green = green > 255 ? 255 : green;
        blue = blue > 255 ? 255 : blue;

        red = red < 0 ? 0 : red;
        green = green < 0 ? 0 : green;
        blue = blue < 0 ? 0 : blue;

        R = red / 255.0;
        G = green / 255.0;
        B = blue / 255.0;
    }

    public static NormalizedColor FromHex(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6)
            throw new ArgumentException("Hex color must be 6 characters long");

        var r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        var g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        var b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

        return new NormalizedColor(r, g, b);
    }

    public string ToHexColor()
    {
        return $"#{(int)(R * 255):X2}{(int)(G * 255):X2}{(int)(B * 255):X2}";
    }
}
