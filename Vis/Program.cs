using Base;
using Tools;
using static Vis.ShowNetwork;

namespace Vis
{
class Program
    {
        public record ProgramArgs(int WindowWidth = 800, int WindowHeight = 400 , bool Darkmode = true);

        //[STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            string fname;
            TrainingData TD;
            if (args.Length > 0)
            {
                fname = args[0];
                if (fname[0] == '_') { fname = "E:\\Base\\" + fname[1..]; }
                if (fname[^1] == '_') { fname = fname[..^1] + ".net"; }
            }
            else
            {
                Console.WriteLine("Enter Filename: ");
                fname = Console.ReadLine();
                if (fname[0] == '_') { fname = "E:\\Base\\" + fname[1..]; }
            }
            if (File.Exists(fname + ".Hdat"))
            {
                Console.WriteLine("Data Input Count");
                int inputCount = int.Parse(Console.ReadLine());
                LoadCSVFromFile L = new LoadCSVFromFile(fname + ".Hdat");
                Console.WriteLine(L.Count);
                TD = TrainingData.fromLCSV(L, inputCount);
            }
            else
            {
                Console.WriteLine("Data file not found.");
                return;
            }
            
            var PA = new ProgramArgs(400, 400, true);
            var DA = new DrawArgs(60, 0.0015f);
            var TA = new TestArgs(0.1f, 0.3f, 1000);
            Application.Run(new ShowNetwork(Network.fromFile(fname+".net"), TD, DA, TA, PA.WindowWidth, PA.WindowHeight, PA.Darkmode));
        }
    }

    public class ShowNetwork : Form
    {
        private Network N;
        private Director Dir;
        private PictureBox NetworkCanvas;
        private PictureBox BottomCanvas;
        private PictureBox RightCanvas;
        private Panel BottomPanel;
        private Panel RightPanel;
        private Splitter HorizontalHandle;
        private Splitter VerticalHandle;
        private List<(float, float, int)> NodeCoordinates = new();
        private DrawArgs InputArgs;
        private TestArgs TestArguments;
        private Pallete SColor;

        private Pallete DarkMode = new Pallete
        {
            Node = Color.White,
            Text = Color.White,
            Background = Color.FromArgb(30, 30, 30),
            DiagramBack = Color.Black,//FromArgb(50, 50, 50),
            ScrollBar = Color.Gray,
            Line = new List<Color>
            {
                Color.FromArgb(255, 255,10,0),
                Color.FromArgb(255, 20,255,0),
                Color.FromArgb(255, 0,50,255),
                Color.FromArgb(255, 0,255,0),
                Color.FromArgb(255, 128,0,128)
            },
            LineOutline = Color.White
        };
        private Pallete LightMode = new Pallete
        {
            Node = Color.Black,
            Text = Color.Black,
            Background = Color.White,
            DiagramBack = Color.White,
            ScrollBar = Color.Gray,
            Line = new List<Color>
            {
                Color.Red,
                Color.Green,
                Color.Orange,
                Color.Blue,
                Color.LightBlue
            },
            LineOutline = Color.White
        };

        private struct Pallete
        {
            public Color Node;
            public Color Text;
            public Color Background;
            public Color DiagramBack;
            public Color ScrollBar;
            public List<Color> Line;
            public Color LineOutline;
        }


        

        public ShowNetwork(Network _N, TrainingData TD, DrawArgs D, TestArgs T, int WindowWidth = 400, int WindowHeight = 800, bool Darkmode = false)
        {
            Dir = new Director(_N);
            Dir.LoadData(TD);

            TestArguments = T;
            if (Darkmode) { SColor = DarkMode; } else { SColor = LightMode; }

            this.Text = "Network Viewer";
            InputArgs = D;

            BottomPanel = new Panel();
            BottomPanel.Dock = DockStyle.Bottom;
            BottomPanel.AutoScroll = true;

            BottomCanvas = new PictureBox();
            BottomCanvas.Location = new Point(0, 0);
            BottomCanvas.Size = new Size(WindowWidth, WindowHeight);
            BottomCanvas.Paint += new(BottomPaint);
            BottomPanel.Controls.Add(BottomCanvas);
            this.Controls.Add(BottomPanel);

            RightPanel = new Panel();
            RightPanel.Dock = DockStyle.Right;
            RightPanel.AutoScroll = true;

            RightCanvas = new PictureBox();
            RightCanvas.Location = new Point(0, 0);
            RightCanvas.Size = new Size(WindowWidth, WindowHeight);
            RightCanvas.Paint += new(RightPaint);
            RightPanel.Controls.Add(RightCanvas);
            this.Controls.Add(RightPanel);

            HorizontalHandle = new Splitter();
            HorizontalHandle.Dock = DockStyle.Bottom;
            HorizontalHandle.Height = 5; // Thickness of the draggable handle
            HorizontalHandle.BackColor = SColor.ScrollBar;
            HorizontalHandle.SplitterMoved += new((s,e) => RefreshCanvases());

            VerticalHandle = new Splitter();
            VerticalHandle.Dock = DockStyle.Right;
            VerticalHandle.Width = 5; // Thickness of the draggable handle
            VerticalHandle.BackColor = SColor.ScrollBar;
            VerticalHandle.SplitterMoved += new((s, e) => RefreshCanvases());

            NetworkCanvas = new PictureBox();
            NetworkCanvas.Location = new Point(0, 0);
            NetworkCanvas.Size = new Size(WindowWidth, WindowHeight); // Define your total layout space here
            NetworkCanvas.Dock = DockStyle.Fill; // Fills everything left over
            NetworkCanvas.Paint += new(NetworkPaint);

            //Colors
            this.BackColor = SColor.Background;
            NetworkCanvas.BackColor = SColor.DiagramBack;
            BottomCanvas.BackColor = SColor.Background;
            BottomPanel.BackColor = SColor.Background;

            // Added last, fills remaining space
            this.Controls.Add(VerticalHandle);     // Added third, sits directly to the left of the panel
            this.Controls.Add(RightPanel);
            this.Controls.Add(HorizontalHandle);   // Added second, sits directly above the panel
            this.Controls.Add(BottomPanel);
            this.Controls.Add(NetworkCanvas);// Added first, locks to the absolute bottom

            this.Resize += new((s, e) => RefreshCanvases()); // Refresh on resize
            N = _N;
            this.Size = new Size(WindowWidth, WindowHeight);
            this.DoubleBuffered = true;
        }
        private void RefreshCanvases()
        {
            NetworkCanvas.Refresh();
            BottomCanvas.Refresh();
            RightCanvas.Refresh();
        }
        private void NetworkPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            drawNetworkBetter(e, N, InputArgs);
        }
        public record TestArgs(float Accuracy, float Deviation, float Count);
        private void RightPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            String[] Results = Dir.TestVerbose(TestArguments.Accuracy, (TestArguments.Deviation, (int)TestArguments.Count), false);
            //sDir.TestVerbose()
            int YPos = 10;

            foreach (var line in Results)
            {
                g.DrawString(line, new Font("Arial", 10), new SolidBrush(SColor.Text), new PointF(10, YPos));
                YPos += 20;
            }
        }
        private void BottomPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            float StepSize = 80;
            int Pos = 0;
            float XPos = 10;
            for (int i = 1; i < N.Structure.Length; i++)
            {
                int PCount = N.Structure[i - 1] * N.Structure[i];
                g.DrawString($"L{i}_Weights: \n\n" + string.Join("\n", N.Weights[Pos..(Pos + PCount)]), new Font("Arial", 10), new SolidBrush(SColor.Text), new PointF(XPos, 10));
                Pos += PCount;
                XPos += StepSize;
            }
            
            g.DrawString("Biases: \n\n" + string.Join("\n", N.Biases), new Font("Arial", 10), new SolidBrush(SColor.Text), new PointF(XPos, 10));
        }
        

        public void drawNode(PaintEventArgs e, float xAsDecimal, float yAsDecimal, float DotSize)
        {
            float dotX = (e.ClipRectangle.Width * xAsDecimal) - (GetScalar(e) * DotSize) / 2;
            float dotY = (GetScalar(e) * yAsDecimal) - (GetScalar(e) * DotSize) / 2;

            e.Graphics.FillEllipse(new SolidBrush(SColor.Node), dotX, dotY, (GetScalar(e) * DotSize), (GetScalar(e) * DotSize));
        }

        public void drawLine(PaintEventArgs e, float xOrigin, float yOrigin, float xTarget, float yTarget, float LineThickness = 0.02f, float OutlineThickness = 0.01f, int color = 1)
        {
            if (LineThickness <= 0) { return; }
            (float X, float Y) = (e.ClipRectangle.Width * xOrigin, GetScalar(e) * yOrigin);
            (float X2, float Y2) = (e.ClipRectangle.Width * xTarget, GetScalar(e) * yTarget);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawLine(new Pen(SColor.LineOutline, LineThickness * GetScalar(e)), X, Y, X2, Y2);
            e.Graphics.DrawLine(new Pen(SColor.Line[color % SColor.Line.Count], LineThickness * GetScalar(e) - OutlineThickness), X, Y, X2, Y2);
        }

        public record DrawArgs(float NodeSize, float WeightScalar);
        public void drawNetworkBetter(PaintEventArgs e, Base.Network N, DrawArgs D)
        {
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

                        drawLine(e, origin.Item1, origin.Item2, target.Item1, target.Item2, (float.Abs(N.Weights[p]) * D.WeightScalar), 0.01f, origin.Item3);
                        p++;
                    }
                }
            }
            foreach ((float, float, int) node in NodeCoordinates)
            {
                drawNode(e, node.Item1, node.Item2, D.NodeSize * .001f);
            }
        }

        public static int GetScalar(PaintEventArgs e)
        {
            return (e.ClipRectangle.Width < e.ClipRectangle.Height) ? e.ClipRectangle.Width : e.ClipRectangle.Height;
        }

        public void showWindowSize(PaintEventArgs e) { showWindowSize(e, (0f, 0f)); }
        public void showWindowSize(PaintEventArgs e, (float, float) padding)
        {
            var color = Brushes.Red;
            float DotSize = 0.02f;

            float x = e.ClipRectangle.Width * (0f + padding.Item1);
            float y = GetScalar(e) * (0f + padding.Item2);

            e.Graphics.DrawRectangle(new Pen(Brushes.Black, e.ClipRectangle.Width * (DotSize / 1.2f)), x, y, e.ClipRectangle.Width - x * 2, GetScalar(e) - y * 2);

            drawNode(e, 0f + padding.Item1, 0f + padding.Item2, DotSize);
            drawNode(e, 1f - padding.Item1, 0f + padding.Item2, DotSize);
            drawNode(e, 0f + padding.Item1, 1f - padding.Item2, DotSize);
            drawNode(e, 1f - padding.Item1, 1f - padding.Item2, DotSize);
        }
    }

}
