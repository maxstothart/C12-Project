using Tools;
using CT = Tools.ConsoleTools;
using OP = Tools.Operations;

namespace Base
{
    
    public struct TrainingData
    {
        private Random Rand = new();
        public int inputs;
        public int outputs;
        public List<float[]> Data = new();
        public int DataCount = 0;
        public TrainingData() { }
        public TrainingData(int _inputs, int _outputs)
        {
            inputs = _inputs;
            outputs = _outputs;
        }
        public void showData()
        {
            var output = new String[Data.Count];
            for (int i = 0; i < output.Length; i++)
            {
                foreach (float F in Data[i])
                {
                    output[i] += $"{F}, ";
                }
                output[i] = output[i][..^2];
            }
            CT.Print(output);
        }
        public TrainingData RandSubset(int DataSize)
        {
            TrainingData Output = new();
            Output.inputs = this.inputs;
            Output.outputs = this.outputs;
            for (int i = 0; i < DataSize; i++)
            {
                Output.Data.Add(this.Data[Rand.Next(0, this.Data.Count)]);
            }
            return Output;
        }
        public TrainingData Subset(int Pos, int DataSize)
        {
            TrainingData Output = new();
            Output.inputs = this.inputs;
            Output.outputs = this.outputs;
            if (Pos >= Data.Count) { Pos %= Data.Count; }
            if (DataSize >= Data.Count) { DataSize %= Data.Count; }
            Output.Data = Data.GetRange(Pos, DataSize - (int)((Pos+DataSize) - Data.Count));
            if (Pos + DataSize >= Data.Count) { Output.Data.AddRange(Data.GetRange(0, DataSize - Output.Data.Count)); }
            //Console.WriteLine(Output.Data.Count);
            return Output;

            
        }
        public float[] getPoint()
        {
            return Data[Rand.Next(Data.Count - 1)];
        }
        public void toFile(String fname)
        {
            BinaryWriter BW = new(new FileStream(fname, FileMode.Create));

            BW.Write((int)inputs);
            BW.Write((int)outputs);

            foreach (float[] line in Data)
            {
                foreach (float Val in line)
                {
                    BW.Write(Val);
                }
            }
            BW.Flush();
            BW.Close();
        }
        public static TrainingData fromFile(String fname)
        {
            TrainingData Output = new();
            BinaryReader BR = new(new FileStream(fname, FileMode.Open));
            Output.inputs = BR.ReadInt32();
            Output.outputs = BR.ReadInt32();
            while (BR.BaseStream.Position < BR.BaseStream.Length)
            {
                var Entry = new float[Output.outputs + Output.inputs];
                for (int i = 0; i < Entry.Length; i++)
                {
                    Entry[i] = BR.ReadSingle();
                }
                Output.Data.Add(Entry);
            }
            BR.Close();
            return Output;
        }

        public static TrainingData fromLCSV(LoadCSVFromFile LCSV, int inputs)
        {
            var O = new TrainingData();
            O.inputs = inputs;
            O.outputs = LCSV.Indices.Length - inputs;
            for (int i = 0; i < LCSV.Count; i++)
            {
                O.Data.Add((LCSV.GetLine(i)).Select(float.Parse).ToArray());
            }
            return O;
        }
        public List<float[]> getOrigData()
        {
            return Data[..DataCount];
        }
        public void revertToOriginal()
        {
            Data = Data[..DataCount];
        }
        public void refreshData(float Deviation, int count = 5)
        {
            if (DataCount != Data.Count && DataCount != 0) { revertToOriginal(); }
            PermutateFill(Deviation, count);
        }
        public void PermutateFill(float Deviation, int count = 5)
        {
            DataCount = Data.Count;
            for (int i = 0; i < count/DataCount; i++)
            {
                for (int k = 0; k < DataCount; k++)
                {
                    float[] val = Data[Rand.Next(DataCount)].ToArray();
                    for (int j = 0; j < inputs; j++)
                    {
                        val[j] = OP.Clamp((val[j] + (Rand.NextSingle() - 0.5f) * Deviation * 2), 0, float.PositiveInfinity);
                    }
                    Data.Add(val);
                }
            }

        }
    }
}
