﻿using System;
using System.Diagnostics;
using System.Drawing;
using VVVF_Simulator.Yaml.VVVF_Sound;
using static VVVF_Simulator.VVVF_Structs;

namespace VVVF_Simulator.Generation.Video.FS
{
    public class Generate_FS
    {

        public static double Get_Fourier(ref Wave_Values[] UVW, double N, double InitialPhase)
        {
            double integral = 0;
            double dt = 1.0 / (UVW.Length - 1);

            for (int i = 0; i < UVW.Length; i++)
            {
                double sum = (UVW[i].U - UVW[i].V) * My_Math.sin(N * (My_Math.M_2PI * i / (UVW.Length - 1) - InitialPhase)) * dt;
                integral += sum;
            }
            double bn = integral / 1.1026577908425;
            return Math.Round(bn, 4);
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
    

        public static Bitmap Get_FS_Image(VVVF_Values Control, Yaml_VVVF_Sound_Data Sound, int Delta, int Division)
        {
            Wave_Values[] PWM_Array = Generate_Common.Get_UWV_Cycle(Control, Sound, 0, Delta, false);
            

            Bitmap image = new(1000, 1000);
            Graphics g = Graphics.FromImage(image);

            g.FillRectangle(new SolidBrush(Color.White), 0, 0, 1000, 1000);

            int division = Division;
            int space = 1000 / division;

            //String fx = "f(x) = ";

            for (int i = 0; i <= division; i++)
            {
                double result = Get_Fourier(ref PWM_Array, i+1, My_Math.M_PI_6);
                int height = (int)( Math.Log10(result * 1000) * 1000 / 3.0 / 2.0 );
                //int height = (int)(result * 1000) / 2;
                
                SolidBrush solidBrush = new(MagnitudeColor.GetColor(Math.Abs(height*2.0/1000)));
                if(height < 0) g.FillRectangle(solidBrush, space * i, 500, 1000 / division, -height);
                else g.FillRectangle(solidBrush, space * i, 500 - height, 1000 / division, height);

                if(space > 10 && i != 0 && i != division) g.DrawLine(new Pen(Color.Gray), space * i, 0, space * i, 1000);

                //fx += (i == 0 ? "" : "+") + result + "sin(" + i + "x)";
            }

            //Debug.WriteLine(fx);

            g.Dispose();

            return image;

        }

    }
}
