using DigitalAssistant.HueConnector.ApiModels;


namespace DigitalAssistant.HueConnector.Services;

// Implementation of the RGB to XY conversion with help of the following resources:
// https://developers.meethue.com/develop/application-design-guidance/color-conversion-formulas-rgb-to-xy-and-back/
// https://github.com/johnciech/PhilipsHueSDK/blob/master/ApplicationDesignNotes/RGB%20to%20xy%20Color%20conversion.md
// https://github.com/michielpost/Q42.HueApi/blob/master/src/HueApi.ColorConverters/Original/HueColorConverter.cs
public class HueColorConverter
{
    public static XyPoint HexToXyColor(string color, Gamut gamut)
    {
        var normalizedColor = NormalizedColor.FromHex(color);
        double red = normalizedColor.R;
        double green = normalizedColor.G;
        double blue = normalizedColor.B;

        // Apply gamma correction
        double r = (red > 0.04045f) ? (float)Math.Pow((red + 0.055f) / (1.0f + 0.055f), 2.4f) : (red / 12.92f);
        double g = (green > 0.04045f) ? (float)Math.Pow((green + 0.055f) / (1.0f + 0.055f), 2.4f) : (green / 12.92f);
        double b = (blue > 0.04045f) ? (float)Math.Pow((blue + 0.055f) / (1.0f + 0.055f), 2.4f) : (blue / 12.92f);

        // Wide gamut conversion D65
        double X = r * 0.664511f + g * 0.154324f + b * 0.162028f;
        double Y = r * 0.283881f + g * 0.668433f + b * 0.047685f;
        double Z = r * 0.000088f + g * 0.072310f + b * 0.986039f;

        double cx = 0.0f;

        if ((X + Y + Z) != 0)
            cx = X / (X + Y + Z);

        double cy = 0.0f;
        if ((X + Y + Z) != 0)
            cy = Y / (X + Y + Z);

        //Check if the given XY value is within the colourreach of our lamps.

        XyPoint xyPoint = new XyPoint(cx, cy);
        bool inReachOfLamps = CheckPointInLampsReach(xyPoint, gamut);

        if (!inReachOfLamps)
        {
            //It seems the colour is out of reach
            //let's find the closest colour we can produce with our lamp and send this XY value out.

            //Find the closest point on each line in the triangle.
            var pAB = GetClosestPointToPoints(gamut.Red, gamut.Green, xyPoint);
            var pAC = GetClosestPointToPoints(gamut.Blue, gamut.Red, xyPoint);
            var pBC = GetClosestPointToPoints(gamut.Green, gamut.Blue, xyPoint);

            //Get the distances per point and see which point is closer to our Point.
            var dAB = GetDistanceBetweenTwoPoints(xyPoint, pAB);
            var dAC = GetDistanceBetweenTwoPoints(xyPoint, pAC);
            var dBC = GetDistanceBetweenTwoPoints(xyPoint, pBC);

            float lowest = dAB;
            XyPoint closestPoint = pAB;

            if (dAC < lowest)
            {
                lowest = dAC;
                closestPoint = pAC;
            }
            if (dBC < lowest)
            {
                lowest = dBC;
                closestPoint = pBC;
            }

            //Change the xy value to a value which is within the reach of the lamp.
            cx = (float)closestPoint.X;
            cy = (float)closestPoint.Y;
        }

        return new XyPoint(cx, cy);
    }

