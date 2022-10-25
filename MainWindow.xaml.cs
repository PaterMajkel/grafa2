using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace grafa2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        static string type = "";
        static int? width = null;
        static int? height = null;
        int? highest = null;
        string fileName;
        int binaryCounter = 0;
        List<int> values = new List<int>();
        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.ppm;)|*.ppm;|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                fileName = openFileDialog.FileName;
                using (StreamReader file = new StreamReader(fileName))
                {
                    int counter = 0;
                    binaryCounter = 0;
                    int lineCounter = 0;
                    string ln;
                    values = new();
                    while ((ln = file.ReadLine()) != null)
                    {
                        var splitLine = Regex.Split(ln, @"\s+").Where(s => s != string.Empty).ToArray();
                        binaryCounter += splitLine.Length;
                        var localCounter = 0;
                        lineCounter++;
                        foreach (var text in splitLine)
                        {
                            localCounter++;
                            var localText = text.Trim();
                            if (localText.Contains('#')) break;
                            if (String.IsNullOrEmpty(localText)) continue;

                            if (counter < bpp)
                            {

                                switch (counter)
                                {
                                    case 0:
                                        type = localText;
                                        break;
                                    case 1:
                                        width = int.Parse(localText);
                                        break;
                                    case 2:
                                        height = int.Parse(localText);
                                        break;
                                    case 3:
                                        highest = int.Parse(localText);
                                        break;
                                }
                                counter++;
                            }

                            if (highest != null)
                                binaryCounter -= splitLine.Length - localCounter + lineCounter;

                            

                        }
                        if (counter > 3)
                            break;
                    }
                    file.Close();

                }

                if (int.Parse(type[^1].ToString()) > 3)
                {
                    ReadBinaryFile(fileName);

                }
                else
                    ReadNormalFile(fileName);

               
                var bitmap = new Bitmap((int)width, (int)height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                var bytes = Array.ConvertAll<int, byte>(values.ToArray(), Convert.ToByte);

                bitmap = PassValues(bitmap, bytes);
                Img.Source = ImageSourceFromBitmap(bitmap);
            }
        }


        // A B C
        // D E F
        //

        // A B C 0
        // D E F 0
        // 1. Change the RawFormat from BMP to i.e. PNG
        // 2. Handle the bytes padding



        public void ReadBinaryFile(string fileName)
        {

            FileStream fs = new FileStream(fileName, FileMode.Open);
            int hexIn;
            string hex;

            for (int i = 0; (hexIn = fs.ReadByte()) != -1; i++)
            {
                if (i < binaryCounter)
                    continue;
                hex = string.Format("{0:X2}", hexIn);

                var val = this.NormalizeBinary(hex);
                 /*if (val == 35)
                     continue;*/
                values.Add(val);
            }

            fs.Close();

        }

        public void ReadNormalFile(string fileName)
        {
            using (StreamReader file = new StreamReader(fileName))
            {
                int counter = 0;
                string ln;
                values = new();
                while ((ln = file.ReadLine()) != null)
                {
                    var slitLine = Regex.Split(ln, @"\s+").Where(s => s != string.Empty).ToArray();
                    foreach (var text in slitLine)
                    {
                        var localText = text.Trim();
                        if (localText.Contains('#')) break;
                        if (String.IsNullOrEmpty(localText)) continue;

                        if (counter < bpp)
                        {
                            counter++;
                            continue;
                        }

                        values.Add(this.NormalizeInt(localText));


                    }
                }
                file.Close();

            }
        }
        public int NormalizeInt(string text)
        {
            var value = int.Parse(text);
            if (highest != 255)
            {
                return value * 255 / highest!.Value;
            }
            return value;
        }

        public int NormalizeBinary(string text)
        {
            var value = int.Parse(text, System.Globalization.NumberStyles.HexNumber);
            if (highest != 255)
            {
                return value * 255 / highest!.Value;
            }
            return value;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]

        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
            }
            finally { DeleteObject(handle); }
        }

        private const int bpp = 4;

        public static Bitmap PassValues(Bitmap bmp, byte[] values)
        {
           /* for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (y * bmp.Width * 3 + x * 3 > values.Length - 3)
                        continue;
                    var color = System.Drawing.Color.FromArgb(255,
                        values[y * bmp.Width * 3 + x * 3 + 0],
                        values[y * bmp.Width * 3 + x * 3 + 1],
                        values[y * bmp.Width * 3 + x * 3 + 2]);
                    bmp.SetPixel(x, y, color);
                }*/
            BitmapData data = bmp.LockBits(
                new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), 
                ImageLockMode.ReadWrite, 
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );
            byte[] vs = new byte[data.Width * MainWindow.bpp * data.Height];
            Marshal.Copy(data.Scan0, vs, 0, vs.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                {
                    /*if (y * bmp.Width * 3 + x * 3 > values.Length - 3)
                        continue;*/
                    vs[y * bmp.Width * bpp + x * bpp + 3] = 255;
                    vs[y * bmp.Width * bpp + x * bpp + 2] = (byte)(values[y * bmp.Width * 3 + x * 3 + 0]);
                    vs[y * bmp.Width * bpp + x * bpp + 0] = (byte)(values[y * bmp.Width * 3 + x * 3 + 1]);
                    vs[y * bmp.Width * bpp + x * bpp + 1] = (byte)(values[y * bmp.Width * 3 + x * 3 + 2]);
                }

            Marshal.Copy(vs, 0, data.Scan0, vs.Length);
            bmp.UnlockBits(data);
            return bmp;
        }
        public static Bitmap ConvertToBitmap(BitmapSource bitmapSource)
        {
            var width = bitmapSource.PixelWidth;
            var height = bitmapSource.PixelHeight;
            var stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
            bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), memoryBlockPointer, height * stride, stride);
            var bitmap = new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, memoryBlockPointer);
            return bitmap;
        }
        private void SaveFile(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Images|*.png;*.bmp;*.jpg";
            ImageFormat format = ImageFormat.Png;
            if (saveFileDialog.ShowDialog() == true)
            {
                string ext = System.IO.Path.GetExtension(saveFileDialog.FileName);
                switch (ext)
                {
                    case ".jpg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".png":
                        format = ImageFormat.Png;
                        break;
                }
                ConvertToBitmap((BitmapSource)Img.Source).Save(saveFileDialog.FileName, format);
            }
        }
    }
}
