namespace BlazorDatasheet.Core.Color;

public class ColorConverter
{
    public static System.Drawing.Color HsvToRgb(double h, double s, double v)
    {
        double r, g, b;

        if (s == 0)
        {
            r = v;
            g = v;
            b = v;
        }
        else
        {
            int i;
            double f, p, q, t;

            if (Math.Abs(h - 360) < 0.001)
                h = 0;
            else
                h = h / 60;

            i = (int)Math.Truncate(h);
            f = h - i;

            p = v * (1.0 - s);
            q = v * (1.0 - (s * f));
            t = v * (1.0 - (s * (1.0 - f)));

            switch (i)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;

                default:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }
        }

        return System.Drawing.Color.FromArgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    public static (double h, double s, double v) RgbToHsv(System.Drawing.Color color)
    {
        float cmax = Math.Max(color.R, Math.Max(color.G, color.B));
        float cmin = Math.Min(color.R, Math.Min(color.G, color.B));
        float delta = cmax - cmin;

        float hue = 0;
        float saturation = 0;

        if (Math.Abs(cmax - color.R) < 0.001)
        {
            hue = 60 * (((color.G - color.B) / delta) % 6);
        }
        else if (Math.Abs(cmax - color.G) < 0.001)
        {
            hue = 60 * ((color.B - color.R) / delta + 2);
        }
        else if (Math.Abs(cmax - color.B) < 0.001)
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