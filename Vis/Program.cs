using Base;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using Tools;
using CT = Tools.ConsoleTools;
using LCSV = Tools.LoadCSVFromFile;
using OP = Tools.Operations;

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
        private List<(float, float, int)> NodeCoordinates = new();
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
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            //showWindowSize(e, (.1f,.1f));
            //drawLayer(e, Brushes.Green, 2, .3f, .2f, 0.05f);
            //drawLayer(e, Brushes.Green, 4, .5f, .2f, 0.05f);
            //drawLayer(e, Brushes.Green, 3, .7f, .2f, 0.05f);
            List<int> structure = new List<int> { 2,4,3};
            drawNetwork(e, structure, (.1f, .3f), (0f, .8f), 0.005f);
        }

        public static void drawNode(PaintEventArgs e, Brush color, float xAsDecimal, float yAsDecimal, float DotSize)
        {
            float dotX = (e.ClipRectangle.Width * xAsDecimal) - (e.ClipRectangle.Width * DotSize) / 2;
            float dotY = (e.ClipRectangle.Height * yAsDecimal) - (e.ClipRectangle.Width * DotSize) / 2;

            e.Graphics.FillEllipse(color, dotX, dotY, (e.ClipRectangle.Width * DotSize), (e.ClipRectangle.Width * DotSize));
        }

        public static void drawLine(PaintEventArgs e, float xOrigin, float yOrigin, float xTarget, float yTarget, float Thickness = 0.02f, int color = 1)
        {
            (float X, float Y) = (e.ClipRectangle.Width * xOrigin, e.ClipRectangle.Height * yOrigin);
            (float X2, float Y2) = (e.ClipRectangle.Width * xTarget, e.ClipRectangle.Height * yTarget);
            float LineThickness = (e.ClipRectangle.Width * Thickness);
            Brush Colour = new Brush[6] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Orange, Brushes.Yellow, Brushes.Brown } [color % 6];

            e.Graphics.DrawLine(new Pen(Colour, LineThickness), X, Y, X2, Y2);
        }

        public void drawNetwork(PaintEventArgs e, List<int> structure, (float, float) Padding, (float, float) SpacingRatio, float lineThickness= 0.02f)
        {
            Brush color = Brushes.Black;

            (float, float) windowSize = (1f - Padding.Item1 * 2, 1f - Padding.Item2 * 2);
            float nodeSize = (windowSize.Item2 / Sort.Max(structure)) * SpacingRatio.Item2;
            float xAsDecimal = (windowSize.Item1 / (structure.Count + 1)) / 2;
            float xSpacing = xAsDecimal * 2 *(1f - SpacingRatio.Item1);

            for (int i = 0; i < structure.Count; i++)
            {
                xAsDecimal += xSpacing;
                float yAsDecimal = Padding.Item2 + windowSize.Item2 / (structure[i] + 1);

                for (int j  = 0; j < structure[i]; j++)
                {
                    NodeCoordinates.Add((xAsDecimal, yAsDecimal, i));
                    drawNode(e, color, xAsDecimal, yAsDecimal, windowSize.Item2 * nodeSize);
                    yAsDecimal += windowSize.Item2 / (structure[i] + 1);
                }
            }

            int start = 0; int next = 0;
            if (lineThickness > 0) {
                for (int i = 0; i < structure.Count; i++)
                {
                    next += structure[i];
                    if (structure.Count > i + 1)
                    {
                        for (int j = 0; j < structure[i + 1]; j++)
                        {
                            (float, float, int) target = NodeCoordinates[next + j];
                            for (int k = 0; k < structure[i]; k++)
                            {
                                (float, float, int) origin = NodeCoordinates[start + k];
                                drawLine(e, origin.Item1, origin.Item2, target.Item1, target.Item2, lineThickness, origin.Item3);
                            }
                        }
                    }
                    start += structure[i];
                } 
            }

            for (int i = 0; i < NodeCoordinates.Count; i++) { drawNode(e, color, NodeCoordinates[i].Item1, NodeCoordinates[i].Item2, windowSize.Item2 * nodeSize); }
        }

        public static void showWindowSize(PaintEventArgs e) { showWindowSize(e, (0f, 0f)); }
        public static void showWindowSize(PaintEventArgs e, (float, float) padding)
        {
            var color = Brushes.Red;
            float DotSize = 0.02f;

            float x = e.ClipRectangle.Width * (0f + padding.Item1);
            float y = e.ClipRectangle.Height * (0f + padding.Item2);

            e.Graphics.DrawRectangle(new Pen(Brushes.Black, e.ClipRectangle.Width * (DotSize / 1.2f)), x, y, e.ClipRectangle.Width - x * 2, e.ClipRectangle.Height - y * 2);

            drawNode(e, color, 0f + padding.Item1, 0f + padding.Item2, DotSize);
            drawNode(e, color, 1f - padding.Item1, 0f + padding.Item2, DotSize);
            drawNode(e, color, 0f + padding.Item1, 1f - padding.Item2, DotSize);
            drawNode(e, color, 1f - padding.Item1, 1f - padding.Item2, DotSize);
        }
    }

}