    public static string XyToHexColor(XyPoint xy, Gamut gamut)
    {
        bool inReachOfLamps = CheckPointInLampsReach(xy, gamut);

        if (!inReachOfLamps)
        {
            //It seems the colour is out of reach
            //let's find the closest colour we can produce with our lamp and send this XY value out.

            //Find the closest point on each line in the triangle.
            XyPoint pAB = GetClosestPointToPoints(gamut.Red, gamut.Green, xy);
            XyPoint pAC = GetClosestPointToPoints(gamut.Blue, gamut.Red, xy);
            XyPoint pBC = GetClosestPointToPoints(gamut.Green, gamut.Blue, xy);

            //Get the distances per point and see which point is closer to our Point.
            float dAB = GetDistanceBetweenTwoPoints(xy, pAB);
            float dAC = GetDistanceBetweenTwoPoints(xy, pAC);
            float dBC = GetDistanceBetweenTwoPoints(xy, pBC);

            float lowest = dAB;
            XyPoint closestPoint = pAB;

            if (dAC < lowest)
            {
                lowest = dAC;
                closestPoint = pAC;
            }
            if (dBC < lowest)
            {
                lowest = dBC;
                closestPoint = pBC;
            }

            //Change the xy value to a value which is within the reach of the lamp.
            xy.X = closestPoint.X;
            xy.Y = closestPoint.Y;
        }

        float x = (float)xy.X;
        float y = (float)xy.Y;
        float z = 1.0f - x - y;

        float Y = 1.0f;
        float X = (Y / y) * x;
        float Z = (Y / y) * z;

        // sRGB D65 conversion
        float r = X * 1.656492f - Y * 0.354851f - Z * 0.255038f;
        float g = -X * 0.707196f + Y * 1.655397f + Z * 0.036152f;
        float b = X * 0.051713f - Y * 0.121364f + Z * 1.011530f;

        if (r > b && r > g && r > 1.0f)
        {
            // red is too big
            g = g / r;
            b = b / r;
            r = 1.0f;
        }
        else if (g > b && g > r && g > 1.0f)
        {
            // green is too big
            r = r / g;
            b = b / g;
            g = 1.0f;
        }
        else if (b > r && b > g && b > 1.0f)
        {
            // blue is too big
            r = r / b;
            g = g / b;
            b = 1.0f;
        }

        // Apply gamma correction
        r = r <= 0.0031308f ? 12.92f * r : (1.0f + 0.055f) * (float)Math.Pow(r, 1.0f / 2.4f) - 0.055f;
        g = g <= 0.0031308f ? 12.92f * g : (1.0f + 0.055f) * (float)Math.Pow(g, 1.0f / 2.4f) - 0.055f;
        b = b <= 0.0031308f ? 12.92f * b : (1.0f + 0.055f) * (float)Math.Pow(b, 1.0f / 2.4f) - 0.055f;

        if (r > b && r > g)
        {
            // red is biggest
            if (r > 1.0f)
            {
                g = g / r;
                b = b / r;
                r = 1.0f;
            }
        }
        else if (g > b && g > r)
        {
            // green is biggest
            if (g > 1.0f)
            {
                r = r / g;
                b = b / g;
                g = 1.0f;
            }
        }
        else if (b > r && b > g)
        {
            // blue is biggest
            if (b > 1.0f)
            {
                r = r / b;
                g = g / b;
                b = 1.0f;
            }
        }

        return new NormalizedColor(r, g, b).ToHexColor();
    }

    private static XyPoint GetClosestPointToPoints(XyPoint a, XyPoint b, XyPoint p)
    {
        var ap = new XyPoint(p.X - a.X, p.Y - a.Y);
        var ab = new XyPoint(b.X - a.X, b.Y - a.Y);
        float ab2 = (float)(ab.X * ab.X + ab.Y * ab.Y);
        float ap_ab = (float)(ap.X * ab.X + ap.Y * ab.Y);

        float t = ap_ab / ab2;

        if (t < 0.0f)
            t = 0.0f;
        else if (t > 1.0f)
            t = 1.0f;

        return new XyPoint(a.X + ab.X * t, a.Y + ab.Y * t);
    }

    private static float GetDistanceBetweenTwoPoints(XyPoint one, XyPoint two)
    {
        float dx = (float)(one.X - two.X); // horizontal difference
        float dy = (float)(one.Y - two.Y); // vertical difference
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool CheckPointInLampsReach(XyPoint p, Gamut gamut)
    {
        var v1 = new XyPoint(gamut.Green.X - gamut.Red.X, gamut.Green.Y - gamut.Red.Y);
        var v2 = new XyPoint(gamut.Blue.X - gamut.Red.X, gamut.Blue.Y - gamut.Red.Y);

        var q = new XyPoint(p.X - gamut.Red.X, p.Y - gamut.Red.Y);

        float s = CrossProduct(q, v2) / CrossProduct(v1, v2);
        float t = CrossProduct(v1, q) / CrossProduct(v1, v2);

        if ((s >= 0.0f) && (t >= 0.0f) && (s + t <= 1.0f))
            return true;
        else
            return false;
    }

    private static float CrossProduct(XyPoint p1, XyPoint p2)
    {
        return (float)(p1.X * p2.Y - p1.Y * p2.X);
    }


    
}

