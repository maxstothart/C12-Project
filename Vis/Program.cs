using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using CT = Tools.ConsoleTools;
using LCSV = Tools.LoadCSVFromFile;
using Base;

namespace Vis
{
class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            // This starts the UI loop and opens the window
            Application.Run(new ShowNetwork(Network.fromFile("E:\\Vis\\Data\\Network.dat")));
        }
    }

    public class VisualiserForm : Form
    {
        private float _x = 50, _y = 50, _speedX = 3, _speedY = 3;
        private const int DotSize = 15;

        public VisualiserForm()
        {
            this.Text = "Dot Visualiser";
            this.DoubleBuffered = true;
            this.Size = new Size(400, 400);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 16 };
            timer.Tick += (s, e) => {
                _x += _speedX;
                _y += _speedY;

                if (_x < 0 || _x > ClientSize.Width - DotSize) _speedX *= -1;
                if (_y < 0 || _y > ClientSize.Height - DotSize) _speedY *= -1;

                this.Invalidate();
            };
            timer.Start();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(Brushes.Red, _x, _y, DotSize, DotSize);
        }
    }
    public class ShowNetwork : Form
    {
        private Network N;
        public ShowNetwork(Network _N, int WindowHeight = 400, int WindowWidth = 400)
        {
            N = _N;
            this.Size = new Size(WindowHeight, WindowWidth);
            this.DoubleBuffered = true;

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) =>
            {

            };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //drawDot(e, Brushes.Red, .7f, .5f, .02f);
            //drawDot(e, Brushes.Red, .5f, .7f, .02f);
            drawLayer(e, 8, .5f);
        }


        public static void drawNode(PaintEventArgs e, Brush color, float xAsDecimal, float yAsDecimal, float DotSize)
        {
            float dotX = (e.ClipRectangle.Width * xAsDecimal) - (e.ClipRectangle.Width * DotSize) / 2;
            float dotY = (e.ClipRectangle.Height * yAsDecimal) - (e.ClipRectangle.Width * DotSize) / 2;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(color, dotX, dotY, (e.ClipRectangle.Width * DotSize), (e.ClipRectangle.Width * DotSize));
        }


        public static void drawLayer(PaintEventArgs e, int numberOfNodes, float xAsDecimal, float YPadding = 0.1f, float nodeSpaceRatio=.3f)
        {
            float windowSize = 1f - YPadding * 2;
            float nodeSpacing = windowSize / numberOfNodes;
            float nodeSize = nodeSpacing * nodeSpaceRatio;

            for (int i = 0; i < numberOfNodes; i++)
            {
                drawNode(e, Brushes.Red, xAsDecimal, YPadding + nodeSpacing * i, nodeSize);
            }
        }

        public static void drawNetwork()
        {

        }

    }

}
