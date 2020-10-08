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
using System.Threading;
using System.Numerics;
using System.Diagnostics;
using ZedGraph;

namespace colorization
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int mx,ux;
        int my,uy;

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


            if (d == 65535) return 0.0;
            if (d == 0) d = 0.5;
            return 1.0 / (d*d*d*d);

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

            Bitmap image2 = (Bitmap)pictureBox2.Image;
            int col = image2.Width;
            int row = image2.Height;

            Dictionary<int, int> id = new Dictionary<int, int>();
            Dictionary<int, double> transtocb = new Dictionary<int, double>();
            Dictionary<int, double> transtocr = new Dictionary<int, double>();
            Image<Bgr, Byte> tmp1 = new Image<Bgr, Byte>(row, col);
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    Color pixelColor = image2.GetPixel(j, i);
                    tmp1.Data[i, j, 0] = pixelColor.B;
                    tmp1.Data[i, j, 1] = pixelColor.G;
                    tmp1.Data[i, j, 2] = pixelColor.R;

                }
            }



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

            List<Tuple<int, int, int>> rgb = new List<Tuple<int, int, int>>();
            int cnt = 1;
          
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    Color pixelColor = image2.GetPixel(j, i);
                    if (Math.Abs(pixelColor.B - pixelColor.G) > 20 ||
                          Math.Abs(pixelColor.R - pixelColor.G) > 20 ||
                          Math.Abs(pixelColor.B - pixelColor.R) > 20)
                    {
                        check[i, j] = 1;
                        vectorcb[i, j] = 128.0-0.168736*pixelColor.R-0.331264*pixelColor.G+0.5*pixelColor.B;
                        vectorcr[i, j] = 128.0+0.5*pixelColor.R-0.418688*pixelColor.G-0.081312*pixelColor.B;

                        int code = pixelColor.R * 255 * 255 + pixelColor.G * 255 + pixelColor.B;

                        int minr = 99999999;
                        int mostrelateid = 0;

                        for (int t = 0; t < rgb.Count; t++)
                        {
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

                        if (minr > 30)
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
                        else
                        {

                            colortype = id[rgb[mostrelateid].Item3 * 255 * 255 + rgb[mostrelateid].Item2 * 255 + rgb[mostrelateid].Item1];
                        }

                        dis[i, j, colortype] = 0;

                        Loc tmp = new Loc(i, j, colortype);
                        q.Enqueue(tmp);

                    }
                }
            }

            int countbfs = 0;
            while (q.Count > 0)
            {
                Loc now = q.Dequeue();
                countbfs++;
                if (now.i - 1 >= 0)
                {

                    if (dis[now.i - 1, now.j, now.col] > dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i - 1, now.j]))
                    {

                        dis[now.i - 1, now.j, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i - 1, now.j]);
                        Loc newtmp = new Loc(now.i - 1, now.j, now.col);
                       
                        q.Enqueue(newtmp);

                    }
                }


                if (now.i + 1 < row)
                {

                    if (dis[now.i + 1, now.j, now.col] > dis[now.i, now.j, now.col] +
    Math.Abs(vectory[now.i, now.j] - vectory[now.i + 1, now.j]))
                    {

                        dis[now.i + 1, now.j, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i + 1, now.j]);
                        Loc newtmp = new Loc(now.i + 1, now.j, now.col);
                        q.Enqueue(newtmp);

                    }
                }

                if (now.j - 1 >= 0)
                {

                    if (dis[now.i, now.j - 1, now.col] > dis[now.i, now.j, now.col] +
    Math.Abs(vectory[now.i, now.j] - vectory[now.i, now.j - 1]))
                    {

                        dis[now.i, now.j - 1, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i, now.j - 1]);
             
                        Loc newtmp = new Loc(now.i, now.j - 1, now.col);
                        q.Enqueue(newtmp);

                    }

                }

                if (now.j + 1 < col)
                {

                    if (dis[now.i, now.j + 1, now.col] > dis[now.i, now.j, now.col] +
Math.Abs(vectory[now.i, now.j] - vectory[now.i, now.j + 1]))
                    {

                        dis[now.i, now.j + 1, now.col] = dis[now.i, now.j, now.col] +
                        Math.Abs(vectory[now.i, now.j] - vectory[now.i, now.j + 1]);
                        Loc newtmp = new Loc(now.i, now.j + 1, now.col);
                        q.Enqueue(newtmp);

                    }

                }
            }

            Console.WriteLine(countbfs);

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {


                    if (check[i, j] == 1)
                    {
                        resultcb[i, j] = vectorcb[i, j];
                        resultcr[i, j] = vectorcr[i, j];
                        continue;
                    }

                    for (int k = 1; k < cnt; k++)
                    {
          
                        //Console.WriteLine(dis[i, j, k]);

                        resultweightcb[i, j] += weight(dis[i, j, k]);
                        resultweightcr[i, j] += weight(dis[i, j, k]) ;
                        //Console.WriteLine(transtocb[k]);
                        resultcb[i, j] += weight(dis[i, j, k]) * transtocb[k] ;
                        resultcr[i, j] += weight(dis[i, j, k]) * transtocr[k] ;
                        //Console.WriteLine(transtocr[k]);

                        
                    }
                    resultcb[i, j] /= resultweightcb[i, j];
                    resultcr[i, j] /= resultweightcr[i, j];
                    

                    //Console.WriteLine(resultweightcb[i, j]);
                    //Console.WriteLine(resultweightcr[i, j]);
                    //Console.WriteLine(resultcb[i, j]);
                    //Console.WriteLine(resultcr[i, j]);
                }
            }


            Image<Bgr, Byte> resultimage = new Image<Bgr, Byte>(col, row);

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    //resultimage.Data[i, j, 0] = (Byte)( (vectory[i, j])  * (resultcb[i, j] - 128)+0.5 );

                    //resultimage.Data[i, j, 1] = (Byte)((vectory[i, j]) - 0.34414 * (resultcb[i, j]-128) - 0.71414 * (resultcr[i, j]-128)+0.5 );
                    //resultimage.Data[i, j, 2] = (Byte)( (vectory[i, j]) + 1.402 * (resultcr[i, j]-128)+0.5);
                    resultimage.Data[i, j, 0] = (Byte)Math.Max(0, Math.Min(255, ((vectory[i, j]) + 1.772 * (resultcb[i, j] - 0x80) + 0.5)));

                    resultimage.Data[i, j, 1] = (Byte)Math.Max(0, Math.Min(255, ((vectory[i, j]) - 0.34414 * (resultcb[i, j] - 0x80) - 0.71414 * (resultcr[i, j] - 0x80) + 0.5)));
                    
                    resultimage.Data[i, j, 2] = (Byte) Math.Max(0,Math.Min(255,((vectory[i, j]) + 1.402*(resultcr[i,j]-0x80) )+0.5 ) );

                  
                   
                  
                }
            }
            pictureBox3.Image = resultimage.ToBitmap();
            //B = 1.164Y + 2.018Cb - 276.928
            //G = 1.164Y - 0.391Cb - 0.813Cr + 135.488
            //R = 1.164Y + 1.596Cr - 222.912






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





        }

        private void button2_Click(object sender, EventArgs e)
        {
        
            int col;
            int row;

            OpenFileDialog Openfile = new OpenFileDialog();
            Bitmap image1 = (Bitmap)pictureBox1.Image;
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(Openfile.FileName);
                Bitmap map = My_Image.ToBitmap();
                 col = My_Image.Width;
                row = My_Image.Height;
                pictureBox2.Image = map;

                //Graphics g = Graphics.FromImage(map);

                //Rectangle circle = new Rectangle(50, 70, 40, 40);
                //g.FillEllipse(Brushes.Red, circle);

                //Rectangle circle1 = new Rectangle(120, 10, 40, 40);
                //g.FillEllipse(Brushes.Green, circle1);

                //Rectangle circle2 = new Rectangle(130, 130, 40, 40);
                //g.FillEllipse(Brushes.DodgerBlue, circle2);
              

                Image<Bgr, Byte> tmp1 = new Image<Bgr, Byte>(col,row );
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        Color pixelColor = image1.GetPixel(j, i);
                        tmp1.Data[i, j, 0] = pixelColor.B;
                        tmp1.Data[i, j, 1] = pixelColor.G;
                        tmp1.Data[i, j, 2] = pixelColor.R;

                    }
                }
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {

                        vectory[i, j] = 0.299 * tmp1.Data[i, j, 2] + 0.587 * tmp1.Data[i, j, 1] + 0.114 * tmp1.Data[i, j, 0];
                        //vectorcb[i, j] = 128 - 0.168736 * tmp.Data[i, j, 2] - 0.331264 * tmp.Data[i, j, 1] + 0.5 * tmp.Data[i, j, 0];
                        //vectorcr[i, j] = 128 +0.5* tmp.Data[i, j, 2] - 0.418688 * tmp.Data[i, j, 1] - 0.081312 * tmp.Data[i, j, 0];
                    }
                }


            }
   





        }

        private void button4_Click(object sender, EventArgs e)
        {




        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_MouseDown_1(object sender, MouseEventArgs e)
        {
            mx = e.X;
            my = e.Y;

            Bitmap image2 = (Bitmap)pictureBox2.Image;
            int col = image2.Width;
            int row = image2.Height;
            Image<Bgr, Byte> tmp = new Image<Bgr, Byte>(row, col);

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
            pictureBox2.Image = tmp.ToBitmap();
            Graphics g = Graphics.FromImage(pictureBox2.Image);
            var brush = new SolidBrush(Color.FromArgb(255, (byte)(Convert.ToInt32(textBox1.Text)), (byte)Convert.ToInt32(textBox2.Text), (byte)Convert.ToInt32(textBox3.Text)));
            Rectangle circle1 = new Rectangle(mx, my, 20, 20);
            g.FillEllipse(brush, circle1);
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
   
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            
        }


    }
}
