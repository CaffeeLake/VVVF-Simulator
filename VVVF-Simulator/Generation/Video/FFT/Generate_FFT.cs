﻿using NAudio.Dsp;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVF_Simulator.Yaml.VVVF_Sound;
using static VVVF_Simulator.Generation.Generate_Common;
using static VVVF_Simulator.Generation.Generate_Common.GenerationBasicParameter;
using static VVVF_Simulator.MainWindow;
using static VVVF_Simulator.VVVF_Structs;
using static VVVF_Simulator.Yaml.Mascon_Control.Yaml_Mascon_Analyze;

namespace VVVF_Simulator.Generation.Video.FFT
{
    public class Generate_FFT
    {
        private static readonly int pow = 15;
        private static Complex[] FFTNAudio(ref Wave_Values[] WaveForm)
        {
            Complex[] fft = new Complex[WaveForm.Length];
            for (int i = 0; i < WaveForm.Length; i++)
            {
                fft[i].X = (float)((WaveForm[i].U - WaveForm[i].V) * FastFourierTransform.HammingWindow(i, WaveForm.Length));;
                fft[i].Y = 0;
            }
            FastFourierTransform.FFT(true, pow, fft);
            Array.Resize(ref fft, fft.Length/2);
            return fft;
        }
        private static (float R, float θ) ConvertComplex(Complex C)
        {
            float R = C.X * C.X + C.Y * C.Y;
            float θ = (float)Math.Atan2(C.Y, C.X);
            return (R, θ);
        }
        public static Bitmap Get_FFT_Image(VVVF_Values control, Yaml_VVVF_Sound_Data sound)
        {
            control.set_Allowed_Random_Freq_Move(false);
            Wave_Values[] PWM_Array = Generate_Basic.Get_UVW_Sec(control, sound, My_Math.M_PI_6, (int)Math.Pow(2,pow) - 1, false);
            Complex[] FFT = FFTNAudio(ref PWM_Array);

            Bitmap image = new(1000, 1000);
            Graphics g = Graphics.FromImage(image);

            g.FillRectangle(new SolidBrush(Color.White),0,0, 1000, 1000);

            for (int i = 0; i < 1000 - 1; i++)
            {
                var (Ri, θi) = ConvertComplex(FFT[(int)(My_Math.M_PI * i)]);
                var (Rii, θii) = ConvertComplex(FFT[(int)(My_Math.M_PI * (i + 1))]);
                PointF start = new(i, 1000 - Ri * 2000);
                PointF end = new(i + 1, 1000 - Rii * 2000);
                g.DrawLine(new Pen(Color.Black, 2), start, end);
            }

            g.Dispose();

            return image;

        }

        public static void Generate_FFT_Video(GenerationBasicParameter generationBasicParameter, String fileName)
        {
            Yaml_VVVF_Sound_Data vvvfData = generationBasicParameter.vvvfData;
            Yaml_Mascon_Data_Compiled masconData = generationBasicParameter.masconData;
            ProgressData progressData = generationBasicParameter.progressData;

            VVVF_Values control = new();
            control.reset_control_variables();
            control.reset_all_variables();

            control.set_Allowed_Random_Freq_Move(false);

            int fps = 60;

            int image_width = 1000;
            int image_height = 1000;

            VideoWriter vr = new(fileName, OpenCvSharp.FourCC.H264, fps, new OpenCvSharp.Size(image_width, image_height));


            if (!vr.IsOpened())
            {
                return;
            }

            // Progress Initialize
            progressData.Total = masconData.GetEstimatedSteps(1.0 / fps) + 120;

            Boolean START_WAIT = true;
            if (START_WAIT)
                Generate_Common.Add_Empty_Frames(image_width, image_height, 60, vr);

            // PROGRESS CHANGE
            progressData.Progress+=60;

            Boolean loop = true;
            while (loop)
            {

                control.set_Sine_Time(0);
                control.set_Saw_Time(0);

                Bitmap image = Get_FFT_Image(control, vvvfData);


                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);
                vr.Write(mat);
                mat.Dispose();
                ms.Dispose();

                MemoryStream resized_ms = new();
                Bitmap resized = new(image, image_width / 2, image_height / 2);
                resized.Save(resized_ms, ImageFormat.Png);
                byte[] resized_img = resized_ms.GetBuffer();
                Mat resized_mat = OpenCvSharp.Mat.FromImageData(resized_img);
                Cv2.ImShow("Wave Form", resized_mat);
                Cv2.WaitKey(1);
                resized_mat.Dispose();
                resized_ms.Dispose();

                image.Dispose();

                loop = Generate_Common.Check_For_Freq_Change(control, masconData, vvvfData.mascon_data, 1.0 / fps);
                if (progressData.Cancel) loop = false;

                // PROGRESS CHANGE
                progressData.Progress++;
            }

            Boolean END_WAIT = true;
            if (END_WAIT)
                Generate_Common.Add_Empty_Frames(image_width, image_height, 60, vr);

            // PROGRESS CHANGE
            progressData.Progress += 60;

            vr.Release();
            vr.Dispose();
        }

        public static void Generate_FFT_Image(String fileName, Yaml_VVVF_Sound_Data sound_data, double d)
        {
            VVVF_Values control = new();

            control.reset_control_variables();
            control.reset_all_variables();
            control.set_Allowed_Random_Freq_Move(false);

            control.set_Sine_Angle_Freq(d * My_Math.M_2PI);
            control.set_Control_Frequency(d);

            Bitmap image = Get_FFT_Image(control, sound_data);

            MemoryStream ms = new();
            image.Save(ms, ImageFormat.Png);
            byte[] img = ms.GetBuffer();
            Mat mat = Mat.FromImageData(img);

            image.Save(fileName, ImageFormat.Png);


            Cv2.ImShow("FFT", mat);
            Cv2.WaitKey();
            image.Dispose();
        }
    }
}
