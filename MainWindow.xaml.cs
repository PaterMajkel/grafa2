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

        string type = "";
        int? width = null;
        int? height = null;
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

                    string ln;
                    values = new();
                    while ((ln = file.ReadLine()) != null)
                    {
                        var splitLine = Regex.Split(ln, @"\s+").Where(s => s != string.Empty).ToArray();
                        binaryCounter += splitLine.Length;
                        foreach (var text in splitLine)
                        {
                            var localText = text.Trim();
                            if (localText.Contains('#')) break;
                            if (String.IsNullOrEmpty(localText)) continue;

                            if (counter < 4)
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
                                continue;
                            }

                            

                        }
                    }
                    file.Close();
                   
                }

                if (int.Parse(type[^1].ToString()) > 3)
                {
                    ReadBinaryFile(fileName);
                    
                }
                else
                    ReadNormalFile(fileName);

                var format = type switch
                {
                    "P1" => System.Drawing.Imaging.PixelFormat.Format1bppIndexed,
                    "P2" => System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                    "P3" => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                    "P4" => System.Drawing.Imaging.PixelFormat.Format1bppIndexed,
                    "P5" => System.Drawing.Imaging.PixelFormat.Format8bppIndexed,
                    "P6" => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                };
                var bitmap = new Bitmap((int)width, (int)height, format);
                var bytes = Array.ConvertAll<int, byte>(values.ToArray(), Convert.ToByte);

                bitmap = PassValues(bitmap, bytes);
                Img.Source = ImageSourceFromBitmap(bitmap);
            }
        }

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
               /* if (val == 35)
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

                        if (counter < 4)
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
                return value * 255 / (int)highest;
            }
            return value;
        }

        public int NormalizeBinary(string text)
        {
            var value = int.Parse(text, System.Globalization.NumberStyles.HexNumber);
            if (highest != 255)
            {
                return value * 255 / (int)highest;
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

        public static Bitmap PassValues(Bitmap bmp, byte[] values)
        {

            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            byte[] vs = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, vs, 0, vs.Length);


            for (int i = 0; i < data.Height; i++)
                for (int y = 0; y < data.Width; y++)
                    vs[i * bmp.Width + y] = (byte)(values[i * bmp.Width + y]);

            Marshal.Copy(vs, 0, data.Scan0, vs.Length);
            bmp.UnlockBits(data);
            return bmp;
        }
    }
}
