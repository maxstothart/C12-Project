using System.Threading.Channels;
using Tools;

namespace NEAT
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("test");
        }
        
    }
    public class NEATNet
    {
        private Random rand = new();
        List<Node> nodes = new();
        List<Connection> Connections = new();
        public enum NodeMode
        {
            Input,
            Output,
            Hidden
        }
        public NEATNet(params int[] Structure)
        {
            int NID = 0;
            for (int i = 0; i < Structure[0]; i++)
            {
                nodes.Add(new Node(NID, NodeMode.Input, 1f));
                NID++;
            }
            foreach (var item in Structure[1..^1])
            {
                for (int i = 0; i < item; i++)
                {
                    nodes.Add(new Node(NID, NodeMode.Hidden, 1f));
                    NID++;
                }
            }
            for (int i = 0; i < Structure[^1]; i++)
            {
                nodes.Add(new Node(NID, NodeMode.Output, 1f));
                NID++;
            }
        }
        public record Node(int ID, NodeMode mode, float Bias);
        public record Connection(int InputNodeID, int OutputNodeID, float Weight, bool Enabled, int Iteration);

        public NEATNet Reproduce(NEATNet P2)
        {
            NEATNet Output = new(0, 0, 0);
            Output.nodes = nodes;
            Output.Connections = Connections;
            for (int i = 0; i < P2.Connections.Count; i++)
            {
                int Position;
                if (Operations.TryGetPosition(Output.Connections, P2.Connections[i], out Position)) 
                {
                    if (rand.Next(0, 1) == 1)
                    {
                        Output.Connections[Position] = P2.Connections[i];
                    }
                }
                else
                {
                    Output.Connections.Add(P2.Connections[i]);
                }
            }
            return Output;
        }

        public void Mutate(float WeightDrift)
        {
            switch (rand.Next(0, 2))
            {
                case 0:
                    Connections[rand.Next(0, Connections.Count)].Weight += rand.NextSingle() * WeightDrift;
            }
        }



    }
}
