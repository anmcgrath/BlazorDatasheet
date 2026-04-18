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

    public static (double l, double a, double b) RgbToOklab(System.Drawing.Color color)
    {
        double r = SrgbToLinear(color.R / 255.0);
        double g = SrgbToLinear(color.G / 255.0);
        double b = SrgbToLinear(color.B / 255.0);

        double l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
        double m = 0.2119034982 * r + 0.6740817638 * g + 0.1140147380 * b;
        double s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

        double l_ = Math.Cbrt(l);
        double m_ = Math.Cbrt(m);
        double s_ = Math.Cbrt(s);

        return (
            0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
            1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
            0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_
        );
    }

    public static System.Drawing.Color OklabToRgb(double l, double a, double b)
    {
        double l_ = l + 0.3963377774 * a + 0.2158037573 * b;
        double m_ = l - 0.1055613458 * a - 0.0638541728 * b;
        double s_ = l - 0.0894841775 * a - 1.2914855480 * b;

        double l_lin = l_ * l_ * l_;
        double m_lin = m_ * m_ * m_;
        double s_lin = s_ * s_ * s_;

        double rVal = +4.0767416621 * l_lin - 3.3077115913 * m_lin + 0.2309699292 * s_lin;
        double gVal = -1.2684380046 * l_lin + 2.6097574011 * m_lin - 0.3413193965 * s_lin;
        double bVal = -0.0041960863 * l_lin - 0.7034186147 * m_lin + 1.7076147010 * s_lin;

        return System.Drawing.Color.FromArgb(
            (byte)Math.Clamp(LinearToSrgb(rVal) * 255, 0, 255),
            (byte)Math.Clamp(LinearToSrgb(gVal) * 255, 0, 255),
            (byte)Math.Clamp(LinearToSrgb(bVal) * 255, 0, 255)
        );
    }

    public static (double l, double a, double b) OklabInterp((double l, double a, double b) c0,
        (double l, double a, double b) c1, double t)
    {
        return (
            (1 - t) * c0.l + t * c1.l,
            (1 - t) * c0.a + t * c1.a,
            (1 - t) * c0.b + t * c1.b
        );
    }

    private static double SrgbToLinear(double x)
    {
        return x <= 0.04045 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
    }

    private static double LinearToSrgb(double x)
    {
        return x <= 0.0031308 ? 12.92 * x : 1.055 * Math.Pow(x, 1 / 2.4) - 0.055;
    }
}