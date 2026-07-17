using Base;
using IniParser;
using IniParser.Model;
using static Tools.LoadCSVFromFile;
using static Vis.ShowNetwork;

namespace Vis
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.Parser.Parse(File.ReadAllText(AppContext.BaseDirectory+"Config.ini"));

            Application.EnableVisualStyles();

            var PA = new ProgramArgs(int.Parse(data["Window"]["Width"]), int.Parse(data["Window"]["Height"]), int.Parse(data["Window"]["RightPanelWidth"]), int.Parse(data["Window"]["BottomPanelHeight"]), bool.Parse(data["Style"]["ShowWeights"]));
            var DA = new DrawArgs(float.Parse(data["Model"]["NodeSize"]), float.Parse(data["Model"]["WeightScalar"]));
            var TA = new TestArgs(float.Parse(data["Test"]["Accuracy"]), float.Parse(data["Test"]["Deviation"]), int.Parse(data["Test"]["Count"]));


            Application.Run(new ShowNetwork(DA, TA, PA, bool.Parse(data["Style"]["DarkMode"]) ? ShowNetwork.DarkMode : ShowNetwork.LightMode, args));
        }
    }

    public class ShowNetwork : Form
    {
        private Network? N = null;
        private TrainingData? TD = null;
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
        private ProgramArgs WindowSettings;

        private String[] TestResults;
        private Pallete SColor;

        public struct Pallete
        {
            public Color Node;
            public Color Text;
            public Color Background;
            public Color DiagramBack;
            public Color ScrollBar;
            public List<Color> Line;
            public Color LineOutline;
            public Font TextFont;
        };

        public static Pallete DarkMode = new Pallete
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
            LineOutline = Color.White,
            TextFont = new Font("Cascadia Code", 10)
        };

        public static Pallete LightMode = new Pallete
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
            LineOutline = Color.White,
            TextFont = new Font("Cascadia Code", 10)
        };

        

        public record ProgramArgs(int WindowWidth, int WindowHeight, int RightPanelWidth, int BottomPanelHeight, bool ShowWeights = true);
        public record DrawArgs(float NodeSize, float WeightScalar);
        public record TestArgs(float Accuracy, float Deviation, float Count);


        public ShowNetwork(DrawArgs D, TestArgs T, ProgramArgs PA, Pallete _Pallete, String[]? Payload = null)
        {
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(50,50);

            SColor = _Pallete;
            TestArguments = T;
            WindowSettings = PA;

            this.Text = "Network Viewer";
            InputArgs = D;



            BottomPanel = new Panel();
            BottomPanel.Dock = DockStyle.Bottom;
            BottomPanel.AutoScroll = true;
            BottomPanel.Height = PA.BottomPanelHeight;

            BottomCanvas = new PictureBox();
            BottomCanvas.Location = new Point(0, 0);
            BottomCanvas.Size = new Size(PA.WindowWidth, PA.WindowHeight);
            BottomCanvas.Paint += new(BottomPaint);
            BottomPanel.Controls.Add(BottomCanvas);
            this.Controls.Add(BottomPanel);

            RightPanel = new Panel();
            RightPanel.Dock = DockStyle.Right;
            RightPanel.AutoScroll = true;
            RightPanel.Scroll += (s, e) => RightPanel.Refresh();
            RightPanel.Width = 0;

            RightCanvas = new PictureBox();
            RightCanvas.Location = new Point(0, 0);
            RightCanvas.Size = new Size(PA.WindowWidth, PA.WindowHeight);
            RightCanvas.Paint += new(RightPaint);
            RightPanel.Controls.Add(RightCanvas);
            this.Controls.Add(RightPanel);

            HorizontalHandle = new Splitter();
            HorizontalHandle.Dock = DockStyle.Bottom;
            HorizontalHandle.Height = 5; // Thickness of the draggable handle
            HorizontalHandle.BackColor = SColor.ScrollBar;
            HorizontalHandle.SplitterMoved += new((s, e) => RefreshCanvases());

            VerticalHandle = new Splitter();
            VerticalHandle.Dock = DockStyle.Right;
            VerticalHandle.Width = 5; // Thickness of the draggable handle
            VerticalHandle.BackColor = SColor.ScrollBar;
            VerticalHandle.SplitterMoved += new((s, e) => RefreshCanvases());

            NetworkCanvas = new PictureBox();
            NetworkCanvas.Location = new Point(0, 0);
            NetworkCanvas.Size = new Size(PA.WindowWidth, PA.WindowHeight); // Define your total layout space here
            NetworkCanvas.Dock = DockStyle.Fill; // Fills everything left over
            NetworkCanvas.Paint += new(NetworkPaint);

            NetworkCanvas.AllowDrop = true;
            NetworkCanvas.DragEnter += FileDragEnter;
            NetworkCanvas.DragDrop += FileDragDrop;

            //Colors
            this.BackColor = SColor.DiagramBack;
            NetworkCanvas.BackColor = SColor.DiagramBack;
            BottomCanvas.BackColor = SColor.Background;
            BottomPanel.BackColor = SColor.Background;
            RightCanvas.BackColor = SColor.Background;
            RightPanel.BackColor = SColor.Background;

            // Added last, fills remaining space
            this.Controls.Add(VerticalHandle);     // Added third, sits directly to the left of the panel
            this.Controls.Add(RightPanel);
            this.Controls.Add(HorizontalHandle);   // Added second, sits directly above the panel
            this.Controls.Add(BottomPanel);
            this.Controls.Add(NetworkCanvas);// Added first, locks to the absolute bottom

            this.Resize += new((s, e) => RefreshCanvases()); // Refresh on resize
            this.ClientSize = new Size(PA.WindowWidth-PA.RightPanelWidth, PA.WindowHeight);
            this.DoubleBuffered = true;

            if (!PA.ShowWeights)
            {
                BottomPanel.Height = 0;
                this.Height -= PA.BottomPanelHeight;
            }
            ProcessPayload(Payload);
        }

        public void ProcessPayload(String[]? Payload)
        {
            if (Payload == null) return;
            foreach (string file in Payload)
            {
                if (File.Exists(file))
                {
                    if (file.EndsWith(".net"))
                    {
                        N = Network.fromFile(file);
                        TestUpdate();
                        if (File.Exists(file[..^4]+".Hdat"))
                        {
                            ProcessPayload(new string[] { $"{file[..^4]}.Hdat" });
                        }
                        if (File.Exists(file[..^4] + ".dat"))
                        {
                            ProcessPayload(new string[] { $"{file[..^4]}.dat" });
                        }
                    }
                    else if (file.EndsWith(".Hdat"))
                    {
                        TD = TrainingData.fromLCSV(new Tools.LoadCSVFromFile(file));
                        TestUpdate();
                    }
                    else if (file.EndsWith(".dat"))
                    {
                        TD = TrainingData.fromFile(file);
                        TestUpdate();
                    }
                }
            }
        }

        private void FileDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data!.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void FileDragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            ProcessPayload(files);
            RefreshCanvases();
        }

        private void TestUpdate()
        {
            if (N == null || TD == null) { return; }
            Director Dir = new Director(N.Value);
            Dir.LoadData(TD.Value);
            TestResults = Dir.TestVerbose(TestArguments.Accuracy, (TestArguments.Deviation, (int)TestArguments.Count), false);
            if (RightPanel.Width == 0) { RightPanel.Width = WindowSettings.RightPanelWidth; this.Width += WindowSettings.RightPanelWidth; }
        }
        private void RefreshCanvases()
        {
            
            BottomCanvas.Refresh();
            RightCanvas.Refresh();
            NetworkCanvas.Refresh();
        }


        private void NetworkPaint(object sender, PaintEventArgs e)
        {
            if (N != null)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                drawNetworkBetter(e, N.Value, InputArgs);
            }
        }
        private void RightPaint(object sender, PaintEventArgs e)
        {
            if (TD == null)
            {
                RightPanel.Width = 0;
            }
            //if (TestResults == null) { TestUpdate(); }
            if (N != null && TD != null && TestResults != null)
            {
                Graphics g = e.Graphics;
                RightCanvas.Height = (int)(TestResults.Length * SColor.TextFont.Height + 20);
                RightCanvas.Width = (int)(TestResults.Max(x => x.Length-8) * SColor.TextFont.Size);
                if (RightPanel.Width >= RightCanvas.Width) { RightPanel.Width = RightCanvas.Width; }
                if (TD.Value.inputs == N.Value.Structure[0] && TD.Value.outputs == N.Value.Structure[^1])
                {

                    int YPos = 10;

                    foreach (var line in TestResults)
                    {
                        g.DrawString(line, SColor.TextFont, new SolidBrush(SColor.Text), new PointF(10, YPos));
                        YPos += SColor.TextFont.Height;
                    }
                }
                else
                {
                    g.DrawString($"Training Data does not match Network Structure.\nTD Inputs: {TD.Value.inputs}, TD Outputs: {TD.Value.outputs}\nNetwork Inputs: {N.Value.Structure[0]}, Network Outputs: {N.Value.Structure[^1]}", SColor.TextFont, new SolidBrush(SColor.Text), new PointF(10, 10));
                }
            }
        }
        private void BottomPaint(object sender, PaintEventArgs e)
        {
            if (N != null)
            {
                Graphics g = e.Graphics;
                float StepSize = SColor.TextFont.Size * 12;
                int Pos = 0;
                float XPos = 10;
                int[] WeightCounts = N.Value.Structure.Zip(N.Value.Structure.Skip(1), (a, b) => a * b).ToArray();
                BottomCanvas.Height = (int)((WeightCounts.Max()+5) * SColor.TextFont.Height);
                BottomCanvas.Width = (int)(12 * SColor.TextFont.Size * N.Value.Structure.Length);
                if (BottomPanel.Height >= BottomCanvas.Height) { BottomPanel.Height = BottomCanvas.Height; }

                for (int i = 1; i < N.Value.Structure.Length; i++)
                {
                    int PCount = N.Value.Structure[i - 1] * N.Value.Structure[i];
                    g.DrawString($"L{i}_Weights: \n\n" + string.Join("\n", N.Value.Weights[Pos..(Pos + PCount)]), SColor.TextFont, new SolidBrush(SColor.Text), new PointF(XPos, 10));
                    Pos += PCount;
                    XPos += StepSize;
                }

                g.DrawString("Biases: \n\n" + string.Join("\n", N.Value.Biases), SColor.TextFont, new SolidBrush(SColor.Text), new PointF(XPos, 10));
            }

        }

        
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
