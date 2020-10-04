using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Globalization;

namespace colorization
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public struct Loc
        {
            public int i, j;
            public int col;
            public Loc(int a, int b, int c) {
                i = a;
                j = b;
                col = c;
            }
        };

        public double weight(double d) {
            double b = 1;
            return Math.Pow(d, -b);
        }

        const int N = 500;
        public int[,] check = new int[N, N];
        public double[,] vectorcb = new double[N, N];
        public double[,] vectorcr = new double[N, N];
        public double[,] vectory = new double[N, N];

        public double[,] resultcb = new double[N, N];
        public double[,] resultcr = new double[N, N];

        public double[,] resultweightcb = new double[N, N];
        public double[,] resultweightcr = new double[N, N];
        public double[,,] dis = new double[N, N, 1024];
        public Image<Ycc, Byte> YCrCbFrame;

        private void button3_Click(object sender, EventArgs e)
        {







        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog Openfile = new OpenFileDialog();
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(Openfile.FileName);

                pictureBox1.Image = My_Image.ToBitmap();
            }

            Bitmap image2 = (Bitmap)pictureBox1.Image;
            int col = image2.Width;
            int row = image2.Height;
            Image<Bgr, Byte> tmp = new Image<Bgr, Byte>(col, row);

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    Color pixelColor = image2.GetPixel(j, i);
                    tmp.Data[i, j, 0] = pixelColor.B;
                    tmp.Data[i, j, 1] = pixelColor.G;
                    tmp.Data[i, j, 2] = pixelColor.R;

                }
            }
            
            pictureBox1.Image = tmp.ToBitmap();
            YCrCbFrame = tmp.Convert<Ycc, Byte>();

            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {

                    vectory[i, j] = YCrCbFrame.Data[i, j, 0];
                }
            }



        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog Openfile = new OpenFileDialog();
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(Openfile.FileName);
                pictureBox2.Image = My_Image.ToBitmap();
            }
            Bitmap image2 = (Bitmap)pictureBox2.Image;
            int col = image2.Width;
            int row = image2.Height;
            Dictionary<int, int> id=new Dictionary<int, int>();
            Dictionary<int, double> transtocb=new Dictionary<int, double>();
            Dictionary<int, double> transtocr=new Dictionary<int, double>();

            Queue<Loc> q;
            q = new Queue<Loc>();

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    for (int k = 0; k < 1024; k++)
                    {
                        dis[i, j, k] = 100000000;
                    }
                }
            }

            List<Tuple<int, int, int>> rgb=new List<Tuple<int, int, int>>();
            int cnt = 1;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    Color pixelColor = image2.GetPixel(j, i);
                    if (Math.Abs(pixelColor.B - pixelColor.G) > 10 ||
                          Math.Abs(pixelColor.R - pixelColor.G) >10 ||
                          Math.Abs(pixelColor.B - pixelColor.R) > 10)
                    {
                        check[i, j] = 1;
                        vectorcb[i, j] = YCrCbFrame.Data[i, j, 1];
                        vectorcr[i, j] = YCrCbFrame.Data[i, j, 2];

                        int code = pixelColor.R * 255 * 255 + pixelColor.G * 255 + pixelColor.B;
                        
                        int minr = 9999999;
                        int mostrelateid = 0;

                        for (int t = 0; t < rgb.Count; t++) {
                            int tmpres = Math.Abs(pixelColor.B - rgb[t].Item1) + 
                                Math.Abs(pixelColor.G - rgb[t].Item2)
                                + Math.Abs(pixelColor.R - rgb[t].Item3);
                            if (minr > tmpres)
                            {
                                minr = Math.Min(minr, tmpres);
                                mostrelateid = t;
                            }

                            }

                     
                        int colortype;
                        int flag = 0;

                        if (minr > 40)
                        {
                            rgb.Add(new Tuple<int, int, int>(pixelColor.B, pixelColor.G, pixelColor.R));
                            flag = 1;
                            if (!id.ContainsKey(code))
                            {
                                id[code] = cnt;

                                cnt++;
                            }
                            colortype = id[code];

                            transtocb[colortype] = vectorcb[i, j];
                            transtocr[colortype] = vectorcr[i, j];


                        }
                        else {

                            colortype = id[rgb[mostrelateid].Item3*255*255+ rgb[mostrelateid].Item2*255+ rgb[mostrelateid].Item1];
                        }

                        dis[i, j, colortype] = 0;

                            Loc tmp = new Loc(i, j, colortype);
                            q.Enqueue(tmp);
                        
                    }
                }
            }

            while (q.Count > 0) {
                Loc now = q.Dequeue();

                if (now.i - 1 >= 0 ) {

                    if (dis[now.i - 1, now.j, now.col] > dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i - 1, now.j]) )
                    {

                        dis[now.i - 1, now.j, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i - 1, now.j]);
                        Loc newtmp = new Loc(now.i - 1, now.j, now.col);
                        q.Append(newtmp);

                    }
                }


                if (now.i + 1 < row) {

                    if (dis[now.i + 1, now.j, now.col] > dis[now.i, now.j, now.col] +
    Math.Abs(vectory[now.i, now.j] - vectory[now.i + 1, now.j]))
                    {

                        dis[now.i + 1, now.j, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i + 1, now.j]);
                        Loc newtmp = new Loc(now.i + 1, now.j, now.col);
                        q.Append(newtmp);
                    }
                }

                if (now.j - 1 >= 0 ) {

                    if (dis[now.i , now.j-1, now.col] > dis[now.i, now.j, now.col] +
    Math.Abs(vectory[now.i, now.j] - vectory[now.i , now.j-1]))
                    {

                        dis[now.i , now.j-1, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i, now.j-1]);
                        Loc newtmp = new Loc(now.i , now.j-1, now.col);
                        q.Append(newtmp);

                    }

                }

                if (now.j + 1 < col ) {

                    if (dis[now.i, now.j + 1, now.col] > dis[now.i, now.j, now.col] +
Math.Abs(vectory[now.i, now.j] - vectory[now.i , now.j+1]))
                    {

                        dis[now.i, now.j+1 , now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i, now.j + 1]);
                        Loc newtmp = new Loc(now.i, now.j + 1, now.col);
                        q.Append(newtmp);

                    }

                }
            }


            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {
                    if (check[i, j]==1)
                        continue;

                    for (int k = 1; k < cnt; k++) {
                        resultweightcb[i, j] += weight(dis[i, j, k]);
                        resultweightcr[i, j] += weight(dis[i, j, k]);
                        resultcb[i, j] += weight(dis[i, j, k]) * transtocb[k];
                        resultcr[i, j] += weight(dis[i, j, k]) * transtocr[k];
                    }
                    resultcb[i,j] /= resultweightcb[i,j];
                    resultcr[i, j] /= resultweightcr[i, j];
                }
            }


            Image<Bgr, Byte> resultimage = new Image<Bgr, Byte>(col, row);

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    resultimage.Data[i, j, 0] = (Byte)( 1.164*(vectory[i, j]) + 2.018 * (vectorcb[i, j] )-276.928);

 resultimage.Data[i, j, 1] = (Byte) (1.164 * (vectory[i,j] ) - 0.392 * (vectorcb[i,j] ) - 0.813 * (vectorcr[i,j]  )+135.488 );
                    resultimage.Data[i, j, 2] =  (Byte)(1.164*(vectory[i, j] ) + 1.596 * (vectorcr[i, j] )-222.912);



                }
            }
            pictureBox3.Image=resultimage.ToBitmap();
            //B = 1.164Y + 2.018Cb - 276.928
            //G = 1.164Y - 0.391Cb - 0.813Cr + 135.488
            //R = 1.164Y + 1.596Cr - 222.912






        }

        private void button4_Click(object sender, EventArgs e)
        {




        }
    }
}
