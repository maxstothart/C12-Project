using System.Collections;
using System.ComponentModel.Design;
using System.Security.AccessControl;
using CT = Tools.ConsoleTools;
using SORT = Tools.Sort;
using System.Diagnostics;

namespace Base
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Network N = Builder.Build(2,3,2);

            N.ShowData();
            N.toFile("E:\\Vis\\Data\\Network.dat");

            Network Recieved = Network.fromFile("E:\\Vis\\Data\\Network.dat");
            Recieved.ShowData();

            //Network.ShowNetwork();
            //Network.ShowConnections();
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

            foreach (Layer L in Layers)
            {
                foreach (Node N in L.Nodes)
                {
                    Data.Biases.Add(N.Bias);
                    if (N.inputs == null)
                    {
                        Data.Add(N.NodeID, 1f);
                    }
                    else
                    {
                        foreach (var w in N.inputs)
                        {
                            Data.Add(N.NodeID, w.Item2);
                        }
                    }
                }
            }
            return Data;
        }
        
    }
    public struct Network()
    {
        public List<int> Index = new();
        public List<float> Weights = new();
        public List<float> Biases = new();

        public void Add(int NodeID, float Weight)
        {
            Index.Add(NodeID);
            Weights.Add(Weight);
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
            CT.Print(Weights.ToArray(), Index.ToArray(), "Weights");
            CT.Print(Biases.ToArray(), null, "Biases");
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


            BW.Write((char)'I'); BW.Write((int)Biases.Count);
            BW.Write((char)'B'); BW.Write((int)Biases.Count);
            BW.Write((char)'W'); BW.Write((int)Weights.Count);

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

            Network Output = new Network();

            BinaryReader BR = new(new FileStream(Fname, FileMode.Open));

            Dictionary<char, int> Header = new();

            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());
            Header.Add(BR.ReadChar(), BR.ReadInt32());

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
}
