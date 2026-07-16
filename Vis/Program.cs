using Base;
using static Vis.ShowNetwork;

namespace Vis
{
class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            Console.WriteLine("Enter FileName: ");
            string fname = Console.ReadLine();
            if (fname[0] == '_') { fname = "E:\\Base\\"+fname[1..]; }
            if (fname[^1] == '_') { fname = fname[..^1] + ".net"; }
            // This starts the UI loop and opens the window
            Application.Run(new ShowNetwork(Network.fromFile(fname), new DrawArgs(60f, .4f, 0.0015f)));
        }
    }
    public class ShowNetwork : Form
    {
        private Network N;
        private PictureBox NetworkDiagram;
        private Panel ScrollView;
        private PictureBox ScrollCanvas;
        private Splitter PanelResizer;
        private List<(float, float, int)> NodeCoordinates = new();
        private DrawArgs InputArgs;
        private float DotSize;

        

        public ShowNetwork(Network _N, DrawArgs D, int WindowWidth = 400, int WindowHeight = 800)
        {
            InputArgs = D;
            ScrollView = new Panel();
            ScrollView.Dock = DockStyle.Bottom;
            ScrollView.AutoScroll = true;
            
            
            ScrollCanvas = new PictureBox();
            ScrollCanvas.Location = new Point(0, 0);
            ScrollCanvas.Size = new Size(WindowWidth, WindowHeight);

            ScrollCanvas.Paint += new(TextPaint);
            ScrollView.Controls.Add(ScrollCanvas);
            this.Controls.Add(ScrollView);

            PanelResizer = new Splitter();
            PanelResizer.Dock = DockStyle.Bottom;
            PanelResizer.Height = 5; // Thickness of the draggable handle
            PanelResizer.BackColor = Color.DarkGray;
            PanelResizer.SplitterMoved += new((s,e) => NetworkDiagram.Refresh());

            NetworkDiagram = new PictureBox();
            NetworkDiagram.Location = new Point(0, 0);
            NetworkDiagram.Size = new Size(WindowWidth, WindowHeight/4*3); // Define your total layout space here
            NetworkDiagram.Dock = DockStyle.Fill; // Fills everything left over
            NetworkDiagram.Paint += new(PaintTop);

            this.Controls.Add(NetworkDiagram);     // Added last, fills remaining space
            this.Controls.Add(PanelResizer);   // Added second, sits directly above the panel
            this.Controls.Add(ScrollView); // Added first, locks to the absolute bottom

            this.Resize += (s, e) => { NetworkDiagram.Refresh(); ScrollCanvas.Refresh(); }; // Refresh on resize
            N = _N;
            this.Size = new Size(WindowWidth, WindowHeight/2);
            this.DoubleBuffered = true;
        }

        private void TextPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            float StepSize = 80;
            int Pos = 0;
            float XPos = 10;
            for (int i = 1; i < N.Structure.Length; i++)
            {
                int PCount = N.Structure[i - 1] * N.Structure[i];
                g.DrawString("Weights: \n" + string.Join("\n", N.Weights[Pos..(Pos + PCount)]), new Font("Arial", 10), Brushes.Black, new PointF(XPos, 10));
                Pos += PCount;
                XPos += StepSize;
            }
            
            g.DrawString("Biases: \n" + string.Join("\n", N.Biases), new Font("Arial", 10), Brushes.Black, new PointF(XPos, 10));
        }
        private void PaintTop(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            drawNetworkBetter(e, N, InputArgs);
        }

        public static void drawNode(PaintEventArgs e, Brush color, float xAsDecimal, float yAsDecimal, float DotSize)
        {
            float dotX = (e.ClipRectangle.Width * xAsDecimal) - (e.ClipRectangle.Height * DotSize) / 2;
            float dotY = (e.ClipRectangle.Height * yAsDecimal) - (e.ClipRectangle.Height * DotSize) / 2;

            e.Graphics.FillEllipse(color, dotX, dotY, (e.ClipRectangle.Height * DotSize), (e.ClipRectangle.Height * DotSize));
        }

        public static void drawLine(PaintEventArgs e, float xOrigin, float yOrigin, float xTarget, float yTarget, float LineThickness = 0.02f, int color = 1)
        {
            if (LineThickness <= 0) { return; }
            (float X, float Y) = (e.ClipRectangle.Width * xOrigin, e.ClipRectangle.Height * yOrigin);
            (float X2, float Y2) = (e.ClipRectangle.Width * xTarget, e.ClipRectangle.Height * yTarget);
            Brush Colour = new Brush[6] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Orange, Brushes.Yellow, Brushes.Brown } [color % 6];

            e.Graphics.DrawLine(new Pen(Colour, LineThickness * e.ClipRectangle.Height), X, Y, X2, Y2);
        }

        public record DrawArgs(float NodeSize, float XRatio, float WeightScalar);
        public void drawNetworkBetter(PaintEventArgs e, Base.Network N, DrawArgs D)
        {
            Brush color = Brushes.Black;
            (float x, float y) Padding = (0, 0);
            (float, float) windowSize = (1f - Padding.x * 2, 1f - Padding.y * 2);
            float xSpacing = windowSize.Item1 / (N.Structure.Length + 1);
            float xAsDecimal = Padding.x + xSpacing;
            //xSpacing = xAsDecimal * 2 * (1f - D.XRatio);


            int p = 0;
            for (int i = 0; i < N.Structure.Length; i++)
            {
                
                float yAsDecimal = Padding.y + windowSize.Item2 / (N.Structure[i] + 1);

                for (int j = 0; j < N.Structure[i]; j++)
                {
                    NodeCoordinates.Add((xAsDecimal, yAsDecimal, i));
                    yAsDecimal += windowSize.Item2 / (N.Structure[i] + 1);
                    p++;
                }
                xAsDecimal += xSpacing;
            }
            p = 0;
            for (int i = 0; i < N.Structure.Length - 1; i++)
            {
                for (int j = 0; j < N.Structure[i]; j++)
                {
                    for (int k = 0; k < N.Structure[i + 1]; k++)
                    {
                        (float, float, int) origin = NodeCoordinates[NodeCoordinates.FindIndex(n => n.Item3 == i && n.Item2 == NodeCoordinates.FindAll(n2 => n2.Item3 == i)[j].Item2)];
                        (float, float, int) target = NodeCoordinates[NodeCoordinates.FindIndex(n => n.Item3 == i + 1 && n.Item2 == NodeCoordinates.FindAll(n2 => n2.Item3 == i + 1)[k].Item2)];

                        drawLine(e, origin.Item1, origin.Item2, target.Item1, target.Item2, (float.Abs(N.Weights[p]) * D.WeightScalar), origin.Item3);
                        p++;
                    }
                }
            }
            foreach ((float, float, int) node in NodeCoordinates)
            {
                drawNode(e, color, node.Item1, node.Item2, D.NodeSize * .001f);
            }
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
