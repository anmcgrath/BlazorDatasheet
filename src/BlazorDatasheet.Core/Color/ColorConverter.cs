using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.Core.Color;

public class ColorConverter
{
    public static System.Drawing.Color HSVToRGB(double H, double S, double V)
    {
        double r = 0, g = 0, b = 0;

        if (S == 0)
        {
            r = V;
            g = V;
            b = V;
        }
        else
        {
            int i;
            double f, p, q, t;

            if (H == 360)
                H = 0;
            else
                H = H / 60;

            i = (int)Math.Truncate(H);
            f = H - i;

            p = V * (1.0 - S);
            q = V * (1.0 - (S * f));
            t = V * (1.0 - (S * (1.0 - f)));

            switch (i)
            {
                case 0:
                    r = V;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = V;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = V;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = V;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = V;
                    break;

                default:
                    r = V;
                    g = p;
                    b = q;
                    break;
            }
        }

        return System.Drawing.Color.FromArgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    public static (double h, double s, double v) RGBToHSV(System.Drawing.Color color)
    {
        float cmax = Math.Max(color.R, Math.Max(color.G, color.B));
        float cmin = Math.Min(color.R, Math.Min(color.G, color.B));
        float delta = cmax - cmin;

        float hue = 0;
        float saturation = 0;

        if (cmax == color.R)
        {
            hue = 60 * (((color.G - color.B) / delta) % 6);
        }
        else if (cmax == color.G)
        {
            hue = 60 * ((color.B - color.R) / delta + 2);
        }
        else if (cmax == color.B)
        {
            hue = 60 * ((color.R - color.G) / delta + 4);
        }

        if (cmax > 0)
        {
            saturation = delta / cmax;
        }

        return (hue, saturation, cmax);
    }


    public static (double h, double s, double v) HsvInterp((double h, double s, double v) c0, (double h, double s,
        double v) c1, double t)
    {
        double h = ((1 - t) * c0.h + t * c1.h) % 360;
        double s = Math.Clamp(((1 - t) * c0.s + t * c1.s), 0, 1);
        double v = Math.Clamp(((1 - t) * c0.v + t * c1.v), 0, 1);
        return (h, s, v);
    }
}