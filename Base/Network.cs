using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using CT = Tools.ConsoleTools;
using OP = Tools.Operations;
namespace Base
{
    public struct Network()
    {
        private Random rand = new();

        public int[] Index;
        public int[] Structure;
        public float[] Weights;
        public float[] Biases;
        public int ScalingFactor = 1;

        public Func<float, float> ATO = a => LeakyReLU(a);// Sigmoid(a);// ReLU(a);
        public Func<float, float> OutputATO = a => Sigmoid(a);
        public int ID = 0;
        public void Add(int Layer, float[]? NewWeights=null, float Bias=0)
        {
            int NodeID = OP.BulkAdd(Structure[..Layer]) + Structure[Layer];
            int IndexKey = Array.IndexOf(Index, NodeID);
            if (IndexKey == -1) { IndexKey = Index.Length; }

            if (NewWeights == null)
            {
                NewWeights = new float[Structure[Layer - 1]];
                for (int i = 0; i < NewWeights.Length; i++)
                {
                    NewWeights[i] = 1;
                }
            }

            int[] NIndex = new int[NewWeights.Length];
            for (int i = 0; i < NewWeights.Length; i++)
            {
                NIndex[i] = NodeID;
            }
            Index = OP.Insert(Index, IndexKey, NIndex).ToArray();
            Weights = OP.Insert(Weights, IndexKey, NewWeights).ToArray();
            Biases = OP.Insert(Biases, NodeID, Bias);

            //Index = Index[..IndexKey].Concat(NIndex.Concat(Index[IndexKey..])).ToArray();
            //Weights = Weights[..IndexKey].Concat(NewWeights.Concat(Weights[IndexKey..])).ToArray();
            //Biases = Biases[..NodeID].Concat(new float[] { Bias }.Concat(Biases[NodeID..])).ToArray();

            // Advance Forward values
            for (int i = IndexKey + NewWeights.Length; i < Index.Length; i++)
            {
                Index[i] += 1;
            }

            Structure[Layer] += 1;
            //Fill Forward Weights
            if (Layer + 1 < Structure.Length)
            {
                for (int i = 1; i < Structure[Layer + 1] + 1; i++)
                {
                    
                    OP.Insert(Index, IndexKey + NewWeights.Length + i * Structure[Layer] - 1, NodeID + i);
                    OP.Insert(Weights, IndexKey + NewWeights.Length + i * Structure[Layer] - 1, 0f);
                }
            }

        }

        public static float Sigmoid(float x)
        {
            return (float)(1 / (1 + Math.Pow(Math.E, -x)));
        }
        public static float HyperbolicTan(float x)
        {
            return float.Tanh(x);
        }
        public static float ReLU(float x)
        {
            return (x > 0) ? x : 0;
        }
        public static float LeakyReLU(float x)
        {
            float negativeWeight = 0.01f;
            return (x > 0) ? x : x * negativeWeight;
        }

        public float[] Process(params float[] inputs)
        {
            var Data = inputs.Concat(new float[Biases.Length - inputs.Length]).ToArray();
            int currIndex = inputs.Length;
            int POffset = 0;
            int COffset = inputs.Length;

            for (int Layer = 1; Layer < Structure.Length; Layer++)
            {
                for (int NodeInLayer = 0; NodeInLayer < Structure[Layer]; NodeInLayer++)
                {
                    for (int NodeInPrevLayer = 0; NodeInPrevLayer < Structure[Layer - 1]; NodeInPrevLayer++)
                    {
                        Data[COffset + NodeInLayer] += Data[POffset + NodeInPrevLayer] * Weights[currIndex];
                        currIndex++;
                    }
                    if (Layer+1 == Structure.Length) { Data[COffset + NodeInLayer] = Sigmoid(Data[COffset + NodeInLayer] + Biases[COffset + NodeInLayer]); }
                    else { Data[COffset + NodeInLayer] = OutputATO(Data[COffset + NodeInLayer] + Biases[COffset + NodeInLayer]); }
                        
                }
                POffset = COffset;
                COffset += Structure[Layer];
            }
            return Data[^Structure[^1]..];
        }
        public struct PCParams()
        {
            public float WeightW = 2;
            public float BiasW = 2;
            public float outW = 3;
            public float MultFactor = 10f;
            public float PFactor = 2f;
            public float WBCutOff = 10f;
        }
        
