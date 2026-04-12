using NAudio.Dmo;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.Collections;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Runtime.Intrinsics.X86;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Xml.Linq;
using CT = Tools.ConsoleTools;
using LCSV = Tools.LoadCSVFromFile;
using OP = Tools.Operations;
using SORT = Tools.Sort;

namespace Base
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int sel = 0;
            if (sel == 0)
            {
                Director D = new(Builder.Build(2, 3, 2));
                D.LoadData(TrainingData.fromFile("E:\\Base\\xor.dat"));

                D.FattenData(0f, 1000);
                D.TrainEvolutionary(1, 1, 1, 100000, 200, 2f, 3);

                D.N.ShowData();
                //D.TestVerbose(0.2f);

                //D.N.toFile("E:\\Base\\xor.net");
            }
            if (sel == 1)
            {
                //Director D = new(Network.fromFile("E:\\Base\\xor.net"));
                Director D = new(Builder.Build(2, 3, 2));
                D.LoadData(TrainingData.fromFile("E:\\Base\\xor.dat"));
                //D.N.ShowData();
                //D.N.Process(1, 1);
                CT.Print(D.N.ProcessCost(D.TD, 20));
                
                //D.N.ShowData();
                //D.FattenData(0.3f, 300);
                //D.TestVerbose(0.3f);
            }

        }
    }
    public static class Builder
    {
        struct Node(int ID, List<(int, float)>? _inputs)
        {
            public int NodeID = ID;
            public float Bias = 0f;
            public List<(int, float)>? inputs = _inputs;
        }
        struct Layer(List<(int, float)>? Weights)
        {
            public List<Node> Nodes = new();
            public Layer Populate(int Count, int PrevNodeID)
            {

                for (int i = 0; i < Count; i++)
                {
                    Nodes.Add(new Node(PrevNodeID + i, Weights));
                }
                return this;
            }
            public List<(int, float)> asWeights()
            {
                List<(int, float)> Output = new();
                foreach (Node N in Nodes)
                {
                    Output.Add((N.NodeID, 1));
                }
                return Output;
            }
        }

        public static Network Build(params List<int> Structure)
        {
            List<Layer> Layers = new();
            Layers.Add(new Layer(null).Populate(Structure[0], 0));
            int NodeID = Structure[0];

            foreach (int NodeCount in Structure[1..])
            {
                Layers.Add(new Layer(Layers[^1].asWeights()).Populate(NodeCount, NodeID));
                NodeID += NodeCount;
            }

            Network Data = new();
            Data.Structure = Structure;

            foreach (Layer L in Layers)
            {
                foreach (Node N in L.Nodes)
                {
                    Data.Biases.Add(N.Bias);
                    if (N.inputs == null)
                    {
                        Data.Index.Add(N.NodeID);
                        Data.Weights.Add(1f);
                    }
                    else
                    {
                        foreach (var w in N.inputs)
                        {
                            Data.Index.Add(N.NodeID);
                            Data.Weights.Add(w.Item2);
                        }
                    }
                }
            }
            return Data;
        }

    }

    public class Director(Network _N)
    {
        public Network N = _N;
        public TrainingData TD;
        private Random Rand = new();
        public void LoadData(TrainingData _TD) { TD = _TD; }
        public void LoadData(String fname) { TD = TrainingData.fromFile(fname); }
        public void FattenData(float deviation, int count)
        {
            TD.PermutateFill(deviation, count);
        }
        public void Train()
        {
            
            N.Mutate(0.4f, 7, 2).ShowData();
            N.ShowData();
        }
        public int TrainEvolutionary(int concurrentCount, int ElitePopulation, int Epochs, int maxIT = 10000, int DataDepth = 5, float Deviation = 0.4f, int breadth = 12, float root = 1)
        {
            (float, Network)[] Best = getBest(new (float, Network)[] { (N.ProcessCost(TD, DataDepth, 2, 2, 3), N) }, concurrentCount);
            int i = 1;
            for (int Ep = 0; Ep < Epochs; Ep++)
            {
                Best = getBest(Best, concurrentCount, ElitePopulation);

                float deviation = float.Pow(Deviation, OP.Clamp(float.Log10(Best[0].Item1 / 1.4f), -1.9f, 2));
                if (Best[0].Item1 == 0) { break; }
                while (i % float.Floor(maxIT / Epochs) > 0)
                {
                    ///Order of Operations
                    ///Mutate
                    ///Evaluate
                    ///Position
                    
                    var NewN = Best[i % concurrentCount].Item2.Copy().Mutate(deviation, breadth, 2);
                    NewN.ID = i;

                    var Cost = NewN.ProcessCost(TD, DataDepth, 2, 2, 2);

                    if (Cost < Best[i % concurrentCount].Item1) { Best[i % concurrentCount] = (Cost, NewN.Copy()); CT.Print($"IT: {Best[i % concurrentCount].Item2.ID} - {Best[i % concurrentCount].Item1} - {deviation}"); }
                    i++;
                }
                i++; 
                
            }
            this.N = getBest(Best, 1)[0].Item2;
            CT.Print($"{i} Iterations, Final Cost: {N.ProcessCost(TD, 200, 2, 2, 3)}");
            return i;
        }

        public static (float, Network)[] getBest((float, Network)[] input, int length, int subset=1)
        {
            (float, int)[] LowestCost = new (float, int)[subset];
            for (int i = 0; i < subset; i++) {
                LowestCost[i] = (float.PositiveInfinity, 0);
            }
            
            for (int i = 0; i < input.Length; i++)
            {
                for (int j = 0; j < subset; j++)
                {
                    if (input[i].Item1 < LowestCost[j].Item1)
                    {
                        LowestCost[j] = (input[i].Item1, i);
                        break;
                    }
                }
                 //if (input[i].Item1 < LowestCost.Item1) { LowestCost = (input[i].Item1, i);}
            }
            List<(float, Network)> output = new();
            for (int i = 0; i < length; i++)
            {
                output.Add(input[LowestCost[i%subset].Item2]);
            }
            return output.ToArray();
        }
        public static float[] getNodeCost(float[] Expected, float[] Recieved, float MultFactor = 10f, float PFactor = 2)
        {
            float[] output = new float[Expected.Length];
            for (int i = 0; i < Expected.Length; i++)
            {
                output[i] = float.Pow(float.Abs(Expected[i] - Recieved[i]) * MultFactor, PFactor);
            }
            return output;
        }
        public (bool, List<(float[], float[], bool[], bool)>) Test(float passAccuracy = 0.001f)
        {
            List<(float[], float[], bool[], bool)> Output = new();
            bool NPassed = true;
            foreach (var line in TD.Data)
            {
                float[] Expected = line[TD.inputs..];
                float[] Recieved = N.Process(line[..TD.inputs]);
                bool TPassed = true;
                var Passed = new bool[Expected.Length];

                for (int i = 0; i < Expected.Length; i++)
                {
                    Passed[i] = float.Abs(Expected[i] - Recieved[i]) <= passAccuracy;
                    if (!Passed[i]) { TPassed = false; NPassed = false; }
                }
                if (!TPassed) { Output.Add((line, Recieved, Passed.ToArray(), TPassed));  }
                
            }
            return (NPassed, Output);
        }
        public void TestVerbose(float passAccuracy = 0.001f)
        {
            var Test = this.Test(passAccuracy);
            CT.Print(Test.Item2.Select(a => $"{a.Item4} - ({CT.ToString(a.Item1)}), ({CT.ToString(a.Item2)}), ({CT.ToString(a.Item3)})").ToArray(), null, "Results: ", 5);
            CT.Print(Test.Item1);
        }
    }
    public struct Network()
    {
        private Random rand = new();
        public List<int> Index = new();
        public List<int> Structure = new();
        public List<float> Weights = new();
        public List<float> Biases = new();
        public Func<float, float> ATO = a => Sigmoid(a);
        public int ID = 0;
        public void Add(int Layer, float[] NewWeights, float Bias)
        {
            int NodeID = OP.BulkAdd(Structure[..Layer]) + Structure[Layer];
            int IndexKey = Index.IndexOf(NodeID);
            if (IndexKey == -1) { IndexKey = Index.Count; }

            for (int i = 0; i < NewWeights.Length; i++)
            {
                Index.Insert(IndexKey + i, NodeID);
                Weights.Insert(IndexKey + i, NewWeights[i]);
            }
            Biases.Insert(NodeID, Bias);

            // Advance Forward values
            for (int i = IndexKey + NewWeights.Length; i < Index.Count; i++)
            {
                Index[i] += 1;
            }

            Structure[Layer] += 1;
            //Fill Forward Weights
            if (Layer + 1 < Structure.Count)
            {
                for (int i = 1; i < Structure[Layer + 1] + 1; i++)
                {
                    Index.Insert(IndexKey + NewWeights.Length + i * Structure[Layer] - 1, NodeID + i);
                    Weights.Insert(IndexKey + NewWeights.Length + i * Structure[Layer] - 1, 0f);
                }
            }

        }

        public static float Sigmoid(float x)
        {
            return (float)(1 / (1 + Math.Pow(Math.E, -x)));
        }

        public float[] Process(params float[] inputs)
        {
            var Recieved = new float[inputs.Length];
            int CurrIndex = Structure[0];
            var Data = inputs.Concat(new float[Biases.Count - inputs.Length]).ToArray();

            int offset = 0;
            int currIndex = Structure[0];
            for (int Layer = 1; Layer < Structure.Count; Layer++)
            {
                for (int CurrLayerNode = 0; CurrLayerNode < Structure[Layer]; CurrLayerNode++)
                {
                    for (int PrevLayerNode = 0; PrevLayerNode < Structure[Layer - 1]; PrevLayerNode++)
                    {
                        Data[offset + Structure[Layer - 1] + CurrLayerNode] += Data[offset + PrevLayerNode] * Weights[CurrIndex];
                        currIndex++;
                    }
                    Data[offset + Structure[Layer - 1] + CurrLayerNode] = ATO(Data[offset + Structure[Layer - 1] + CurrLayerNode] + Biases[offset + Structure[Layer - 1] + CurrLayerNode]);
                }
                offset += Structure[Layer - 1];
            }
            return Data[^Recieved.Length..];
        }
        public float ProcessCost(TrainingData TD, int Iterations, float WeightW = 2, float BiasW = 2, float outW = 3)
        {

            float AC = 0;
            float WeightCost = 0;
            float BiasCost = 0;

            foreach (var W in Weights)
            {
                WeightCost += float.Pow(1 + float.Abs(W) / 6, WeightW);
            }
            foreach (var B in Biases)
            {
                BiasCost += float.Pow(1 + float.Abs(B) / 6, BiasW);
            }

            WeightCost /= Biases.Count;
            BiasCost /= Biases.Count;
            

            for (int IT = 1; IT < Iterations + 1; IT++)
            {
                var point = TD.getPoint();
                var inputs = point[..TD.inputs];
                var Expected = point[TD.inputs..];
                var Recieved = Process(point[..TD.inputs]);
                //CT.Print(CT.ToString(Recieved));
                float PC = 0;
                for (int i = 0; i < Recieved.Length; i++)
                {
                    PC += float.Pow(float.Abs(Expected[i] - Recieved[i]) * 10, 2);
                }
                AC += PC;
            }
            return AC;// / Iterations; //WeightCost + BiasCost 
        }
        public int GetPos(int Layer, int Position)
        {
            int output = Structure[0];
            for (int SLayer = 1; SLayer < Structure.Count; SLayer++)
            {
                for (int Node = 0; Node < Structure[SLayer]; Node++)
                {
                    if (SLayer == Layer && Node == Position) { return output; }
                    output += Structure[SLayer - 1];
                }
            }
            return output;
        }
        //Retired
        public void Sort()
        {
            List<int> newIndex = new();
            List<float> newWeights = new();

            for (int i = 0; i < Index.Count; i++)
            {
                if (i == 0 || (Index[i] != Index[i - 1] && newIndex.Contains(Index[i])))
                {
                    for (int j = 0; j < Index.Count; j++)
                    {
                        if (Index[j] == Index[i])
                        {
                            newIndex.Add(Index[j]);
                            newWeights.Add(Weights[j]);
                        }
                    }
                }
            }
            Index = newIndex;
            Weights = newWeights;
        }

        public void ShowData()
        {
            CT.Print(Weights.ToArray(), Index.ToArray(), $"Weights - {Weights.Count}");
            CT.Print(Biases.ToArray(), null, $"Biases - {Biases.Count}");
        }
        public List<int> EstimateStructure()
        {
            List<int> structure = new();
            int prevCount = 0;
            foreach (var node in OP.CountAppearances<int>(Index))
            {
                if (node.Value != prevCount)
                {
                    prevCount = node.Value;
                    structure.Add(1);
                }
                else
                {
                    structure[^1] += 1;
                }
            }
            return structure;
        }
        public void toFile(string fname)
        {
            /// File Format:
            /// I (char)
            /// Count of index values (int)
            /// B (char)
            /// Count of Bias Values (int)
            /// W (char)
            /// Count of Weight Values (int)
            /// Index values (int).....
            /// Bias values(float).......
            /// Weight Vaules (float).....
            /// EOF (String)

            //retired
            //Sort();

            BinaryWriter BW = new(new FileStream(fname, FileMode.Create));

            BW.Write((char)'S'); BW.Write((int)Structure.Count);
            BW.Write((char)'I'); BW.Write((int)Biases.Count);
            BW.Write((char)'B'); BW.Write((int)Biases.Count);
            BW.Write((char)'W'); BW.Write((int)Weights.Count);

            foreach (int s in Structure) { BW.Write((int)s); }

            int start = 0, end = 0;
            for (int i = 0; i < Biases.Count; i++)
            {
                while (end < Index.Count && start < Index.Count && Index[start] == Index[end]) { end += 1; }
                BW.Write((int)end - start);
                start = end;
            }

            foreach (float Bias in Biases) { BW.Write(Bias); }
            foreach (float W in Weights) { BW.Write(W); }

            BW.Write("EOF");
            BW.Close();
        }
        public static Network fromFile(string Fname)
        {
            /// File Format:
            /// S (char)
            /// Count of Structure Data (int)
            /// I (char)
            /// Count of index values (int)
            /// B (char)
            /// Count of Bias Values (int)
            /// W (char)
            /// Count of Weight Values (int)
            /// Structure Data (int)
            /// Index values (int).....
            /// Bias values(float).......
            /// Weight Vaules (float).....
            /// EOF (String)

            Network Output = new Network();

            BinaryReader BR = new(new FileStream(Fname, FileMode.Open));

            Dictionary<char, int> Header = new();

            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());

            for (int i = 0; i < Header['S']; i++)
            {
                Output.Structure.Add(BR.ReadInt32());
            }

            for (int i = 0; i < Header['I']; i++)
            {
                int k = BR.ReadInt32();
                for (int j = 0; j < k; j++)
                {
                    Output.Index.Add(i);
                }

            }

            for (int i = 0; i < Header['B']; i++)
            {
                Output.Biases.Add(BR.ReadSingle());
            }

            for (int i = 0; i < Header['W']; i++)
            {
                Output.Weights.Add(BR.ReadSingle());
            }

            if (BR.ReadString() != "EOF")
            {
                throw new Exception("Expected EOF, didn't find it.  Maybe the file is corrupted?");
            }

            return Output;





        }
        public Network Copy()
        {
            Network output = new();
            output.Structure = Structure.ToList();
            output.Index = Index.ToList();
            output.Weights = Weights.ToList();
            output.Biases = Biases.ToList();
            output.ID = ID;
            return output;
        }
        public Network Mutate(float deviation, int breadth, int WBRatio = 2)
        {
            Network output = Copy();
            for (int i = 0; i < breadth; i++)
            {
                if (WBRatio >= 1 && rand.Next(WBRatio + 1) == WBRatio)
                {
                    int sel = rand.Next(Biases.Count);
                    output.Biases[sel]  = output.Biases[sel]+((rand.NextSingle()-.5f)*deviation*2);
                }
                else 
                {
                    int sel = rand.Next(Weights.Count);
                    //output.Weights[sel] = OP.Clamp(output.Weights[sel]+(rand.NextSingle() - .5f) * deviation * 2, 0, float.PositiveInfinity);
                    output.Weights[sel] = output.Weights[sel] + (rand.NextSingle() - .5f) * deviation * 2;
                }
            }
            return output; 
        }
    }
    public struct TrainingData
    {
        private Random Rand = new();
        public int inputs;
        public int outputs;
        public List<float[]> Data = new();
        private int DataCount = 0;
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
        public float[] getPoint()
        {
            return Data[Rand.Next(Data.Count-1)];
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
            return Output;
        }
        public List<float[]> getOrigData()
        {
            return Data[..DataCount];
        }
        public void PermutateFill(float Deviation, int count=5)
        {
            DataCount = Data.Count;
            for (int i = 0; i < count; i++)
            {
                for (int k = 0; k < DataCount; k++)
                {
                    float[] val = Data[Rand.Next(DataCount)].ToArray();
                    for (int j = 0; j < inputs; j++)
                    {
                        val[j] = OP.Clamp((val[j] + (Rand.NextSingle()-0.5f) * Deviation*2), 0, float.PositiveInfinity);
                    }
                    Data.Add(val);
                }
            }

        }
    }
    
}
