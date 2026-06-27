using ColorHelper;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Net.NetworkInformation;
using Tools;

namespace SaveImage
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Document D = new(8, 10, 30, 2);
            //D.drawPixel(i, j, j+i, 128, 128);// colors[j % 10]);
            //D.setColorData(128, 128);
            //D.drawGrid((x, y) => x + y);
            //D.newPage();
            //D.drawGrid((x, y) => x + y);
            //D.saveFile();
            //D.dispose();
            // Draw a rectangle with the CMYK color

            var D = SaveBinAsDocument(File.ReadAllBytes("T.mp3"), 4, 30);
            D.saveFile();
            D.dispose();

        }
        public static Document SaveBinAsDocument(byte[] data, int bitCount = 8, int _GridSize = 10)
        {
            Document D = new((int)MathF.Pow(2, bitCount), _GridSize, 30, 2);
            byte[] bits = EncodeToXBit(data, bitCount);
            int pos = 0;
            D.setColorData(128, 128);
            while (pos < bits.Length)
            {
                Console.WriteLine($"{pos*3/8000} KB");
                //Console.WriteLine(bits[pos]);
                if (pos > 0 && pos % (D.GridSize[0] * D.GridSize[1]) == 0) { D.newPage(); }
                D.drawPixel((int)(pos % (D.GridSize[0] * D.GridSize[0] * Math.Sqrt(2))), bits[pos]);
                pos += 1;
                if (pos > 1500) { break; }
            }
            return D;
        }
        public static byte[] EncodeToXBit(byte[] data, int bitWidth)
        {
            if (bitWidth < 1 || bitWidth > 8)
                throw new ArgumentOutOfRangeException(nameof(bitWidth), "This snippet is optimized for 1-8 bits.");

            int mask = (1 << bitWidth) - 1;
            var result = new List<byte>((data.Length * 8 / bitWidth) + 1);

            long bitBuffer = 0;
            int bitCount = 0;

            foreach (byte b in data)
            {
                // Push byte into buffer
                bitBuffer = (bitBuffer << 8) | b;
                bitCount += 8;

                // Extract all possible chunks of bitWidth
                while (bitCount >= bitWidth)
                {
                    bitCount -= bitWidth;
                    byte val = (byte)((bitBuffer >> bitCount) & mask);
                    result.Add(val);
                }

                // Clear the bits we've already processed from the buffer
                bitBuffer &= (1L << bitCount) - 1;
            }

            // Handle remaining bits with left-aligned padding
            if (bitCount > 0)
            {
                byte val = (byte)((bitBuffer << (bitWidth - bitCount)) & mask);
                result.Add(val);
            }

            return result.ToArray();
        }
    }
    public class Document
    {
        PdfDocument document;
        XGraphics gfx;

        public int amountOfColours;
        public int[] GridSize;
        int[] Padding;
        int GridPadding;
        public double[] BlockSize;
        byte value = 255;
        byte Alpha = 255;
        int headerLines = 0;

        public void setColorData(byte _value = 255, byte _alpha = 255)
        {
            value = _value;
            Alpha = _alpha;
        }
        public int getSize()
        {
            return GridSize[0] * GridSize[1];
        }
        public void newPage()
        {
            document.AddPage();
            gfx = XGraphics.FromPdfPage(document.Pages[^1]);
            //GridSize[1] += headerLines;
            headerLines = 0;

            BlockSize = new double[] { (document.Pages[^1].Width - Padding[0] * 2) / GridSize[0] - GridPadding, (document.Pages[^1].Height - Padding[1] * 2) / GridSize[1] - GridPadding };
        }
        public Document(int ColorCount, int gridSize, int _Padding, int _GridPadding)
        {
            document = new PdfDocument();
            document.AddPage();
            gfx = XGraphics.FromPdfPage(document.Pages[^1]);
            amountOfColours = ColorCount;
            GridSize = new int[] { gridSize, (int)(gridSize * Math.Sqrt(2f))};
            Padding = new int[]{ _Padding, _Padding};
            GridPadding = _GridPadding;
            BlockSize = new double[] { (document.Pages[^1].Width - Padding[0] * 2) / (GridSize[0]) + _GridPadding, (document.Pages[^1].Height - Padding[1] * 2) / (GridSize[1]) + _GridPadding };
        }
        public Document(int ColorCount, int[] gridSize, int[] _Padding, int _GridPadding)
        {
            document = new PdfDocument();
            document.AddPage();
            gfx = XGraphics.FromPdfPage(document.Pages[^1]);
            headerLines = (int)Math.Ceiling((double)amountOfColours / gridSize[0]);
            amountOfColours = ColorCount;
            GridSize = gridSize;
            GridSize[1] += headerLines;
            Padding = _Padding;
            GridPadding = _GridPadding;

            BlockSize = new double[] { (document.Pages[^1].Width - Padding[0] * 2) / GridSize[0] - _GridPadding, (document.Pages[^1].Height - Padding[1] * 2) / GridSize[1] - _GridPadding };
        }
        

        public void drawHeader()
        {
            for (int i = 0; i < amountOfColours; i++)
            {
                drawPixel(i, -headerLines, i);
            }
            headerLines = (int)Math.Ceiling((double)amountOfColours / GridSize[0]);
        }
        public void drawPixel(int X, int Y, int ColorIndex)
        {
            XBrush brush = new XSolidBrush(XColor.FromArgb(ARGBFromHSV((ColorIndex % amountOfColours)*360/amountOfColours, 255, value, Alpha)));
            gfx.DrawRectangle(brush, Padding[0] + X*(BlockSize[0] + GridPadding), Padding[1] + (Y + headerLines)*(BlockSize[1] + GridPadding), BlockSize[0], BlockSize[1]);
        }
        public void drawPixel(int Pos, int ColorIndex)
        {
            int Y = (int)(Pos / GridSize[1]);
            int X = Pos - Y*GridSize[1];
            drawPixel(X, Y, ColorIndex);
        }
        public void drawGrid(Func<int, int, int> CIndexFunction)
        {
            for (int i = 0; i < GridSize[0]; i++)
            {
                for (int j = 0; j < GridSize[1]-headerLines; j++)
                {
                    drawPixel(i, j, CIndexFunction(i, j));
                }
            }
        }
        public void saveFile(string path = "output.pdf")
        {
            document.Save(path);
        }
        public void dispose()
        {
            document.Close();
            gfx.Dispose();
        }
        public Int32 ARGBFromHSV(int h, byte _s, byte _v, byte alpha = 255)
        {

            double r = 0, g = 0, b = 0;

            double s = _s / 255f;
            double v = _v / 255f;

            if (s == 0)
            {
                r = g = b = v;
            }
            else
            {
                double sectorPos = h / 60.0;
                int sectorNumber = (int)Math.Floor(sectorPos);
                double fractionalSector = sectorPos - sectorNumber;

                double p = v * (1.0 - s);
                double q = v * (1.0 - (s * fractionalSector));
                double t = v * (1.0 - (s * (1 - fractionalSector)));

                switch (sectorNumber % 6)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    case 5: r = v; g = p; b = q; break;
                }
            }
            return alpha << 24 | ((byte)(r * 255) << 16) | ((byte)(g * 255) << 8) | (byte)(b * 255);
        }
    }
}