        public float ProcessCost(TrainingData TD, PCParams P)
        {
            float BCost = 0;
            float WCost = 0;
            float OCost = 0;

            foreach (float B in Biases) { BCost += float.Pow(float.Abs(B), 2); }
            foreach (float W in Weights) { WCost += float.Pow(float.Abs(W), 2); }
            foreach (var point in TD.Data)
            {
                var inputs = point[..TD.inputs];
                var Expected = point[TD.inputs..];
                var Recieved = Process(inputs);

                float PC = 0;
                for (int i = 0; i < Expected.Length; i++)
                {
                    PC += float.Pow(float.Abs(Expected[i] - Recieved[i]) * P.MultFactor, P.PFactor);
                }
                //if (PC > (OCost / it) * 1.5) { PC *= 1.5f; }
                OCost += PC;
            }

            BCost /= Biases.Length;
            WCost /= Weights.Length;
            //OCost /= Iterations;

            BCost *= P.BiasW;
            WCost *= P.WeightW;
            OCost *= P.outW;

            if (OCost < P.WBCutOff*P.outW) { return OCost; }

            return OCost + OP.Clamp(BCost + WCost, 0, float.PositiveInfinity);
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
        public void ShowData()
        {
            CT.Print(Weights.ToArray(), Index.ToArray(), $"Weights - {Weights.Length}");
            CT.Print(Biases.ToArray(), null, $"Biases - {Biases.Length}");
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
            /// S (char)
            /// Count of Structure Data (int)
            /// I (char)
            /// Count of index values (int)
            /// B (char)
            /// Count of Bias Values (int)
            /// W (char)
            /// Count of Weight Values (int)
            /// F (char)
            /// Scaling Factor of Values(int)
            /// Structure Data (int)
            /// Index values (int).....
            /// Bias values(float).......
            /// Weight Vaules (float).....
            /// EOF (String)

            //retired
            //Sort();

            BinaryWriter BW = new(new FileStream(fname, FileMode.Create));

            BW.Write((char)'S'); BW.Write((int)Structure.Length);
            BW.Write((char)'I'); BW.Write((int)Biases.Length);
            BW.Write((char)'B'); BW.Write((int)Biases.Length);
            BW.Write((char)'W'); BW.Write((int)Weights.Length);
            BW.Write((char)'F'); BW.Write((int)ScalingFactor);

            foreach (int s in Structure) { BW.Write((int)s); }

            int start = 0, end = 0;
            for (int i = 0; i < Biases.Length; i++)
            {
                while (end < Index.Length && start < Index.Length && Index[start] == Index[end]) { end += 1; }
                BW.Write((int)end - start);
                start = end;
            }

            foreach (float Bias in Biases) { BW.Write(Bias/ScalingFactor); }
            foreach (float W in Weights) { BW.Write(W/ScalingFactor); }

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
            /// F (char)
            /// Scaling Factor of Values(float)
            /// Structure Data (int)
            /// Index values (int).....
            /// Bias values(float).......
            /// Weight Vaules (float).....
            /// EOF (String)

            Network Output = new Network();

            List<int> Index = new();
            List<int> Structure = new();
            List<float> Weights = new();
            List<float> Biases = new();

            BinaryReader BR = new(new FileStream(Fname, FileMode.Open));

            Dictionary<char, int> Header = new();

            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());

            for (int i = 0; i < Header['S']; i++)
            {
                //Console.WriteLine(BR.ReadInt32());
                Structure.Add(BR.ReadInt32());
            }

            for (int i = 0; i < Header['I']; i++)
            {
                int k = BR.ReadInt32();
                for (int j = 0; j < k; j++)
                {
                    Index.Add(i);
                }

            }

            for (int i = 0; i < Header['B']; i++)
            {
                Biases.Add(BR.ReadSingle() * Header['F']);
            }

            for (int i = 0; i < Header['W']; i++)
            {
                Weights.Add(BR.ReadSingle() * Header['F']);
            }

            if (BR.ReadString() != "EOF")
            {
                throw new Exception("Expected EOF, didn't find it.  Maybe the file is corrupted?");
            }

            Output.Structure = Structure.ToArray();
            Output.Biases = Biases.ToArray();
            Output.Weights = Weights.ToArray();
            Output.Index = Index.ToArray();

            BR.Close();
            return Output;





        }
        public Network Copy(int newID)
        {
            Network output = new();
            output.Structure = (int[])Structure.Clone();
            output.Index = (int[])Index.Clone();
            output.Weights = (float[])Weights.Clone();
            output.Biases = (float[])Biases.Clone();
            output.ID = newID;
            return output;
        }
        public Network Copy() { return Copy(ID); }
        public Network Mutate(float deviation, int breadth, int WBRatio = 2)
        {
            Network output = Copy();
            for (int i = 0; i < breadth; i++)
            {
                if (WBRatio >= 1 && rand.Next(WBRatio + 1) == WBRatio)
                {
                    int sel = rand.Next(Biases.Length);
                    output.Biases[sel] = output.Biases[sel] + ((rand.NextSingle() - .5f) * deviation * 2);
                }
                else
                {
                    int sel = rand.Next(Weights.Length);
                    //output.Weights[sel] = OP.Clamp(output.Weights[sel]+(rand.NextSingle() - .5f) * deviation * 2, 0, float.PositiveInfinity);
                    output.Weights[sel] = output.Weights[sel] + (rand.NextSingle() - .5f) * deviation * 2;
                }
            }
            return output;
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
            List<int> Index = new();
            List<float> Weights = new();
            List<float> Biases = new();
            Data.Structure = Structure.ToArray();

            foreach (Layer L in Layers)
            {
                foreach (Node N in L.Nodes)
                {
                    Biases.Add(N.Bias);
                    if (N.inputs == null)
                    {
                        Index.Add(N.NodeID);
                        Weights.Add(1f);
                    }
                    else
                    {
                        foreach (var w in N.inputs)
                        {
                            Index.Add(N.NodeID);
                            Weights.Add(w.Item2);
                        }
                    }
                }
            }
            Data.Index = Index.ToArray();
            Data.Weights = Weights.ToArray();
            Data.Biases = Biases.ToArray();
            return Data;
        }
    }
}

