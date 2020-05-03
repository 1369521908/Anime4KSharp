using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace Anime4KSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Error: 添加输入文件 如(..\\images\\Rand0mZ_King_Bicubic.png)和输出文件(Rand0mZ_King_Bicubic_cov.png)路径");
                return;
            }

            string inputPath = args[0];
            string outputPath = args[1];
            int push = 2;
            float scale = 2f;

            if (inputPath.Equals(outputPath))
            {
                Console.WriteLine("输入{0} \n 输出{1} \n 路径不能相同", inputPath, outputPath);
                return;
            }

            // int workthread = 20;
            // int iothread = 20;

            // ThreadPool.SetMinThreads(workthread, iothread);
            // ThreadPool.SetMaxThreads(workthread, iothread);

            DateTime begin = DateTime.UtcNow;

            // get image inside the inputPath 
            string[] files = Directory.GetFiles(inputPath);

            for (int i = 0; i < files.Length; i++)
            {
                // dosn`t work...
                // WaitCallback method = (t) => Program.Convert(files[i], args, scale, push, outputPath);
                // ThreadPool.QueueUserWorkItem((t) =>
                // {
                //     Program.Convert(files[i], args, scale, push, outputPath);
                // });

                Pross(files[i], args, scale, push, outputPath);

                // ThreadPool.QueueUserWorkItem(new WaitCallback((t) => Program.Convert(files[i], args, scale, push, outputPath)));
            }

            TimeSpan span = DateTime.UtcNow - begin;
            Console.WriteLine("span {0}", span);
            Console.ReadLine();
        }

        public static void Pross(string file, string[] args, float scale, int push, string outputPath)
        {
            // get primary file name 
            string fileName = file.Substring(file.LastIndexOf("\\") + 1);
            Bitmap img = new Bitmap(file);
            try
            {
                img = copyType(img);

                if (args.Length >= 3)
                {
                    scale = Convert.ToSingle(args[2]);
                }

                float pushStrength = scale / 6f;
                float pushGradStrength = scale / 2f;

                if (args.Length >= 4)
                {
                    pushStrength = Convert.ToSingle(args[3]);
                }

                if (args.Length >= 5)
                {
                    pushGradStrength = Convert.ToSingle(args[4]);
                }

                // 放大
                img = upscale(img, (int)(img.Width * scale), (int)(img.Height * scale));
                //img.Save("Bicubic.png", ImageFormat.Png);

                // Push twice to get sharper lines. 推两次以得到清晰的线条(值越大, 线条越细, 最大值8就够了)
                for (int j = 0; j < push; j++)
                {
                    // Compute Luminance and store it to alpha channel.
                    img = ImageProcess.ComputeLuminance(img);
                    //img.Save("Luminance.png", ImageFormat.Png);

                    // Push (Notice that the alpha channel is pushed with rgb channels).
                    Bitmap img2 = ImageProcess.PushColor(img, clamp((int)(pushStrength * 255), 0, 0xFFFF));
                    //save(img2, inputFile.Replace("images", "out") + ".Push.png", ImageFormat.Png);
                    img.Dispose();
                    img = img2;

                    // Compute Gradient of Luminance and store it to alpha channel.
                    img2 = ImageProcess.ComputeGradient(img);
                    //save(img2, inputFile.Replace("images", "out") + "Grad.png", ImageFormat.Png);
                    img.Dispose();
                    img = img2;

                    // Push Gradient
                    img2 = ImageProcess.PushGradient(img, clamp((int)(pushGradStrength * 255), 0, 0xFFFF));
                    img.Dispose();
                    img = img2;
                }
                save(img, outputPath + fileName, ImageFormat.Png);
            }
            catch (Exception exception)
            {
                Console.WriteLine("{0} 转换失败 \n", file);
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);
            }
            finally
            {
                img.Dispose();
            }

            Console.WriteLine("done {0}", outputPath + fileName);
        }

        static void save(Image image, string filename, ImageFormat format)
        {
            string dir = filename.Substring(0, filename.LastIndexOf("\\"));
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            image.Save(filename, ImageFormat.Png);
        }

        static Bitmap copyType(Bitmap bm)
        {
            Rectangle rect = new Rectangle(0, 0, bm.Width, bm.Height);
            Bitmap clone = bm.Clone(rect, PixelFormat.Format32bppArgb);

            return clone;
        }

        static Bitmap upscale(Bitmap bm, int width, int height)
        {
            // Upscale image with Bicubic interpolation.
            Bitmap newImage = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            Graphics g = Graphics.FromImage(newImage);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.DrawImage(bm, 0, 0, width, height);
            bm.Dispose();
            return newImage;
        }

        private static int clamp(int i, int min, int max)
        {
            if (i < min)
            {
                i = min;
            }
            else if (i > max)
            {
                i = max;
            }

            return i;
        }
    }
}
