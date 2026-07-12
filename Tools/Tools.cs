using System.Linq.Expressions;
using System.Numerics;
using System.Xml;

namespace Tools
{
    /* Import Modules:
           using CT = Tools.ConsoleTools;
           using LCSV = Tools.LoadCSVFromFile; 
     */
    static public class ConsoleTools
    {
        /*
         * Usage:
         *      Single Line:
         *          CT.Print(string/int/bool);  
         *          outputs data to the Console
         *      Array/unIndexed:
         *          CT.Print(String[]/int[]/Object[]/List<Object[]>, null, "Title of Array", Indexing Spacer Maximum)
         *      Array/Indexed:
         *          CT.Print(String[]/int[]/Object[]/List<Object[]>, int[] of indexes for Data, "Title of Array", Indexing Spacer Maximum)
         *          CT.Print((int[] Indices, String[]/int[]/Object[]/List<Object[]>), "Title of Array", Indexing Spacer Maximum)
         *          The bottom method is prefered as it takes a tuple which allows for indexed data to be exported directly from a function
         *          The top is simply there to help with routing
         * 
         * 
         * 
         * 
         */
        public static void Print(string input) { Console.WriteLine(input); }
        public static void Print(int input) { Console.WriteLine(input); }
        public static void Print(bool input) { Console.WriteLine(input); }
        public static void Print(double input) { Console.WriteLine(input); }
        public static void Print(string[] input, int[]? index = null, string Name = "Array: ", int SpacerMax = 2)
        {
            if (input == null) { Console.WriteLine("Input Empty"); }
            else
            {
                String LineSpacer = "";
                String EndSpacer = "-------";
                foreach (int i in Enumerable.Range(0, SpacerMax)) { LineSpacer += " "; }
                foreach (int i in Enumerable.Range(0, Name.Length)) { EndSpacer += "-"; }

                Console.WriteLine("------ " + Name + " ------");
                foreach (int i in Enumerable.Range(0, input.Length))
                {
                    try { Console.WriteLine(((index == null) ? i : index[i]).ToString() + LineSpacer[((index == null) ? i : index[i]).ToString().Length..(SpacerMax)] + "| " + input[i]); }
                    catch (ArgumentOutOfRangeException ex) { Console.WriteLine("SpacerMax too small to show full data, try increasing it."); }
                    
                }
                Console.WriteLine(EndSpacer + "-------");
            }
        }
        public static void Print(int[] input, int[]? index = null, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Select(i => i.ToString()).ToArray(), index, Name, SpacerMax);
        }
        public static void Print(double[] input, int[]? index = null, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Select(i => i.ToString()).ToArray(), index, Name, SpacerMax);
        }
        public static void Print(float[] input, int[]? index = null, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Select(i => i.ToString()).ToArray(), index, Name, SpacerMax);
        }
        public static void Print(Object[] input, int[]? index = null, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Select(i => i.ToString()).ToArray(), index, Name, SpacerMax);
        }
        public static void Print(List<Object[]> input, int[]? index = null, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Select(i => ConsoleTools.ToString(i)).ToArray(), index, Name, SpacerMax);
        }
        public static void Print((int[] Index, Object[] Data) input, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Data, input.Index, Name, SpacerMax);
        }
        public static void Print((int[] Index, int[] Data) input, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Data, input.Index, Name, SpacerMax);
        }
        public static void Print((int[] Index, String[] Data) input, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Data, input.Index, Name, SpacerMax);
        }
        public static void Print((int[] Index, List<Object[]> Data) input, string Name = "Array: ", int SpacerMax = 2)
        {
            ConsoleTools.Print(input.Data.Select(i => ConsoleTools.ToString(i)).ToArray(), input.Index, Name, SpacerMax);
        }


        public static String ToString(params Object[] input) { return ToString(input, ", "); }
        public static String ToString(Object[] input, String Seperator)
        {
            if (input.Length == 0) { return ""; }
            String output = "";
            foreach (Object line in input)
            {
                output += line.ToString() + Seperator;
            }
            return output[0..^Seperator.Length];
        }
        public static String[] ToString(Object[][] input, String Seperator = ", ")
        {
            String[] Output = new String[input.Length];
            foreach (int i in Enumerable.Range(0, input.Length)) 
            {
                Output[i] = ToString(input[i]);
            }
            return Output;
        }
        public static String ToString(bool[] input, String Seperator = ", ")
        {
            String output = "";
            foreach (bool line in input)
            {
                output += $"{line}" + Seperator;
            }
            return output[0..^Seperator.Length];
        }
        public static String ToString<T>(T[] input, String Seperator = ", ") where T : INumber<T>
        {
            String output = "";
            foreach (T line in input)
            {
                output += $"{line}" + Seperator;
            }
            return output[0..^Seperator.Length];
        }
        public static int[] ToInt(String[] input) { return input.Select(i => Convert.ToInt32(i)).ToArray(); }
        public static int[] ToInt(Object[] input) { return input.Select(i => Convert.ToInt32(i)).ToArray(); }

        public static void EmptyLine(int n = 1) { for (int i = 0; i < n; i++) { Console.WriteLine(""); } }
        public static void TestPrint2() { Console.WriteLine("TEST"); }
        public static string toNSD<T>(T input, int toDigit) where T : INumber<T>
        {
            string Sign = ((input < T.Zero) ? "-" : "");
            string[] IAsStr = T.Abs(input).ToString().Split(".");
            if (IAsStr == null ) { return "0"; }
            if (IAsStr.Length == 1) { return Sign+IAsStr[0]; }
            if (IAsStr[1].Length == 0) { return Sign+IAsStr[0]; }
            for (int i = 0; i < IAsStr.Length; i++)
            {
                if (IAsStr[1][i] != '0' && IAsStr[1][i] != '.')
                {
                    if (i+toDigit >= IAsStr[1].Length)
                    {
                        return Sign + IAsStr[0]+IAsStr[1];
                    }
                    return Sign + IAsStr[0] + "." + IAsStr[1][..(i + toDigit)];
                }
            }
            return "0";
        }
        public static string[] toNSD<T>(IEnumerable<T> input, int toDigit) where T : INumber<T>
        {
            string[] output = new string[input.Count()];
            for (int i = 0; i < input.Count(); i++)
            {
                output[i] = toNSD<T>(input.ElementAt(i), toDigit);
            }
            return output;
        }
        public static string WithPadding(String input, String Longest)
        {
            return $"{input}{new String(' ', Longest.Length - input.Length)}";
        }
        public static string WithPadding(int input, int Longest, char? FillChar = '0')
        {
            string SI = input.ToString();
            if (FillChar.HasValue) return $"{new(FillChar.Value, Longest.ToString().Length - SI.Length)}{SI}";
            return WithPadding(SI, Longest.ToString());
        }
    }
    public class LoadCSVFromFile
    {
        /*    Example Usage:
                var Data = new LCSV("D:\\source\\Gemini Challenges\\flights.log");
                CT.Print(Data.Indices);
                CT.Print(Data.GetLine(3));
                CT.Print(CT.ToString(Data.GetLine(3)));
                CT.Print(Data.GetIndex(Data.Indices[0]));
              File Example
                #Column 1, Column 2.....Column x
                Data, Data....Data
                EOF 
              EOF signals the end.  Use Midway through file to manage data load.
        */

        public struct RowView
        {

        }
        public String[] Indices;
        public Dictionary<String, String[]> Data = new Dictionary<string, String[]>();
        public int Count;
        public LoadCSVFromFile(String fname, String IndexSeperator=", ", String DataSeperator=", ", params String[] CustomIndices)
        {
            String[] FileData = File.ReadLines(fname).ToArray();
            if (FileData.Contains("EOF")) { FileData = FileData[0..Array.IndexOf(FileData, "EOF")]; }
            while (FileData.Contains("#EOF")) { FileData = FileData[0..Array.IndexOf(FileData, "#EOF")].Concat(FileData[(Array.IndexOf(FileData, "#EOF")+1)..]).ToArray(); }
            while (FileData.Contains("")) { FileData = FileData[0..Array.IndexOf(FileData, "")].Concat(FileData[(Array.IndexOf(FileData, "") + 1)..]).ToArray(); }

            //If Indices are defined, use those names, otherwise make them numeric
            if (FileData[0][0] == '#') { Indices = FileData[0][1..].Split(IndexSeperator); FileData = FileData[1..FileData.Length]; }
            else if (CustomIndices != null) { Indices = CustomIndices; }
            else { Indices = Enumerable.Range(0, FileData[0].Split(DataSeperator).Length).Select(i => $"{i:D}").ToArray(); }

            //Generate dictionary and fill it with empty arrays
            foreach (string index in Indices) { Data.Add(index, new String[FileData.Length]); }
            for (int i = 0; i < Indices.Length; i++)
            {
                Data.TryGetValue(Indices[i], out String[] DataAtIndex);
                for (int j = 0; j < FileData.Length; j++)
                {
                    if (i >= FileData[j].Split(DataSeperator).Length) { DataAtIndex[j] = " "; }
                    else { DataAtIndex[j] = FileData[j].Split(DataSeperator)[i]; }
                }
            }
            Count = FileData.Length;
            
        }
        public LoadCSVFromFile(params LoadCSVFromFile[] MArr)
        {

            //If Indices are defined, use those names, otherwise make them numeric
            List<String> IndicesList = MArr[0].Indices.ToList();
            IndicesList.Insert(0, "Bank");
            Indices = IndicesList.ToArray();

            int length = 0;
            foreach (var lcsv in MArr) { length += lcsv.LineCount(); }
            foreach (int i in Enumerable.Range(0, Indices.Length))
            {
                Data.Add(Indices[i], new String[length]);
                List<String> Value = new List<String>();
                foreach (int j in Enumerable.Range(0, MArr.Length))
                {
                    
                    if (i == 0)
                    {
                        foreach (int k in Enumerable.Range(0, MArr[j].LineCount())) { Value.Add(j.ToString()); }
                    }
                    else
                    {
                        Value.AddRange(MArr[j].GetData(i-1).ToList());
                    }

                }
                Data[Indices[i]] = Value.ToArray();
            }



        }
        public static LoadCSVFromFile LoadFromDir(String dirName)
        {
            String[] fContains = Directory.GetFiles(dirName);
            List<LoadCSVFromFile> d = new List<LoadCSVFromFile>();
            foreach (int i in Enumerable.Range(0, fContains.Length))
            {
                d.Add(new LoadCSVFromFile(fContains[i], "", " | ", "TimeStamp", "Weight", "Code"));
                //CT.Print(d[i].GetAllLines());
            }
            return(new LoadCSVFromFile(d.ToArray()));
        }

        public void ToFile(String fname, String IndexSeperator = ", ", String DataSeperator = ", ")
        {
            List<String> Output = new List<string>();
            Output.Add($"#{ConsoleTools.ToString(Indices, IndexSeperator)}");
            foreach (String[] line in this.GetAllLines())
            {
                Output.Add(ConsoleTools.ToString(line));
            }
            File.WriteAllLines(fname, Output);
        }

        public void ToBinary(String fname, String IndexSeperator = ", ", String DataSeperator = ", ")
        {
            ///File Format
            /// "LCSV"
            /// Column count - (int)
            /// RowCount - (int)
            /// ColumnName - ()
            /// 
            BinaryWriter BW = new(new MemoryStream());
            BW.Write("LCSV");
            BW.Write((int)Indices.Length);

        }

        public String[] GetLine(int line)
        {
            String[] output = new String[Indices.Length];
            for (int i = 0; i < Indices.Length; i++)
            {
                Data.TryGetValue(Indices[i], out String[] DataAtIndex);
                output[i] = DataAtIndex[line];
            }
            return output;
        }
        public List<String[]> GetLine(int[] line)
        {
            List<String[]> output = new List<String[]>();
            for (int i = 0; i < line.Length; i++) 
            {
                output.Add(GetLine(line[i]));
            }
            return output;
        }
        public List<String[]> GetAllLines() { return this.GetLine(Enumerable.Range(0, this.LineCount()).ToArray()); }
        public String[] GetData(String IndexName)
        {
            Data.TryGetValue(IndexName, out String[] Output);
            return Output;
        }
        public String[] GetData(int Index)
        {
            Data.TryGetValue(this.Indices[Index], out String[] Output);
            return Output;
        }
        public int LineCount() { return this.GetData(0).Length; }
        public void ReOrder(int[] NewIndex)
        {
            for (int i = 0; i < this.Indices.Length; i++)
            {
                Data[Indices[i]] = Sort.ReOrder(this.GetData(i), NewIndex).Item2;
            }
        }
        public void CutLine(int line)
        {
            List<int> range = Enumerable.Range(0, LineCount()).ToList();
            range.Remove(line);
            ReOrder(range.ToArray());
        }
        public void Edit(int Line, int Column, String NewValue)
        {
            String[] x = GetData(Column);
            x[Line] = NewValue;
            Data[Indices[Column]] = x;
        }
        public void InsertLine(int index, params String[] line)
        {
            if (index < 0) { index = 0; }
            if (index > LineCount()) { index = LineCount(); }

            foreach (int i in Enumerable.Range(0,Indices.Length))
            {
                List<String> V = GetData(i).ToList();
                V.Insert(index, line[i]);
                Data[Indices[i]] = V.ToArray();
            }
        }
        public void AppendLine(params String[] Line)
        {
            InsertLine(LineCount(), Line);
        }
        public int[] Find(int Column, String target) { return Find(Indices[Column], target); }
        public int[] Find(String Column, String target)
        {
            List<int> Output = new List<int>();
            String[] D = GetData(Column);
            foreach (int i in Enumerable.Range(0, LineCount()))
            {
                //ConsoleTools.Print((String)D[i]);

                if ((String)D[i] == target)
                {
                    Output.Add(i);
                }
            }
            return Output.ToArray();
        }
        
    }
    public static class Sort
    {
        public static (int[], int[]) Bubble(int[] input)
        {
            (List<int>, List<int>) output = (new List<int>(), new List<int>());

            var data = input.Select((value, index) => new { OriginalIndex = index, Value = value });
            foreach (var item in data.OrderBy(item => item.Value).ToList()) { output.Item1.Add(item.OriginalIndex); output.Item2.Add(item.Value); }
            return (output.Item1.ToArray(), output.Item2.ToArray());
            
        }

        public static (int[], String[]) Bubble(String[] input)
        {
            (List<int>, List<String>) output = (new List<int>(), new List<String>());

            var data = input.Select((value, index) => new { OriginalIndex = index, Value = value });
            foreach (var item in data.OrderBy(item => item.Value).ToList()) { output.Item1.Add(item.OriginalIndex); output.Item2.Add(item.Value); }
            return (output.Item1.ToArray(), output.Item2.ToArray());
        }
        public static (int[], String[]) Bubble(Object[] input)
        {
            (List<int>, List<String>) output = (new List<int>(), new List<String>());

            var data = input.Select((value, index) => new { OriginalIndex = index, Value = value });
            foreach (var item in data.OrderBy(item => item.Value).ToList()) { output.Item1.Add(item.OriginalIndex); output.Item2.Add(item.Value.ToString()); }
            return (output.Item1.ToArray(), output.Item2.ToArray());
        }
        public static (int[], T[]) Bubble<T>(T[] input)
        {
            (List<int>, List<T>) output = (new(), new());

            var data = input.Select((value, index) => new { OriginalIndex = index, Value = value });
            foreach (var item in data.OrderBy(item => item.Value).ToList()) { output.Item1.Add(item.OriginalIndex); output.Item2.Add(item.Value); }
            return (output.Item1.ToArray(), output.Item2.ToArray());
        }



        public static (int[], DateTime[]) DateTimeFromISO_8601(String[] input)
        {
            (List<int>, List<DateTime>) output = (new List<int>(), new List<DateTime>());

            var data = input.Select((value, index) => new { OriginalIndex = index, Value = value });
            foreach (var item in data.OrderBy(item => item.Value).ToList()) { output.Item1.Add(item.OriginalIndex); output.Item2.Add(DateTime.Parse(item.Value)); }
            return (output.Item1.ToArray(), output.Item2.ToArray());

        }

        public static (int[], Object[]) ReOrder(Object[] input, int[] newOrder)
        {
            List<Object> Output = new List<Object>();
            for (int i = 0; i < newOrder.Length; i++)
            {
                Output.Add(input[newOrder[i]]);
            }
            return (Enumerable.Range(0, input.Length).ToArray(), Output.ToArray());
        }
        public static (int[], T[]) ReOrder<T>(T[] input, int[] newOrder)
        {
            List<T> Output = new();
            for (int i = 0; i < newOrder.Length; i++)
            {
                Output.Add(input[newOrder[i]]);
            }
            return (Enumerable.Range(0, input.Length).ToArray(), Output.ToArray());
        }
        public static (int[], List<Object[]>) ReOrder(List<Object[]> input, int[] newOrder)
        {
            List<Object[]> Output = new List<object[]>();
            for (int i = 0; i < newOrder.Length; i++)
            {
                Output.Add(input[newOrder[i]]);
            }
            return (Enumerable.Range(0, input.Count).ToArray(), Output);
        }
        public static (int index, int Value) Min(int[] input)
        {
            (int index, int Value) output = (0, input[0]);
            for (int i = 0; i < input.Length; i++) { if (input[i] < output.Value) { output = (i, input[i]); } }
            return output;
        }
        public static T Max<T>(IEnumerable<T> input) where T : INumber<T>
        {
            T Max = T.Zero;
            foreach (T i in input)
            {
                if (i > Max)
                {
                    Max = i;
                }
            }
            return Max;
        }
    }

    public static class Operations
    {
        public static T BulkAdd<T>(IEnumerable<T> input) where T : INumber<T>
        {
            T val = T.Zero;
            foreach (T x in input) { val += x; }
            return val;
        }
        public static T BulkMult<T>(IEnumerable<T> input) where T : INumber<T>
        {
            T Val = T.One;
            foreach (T x in input) { Val *= x; }
            return Val;
        }
        public static Dictionary<T, int> CountAppearances<T>(IEnumerable<T> input) where T : notnull
        {
            Dictionary<T, int> Output = new();
            foreach (T x in input)
            {
                if (Output.ContainsKey(x)) { Output[x] += 1; }
                else { Output.Add(x, 1); }
            }
            return Output;
        }
        public static T Clamp<T>(T x, T min, T max) where T : INumber<T>
        {
            
            if (x > max) { return max; }
            if (x < min) { return min; }
            return x;
        }
        public static bool Bet<T>(T min, T x, T max) where T : INumber<T>
        {
            if (x >= min && x <= max) { return true; }
            return false;
        }
        public static T[] Insert<T>(this T[] oldArray, int index, T[] newArray) where T : struct
        {
            T[] result = new T[oldArray.Length + newArray.Length];
            Array.Copy(oldArray, 0, result, 0, index);
            Array.Copy(newArray, 0, result, index, newArray.Length);
            Array.Copy(oldArray, index, result, index + newArray.Length, oldArray.Length - index);
            return result;
        }
        public static T[] Insert<T>(this T[] OldArray, int Index, T New) where T : struct
        {
            return Insert<T>(OldArray, Index, new T[] { New });
        }
        public static T[] Add<T>(this T[] OldArray, T NewData) where T : struct
        {
            return OldArray.Concat(new T[] { NewData }).ToArray();
        }
        public static bool TryGetPosition<T>(List<T> list, T item, out int position)
        {
            position = list.IndexOf(item);
            return position != -1;
        }
        public static List<T> GetLoopedRange<T>(this List<T> Data, int start, int count)
        {
            if (start >= Data.Count) { start %= Data.Count; }
            var Out = Data.GetRange(start, count - (int)(start - Data.Count));
            if (start + count >= Data.Count) { Out.AddRange(Data.GetRange(0, count - Out.Count)); }
            return Out;
        }
    }
}
