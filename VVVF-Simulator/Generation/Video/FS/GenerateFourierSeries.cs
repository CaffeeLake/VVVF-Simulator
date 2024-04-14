﻿using System;
using System.Drawing;
using VvvfSimulator.Yaml.VvvfSound;
using static VvvfSimulator.VvvfStructs;

namespace VvvfSimulator.Generation.Video.FS
{
    public class GenerateFourierSeries
    {

        public static double GetFourier(ref WaveValues[] UVW, int N, double InitialPhase)
        {
            double integral = 0;
            double dt = 1.0 / (UVW.Length - 1);

            for (int i = 0; i < UVW.Length; i++)
            {
                double iTime = MyMath.M_2PI * i / (UVW.Length - 1) - InitialPhase;
                double sum = (UVW[i].U - UVW[i].V) * MyMath.sin(N * iTime) * dt;
                integral += sum;
            }
            double bn = integral / 1.1026577908425;
            return Math.Round(bn, 4);
        }

        public static double GetFourierFast(ref WaveValues[] UVW, int N, double InitialPhase)
        {
            double integral = 0;

            int Ft = 0;
            double Time = -InitialPhase;

            for (int i = 0; i < UVW.Length; i++)
            {
                int iFt = UVW[i].U - UVW[i].V;

                if (i == 0)
                {
                    Ft = iFt;
                    continue;
                }

                if (Ft == iFt) continue;
                double iTime = MyMath.M_2PI * i / (UVW.Length - 1) - InitialPhase;
                double sum = (-MyMath.cos(N * iTime) + MyMath.cos(N * Time)) * Ft / N;
                integral += sum;

                Time = iTime;
                Ft = iFt;
            }
            double bn = integral / MyMath.M_2PI / 1.1026577908425;
            return Math.Round(bn, 4);
        }

        public static double[] GetFourierCoefficients(ref WaveValues[] UVW, int N, double InitialPhase)
        {
            double[] coefficients = new double[N];
            for (int n = 1; n <= N; n++)
            {
                double result = GetFourierFast(ref UVW, n, InitialPhase);
                coefficients[n-1] = result;
            }
            return coefficients;
        }

        public static double[] GetFourierCoefficients(VvvfValues Control, YamlVvvfSoundData Sound, int Delta, int N)
        {
            Control.SetRandomFrequencyMoveAllowed(false);
            WaveValues[] PWM_Array = GenerateBasic.GetUVWCycle(Control, Sound, MyMath.M_PI_6, Delta, false);
            return GetFourierCoefficients(ref PWM_Array, N, 0);
        }

        public static string GetDesmosFourierCoefficientsArray(ref double[] coefficients)
        {
            String array = "C = [";
            for(int i = 0; i < coefficients.Length; i++)
            {
                array += (i == 0 ? "" : " ,") + coefficients[i];
            }
            array += "]";
            return array;
        }

        private static class MagnitudeColor
        {

            private static double Linear(double x, double x1, double x2, double y1, double y2)
            {
                double val = (y2 - y1) / (x2 - x1) * (x - x1) + y1;
                return val;
            }

            private static double[] LinearRGB(double x, double x1, double x2, double r1, double g1, double b1, double r2, double g2, double b2)
            {
                double[] val = new double[3]
                {
                    Linear(x, x1, x2, r1, r2),
                    Linear(x, x1, x2, g1, g2),
                    Linear(x, x1, x2, b1, b2)
                };
                return val;
            }

            public static Color GetColor(double rate)
            {
                double[] rgb = new double[3] { 0,0,0 };
                if (rate >= 0.85) rgb = new double[3]{255, 85, 85};
                if (rate < 0.85) rgb = LinearRGB(rate, 0.85, 0.68, 255, 85, 85, 255, 205, 85);
                if (rate < 0.68) rgb = LinearRGB(rate, 0.68, 0.5, 255, 205, 85, 206, 224, 0);
                if (rate < 0.5) rgb = LinearRGB(rate, 0.5, 0.38, 206, 224, 0, 115, 227, 117);
                if (rate < 0.38) rgb = LinearRGB(rate, 0.15, 0.38, 77, 148, 232, 115, 227, 117);
                if (rate < 0.15) rgb = new double[3] { 77, 148, 232 };

                Color color = Color.FromArgb((int)rgb[0], (int)rgb[1], (int)rgb[2]);
                return color;
            }
        }

        public static Bitmap GetImage(ref double[] Coefficients)
        {
            Bitmap image = new(1000, 1000);
            Graphics g = Graphics.FromImage(image);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, 1000, 1000);

            int count = Coefficients.Length;
            int width = 1000 / count;

            for (int i = 0; i < count; i++)
            {
                double result = Coefficients[i];
                int height = (int)( Math.Log10(result * 1000) * 1000 / 3.0 / 2.0 );
                SolidBrush solidBrush = new(MagnitudeColor.GetColor(Math.Abs(height*2.0/1000)));
                if(height < 0) g.FillRectangle(solidBrush, width * i, 500, width, -height);
                else g.FillRectangle(solidBrush, width * i, 500 - height, width, height);

                if(width > 10 && i != 0 && i != count - 1) g.DrawLine(new Pen(Color.Gray), width * i, 0, width * i, 1000);
            }

            g.Dispose();
            return image;

        }

    }
}
