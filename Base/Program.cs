using NAudio.Dmo;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices.Swift;
using System.Security.AccessControl;
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
            Network N = Builder.Build(2, 3, 2);

            if (false)
            {
                N.ShowData();
                //CT.Print(N.EstimateStructure().ToArray());
                N.Add(1, new float[] { 2f, 2f }, 1f);
                N.ShowData();

                N.toFile("E:\\Vis\\Data\\Network.dat");

                Network Recieved = Network.fromFile("E:\\Vis\\Data\\Network.dat");
                CT.Print(Recieved.EstimateStructure().ToArray());
            }
            if (true)
            {
                Director D = new(N);


                D.Train(TrainingData.fromFile("E:\\Base\\xor.dat"));



                CT.Print(D.N.Process(1f, 1f), null, "Results: ");
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

    public class Director
    {
        public Network N;
        public TrainingData TD;
        public Director(Network _N)
        {
            N = _N;
        }
        public void Train(TrainingData TD)
        {
            CT.Print(TD.Data.Count);
        }
    }
    public struct Network()
    {
        public List<int> Index = new();
        public List<int> Structure = new();
        public List<float> Weights = new();
        public List<float> Biases = new();
        public Func<float, float> ATO = a => Sigmoid(a);
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
            return (float)(1 / (1 - Math.Pow(Math.E, -x)));
        }

        public float[] Process(params List<float> inputs)
        {
            var Data = new float[Biases.Count];
            for (int i = 0; i < inputs.Count; i++)
            {
                Data[i] = inputs[i];
            }
            Data = Data.Zip(Biases, (a, b) => a + b).ToArray();

            Queue<float> W = new(Weights);

            int CurrIndex = Structure[0];
            for (int i = 1; i < Structure.Count; i++)
            {
                for (int k = 0; k < Structure[i]; k++)
                {
                    for (int j = 0; j < Structure[i - 1]; j++)
                    {
                        Data[CurrIndex + k] += Data[(CurrIndex - Structure[i - 1]) + j] * W.Dequeue();
                    }
                    Data[CurrIndex + k] = ATO(Data[CurrIndex + k]);
                }
                CurrIndex += Structure[i];
            }
            return Data[^(Structure[^1])..];
        }
        public (int, int) GetPos(int NodeID)
        {
            int layer = 0;
            while (OP.BulkAdd(Structure[..layer]) - 1 < NodeID)
            {
                layer += 1;
            }
            return (layer, NodeID - OP.BulkAdd(Structure[..layer]));
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

            foreach (int s in Structure) { BW.Write(s); }

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
    }
    public struct TrainingData
    {
        public int inputs;
        public int outputs;
        public List<float[]> Data = new();
        public TrainingData() { }
        public TrainingData(int _inputs, int _outputs)
        {
            inputs = _inputs;
            outputs = _outputs;
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
    }
    
}
