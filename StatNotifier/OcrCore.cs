using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace StatNotifier
{
    public class OcrCore
    {
        public class OcrResults
        {
            public String text;
            public Bitmap bmp;
            public OcrResults(String text, Bitmap bmp)
            {
                this.text = String.Copy(text);
                this.bmp = (Bitmap)bmp.Clone();
            }
        }
        Tesseract.TesseractEngine tesseract;
        Watcher watcher;
        const int MULTI = 6;
        OcrResults result;
        float scaling;
        public int threshold { get; set; }

        Bitmap toOcr;

        public OcrCore(Watcher watcher, float sc)
        {
            this.watcher = watcher;
            string path = @".\tessdata\";
            tesseract = new Tesseract.TesseractEngine(path, "eng");
            threshold = 190;
            scaling = sc;
        }
        public void setOcr() {
            //Bitmapの作成
            int ww = Convert.ToInt32(watcher.ClientSize.Width * scaling);
            if (ww < 1) ww = 1;
            int hh = Convert.ToInt32(watcher.ClientSize.Height * scaling);
            if (hh < 1) hh = 1;
            toOcr = new Bitmap(ww,hh );
            //Graphicsの作成
            Graphics g = Graphics.FromImage(toOcr);
            //ウインドウ内をコピーする
            Point loc = watcher.PointToClient(watcher.Bounds.Location);
            int xx = Convert.ToInt32((watcher.Bounds.Location.X - loc.X)*scaling);
            int yy = Convert.ToInt32((watcher.Bounds.Location.Y - loc.Y)*scaling);
            g.CopyFromScreen(xx, yy, 0, 0, toOcr.Size);
            g.Dispose();
        }
        public void doOcr()
        {
            Bitmap resizer = new Bitmap(toOcr.Width * MULTI, toOcr.Height * MULTI);
            Graphics g = Graphics.FromImage(resizer);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
            g.DrawImage(toOcr, 0, 0, resizer.Width, resizer.Height);
            Bitmap bw = Create1bppImage(resizer);

            Tesseract.Page p = tesseract.Process(bw);

            result = new OcrResults(p.GetText(), bw);

            //            bmp.Save("test0.jpg");
            //            resizer.Save("test.jpg");
            //            bw.Save("testBw.jpg");

            bw.Dispose();
            resizer.Dispose();
            g.Dispose();
            p.Dispose();
            toOcr.Dispose();
            return;

        }

        public OcrResults getResults()
        {
            return result;
        }


        /// <summary>
        /// 指定された画像から1bppのイメージを作成する
        /// </summary>
        /// <param name="img">基になる画像</param>
        /// <returns>1bppに変換されたイメージ</returns>
        public Bitmap Create1bppImage(Bitmap img) //どこで拾ったソースだっけ?
        {
            //1bppイメージを作成する
            Bitmap newImg = new Bitmap(img.Width, img.Height,
                PixelFormat.Format1bppIndexed);

            //Bitmapをロックする
            BitmapData bmpDate = newImg.LockBits(
                new Rectangle(0, 0, newImg.Width, newImg.Height),
                ImageLockMode.WriteOnly, newImg.PixelFormat);

            //新しい画像のピクセルデータを作成する
            byte[] pixels = new byte[bmpDate.Stride * bmpDate.Height];
            for (int y = 0; y < bmpDate.Height; y++)
            {
                for (int x = 0; x < bmpDate.Width; x++)
                {
                    //明るさが一定以上の時は白くする
                    if ( threshold / 255.0 < img.GetPixel(x, y).GetBrightness())
                    {
                        //ピクセルデータの位置
                        int pos = (x >> 3) + bmpDate.Stride * y;
                        //白くする
                        pixels[pos] |= (byte)(0x80 >> (x & 0x7));
                    }
                }
            }
            //作成したピクセルデータをコピーする
            IntPtr ptr = bmpDate.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, pixels.Length);

            //ロックを解除する
            newImg.UnlockBits(bmpDate);

            return newImg;
        }
        ~OcrCore()
        {
        }

    }
}
