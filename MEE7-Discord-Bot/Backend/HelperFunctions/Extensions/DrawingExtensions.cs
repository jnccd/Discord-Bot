﻿using IronSoftware.Drawing;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Color = IronSoftware.Drawing.Color;
using Point = IronSoftware.Drawing.Point;
using Rectangle = IronSoftware.Drawing.Rectangle;

namespace MEE7.Backend.HelperFunctions
{
    public static class DrawingExtensions
    {
        public static Bitmap CropImage(this Bitmap source, Rectangle section, bool dispose = true)
        {
            Bitmap bmp = new(section.Width, section.Height);

            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            if (dispose)
                source.Dispose();

            return bmp;
        }
        public static int GetGrayScale(this Color c) => (c.R + c.G + c.B) / 3;
        public static int GetColorDist(this Color c, Color C) => Math.Abs(c.R - C.R) + Math.Abs(c.G - C.G) + Math.Abs(c.B - C.B);
        public static Color HsvToRgb(this Vector3 HSV) // from https://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        {
            double h = HSV.X;
            double S = HSV.Y;
            double V = HSV.Z;

            int Clamp(int i)
            {
                if (i < 0) return 0;
                if (i > 255) return 255;
                return i;
            }

            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            return Color.FromArgb(
                Clamp((int)(R * 255.0)),
                Clamp((int)(G * 255.0)),
                Clamp((int)(B * 255.0)));
        }
        public static float GetValue(this Color c)
        {
            return new float[] { c.R / 255f, c.G / 255f, c.B / 255f }.Max();
        }
        public static Bitmap RotateImage(this Bitmap b, float AngleInDegrees, Vector2 RotationOrigin)
        {
            AnyBitmap re = new Bitmap(b.Width, b.Height);
            using (Graphics g = Graphics.FromImage(re))
            {
                g.TranslateTransform(RotationOrigin.X, RotationOrigin.Y);
                g.RotateTransform(AngleInDegrees);
                g.TranslateTransform(-RotationOrigin.X, -RotationOrigin.Y);
                g.DrawImage(b, new Point(0,0));
            }
            return re;
        }
        public static Point RotatePointAroundPoint(this Point P, Point RotationOrigin, double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Point((int)(cos * (P.X - RotationOrigin.X) - sin * (P.Y - RotationOrigin.Y) + RotationOrigin.X),
                             (int)(sin * (P.X - RotationOrigin.X) + cos * (P.Y - RotationOrigin.Y) + RotationOrigin.Y));
        }
        public static Color Lerp(this Color s, Color t, float k) // from http://www.java2s.com/example/csharp/system.drawing/lerp-between-two-color.html
        {
            static float keepInIntv(float f)
            {
                if (f < 0) f = 0;
                if (f > 255) f = 255;
                return f;
            }

            var bk = (1 - k);
            var a = s.A * bk + t.A * k;
            var r = s.R * bk + t.R * k;
            var g = s.G * bk + t.G * k;
            var b = s.B * bk + t.B * k;
            a = keepInIntv(a);
            r = keepInIntv(r);
            g = keepInIntv(g);
            b = keepInIntv(b);
            return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        }
        private static void MinMaxRgb(out int min, out int max, int r, int g, int b)
        {
            if (r > g)
            {
                max = r;
                min = g;
            }
            else
            {
                max = g;
                min = r;
            }
            if (b > max)
            {
                max = b;
            }
            else if (b < min)
            {
                min = b;
            }
        }
        public static float GetHue(this Color color)
        {
            int r = color.R, g = color.G, b = color.B;

            if (r == g && g == b)
                return 0f;

            MinMaxRgb(out int min, out int max, r, g, b);

            float delta = max - min;
            float hue;

            if (r == max)
                hue = (g - b) / delta;
            else if (g == max)
                hue = (b - r) / delta + 2f;
            else
                hue = (r - g) / delta + 4f;

            hue *= 60f;
            if (hue < 0f)
                hue += 360f;

            return hue;
        }
        public static float GetSaturation(this Color color)
        {
            int r = color.R, g = color.G, b = color.B;

            if (r == g && g == b)
                return 0f;

            MinMaxRgb(out int min, out int max, r, g, b);

            int div = max + min;
            if (div > byte.MaxValue)
                div = byte.MaxValue * 2 - max - min;

            return (max - min) / (float)div;
        }
        public static void ColorToHSV(this Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }
    }
}
