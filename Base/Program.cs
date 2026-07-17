using System.Diagnostics;
using Tools;

namespace Base
{
    internal class Program
    {
        static void Main(string[] args)
        {
            String DataDir = "/mnt/e/Base/";
            DataDir = "E:\\Base\\Data\\";

            if (false)
            {
                Director D = new(Network.fromFile(DataDir + "xor.net"));
                D.N.ShowData();
            }

            if (false)
            {
                Director D = new(Builder.Build(2, 5, 4, 3, 2));
                var TD = TrainingData.fromLCSV(new LoadCSVFromFile(DataDir + "xor.Hdat", DataSeperator: " "));
                D.LoadData(TD);
                D.TrainBackProp(100, 0.1f);
            }
            if (false)
            {
                Director D = new(Builder.Build(6, 10, 8, 4));
                var TD = TrainingData.fromLCSV(new Tools.LoadCSVFromFile(DataDir + "3BAdder.Hdat", ", ", " "));
                D.LoadData(TD);

                D.FattenData(0.0001f, 300);

                D.TrainEvolutionary(
                    concurrentCount: 40,
                    threads: 20,
                    ElitePopulation: 8,
                    EliteDuplication: 3,
                    EpochsPerMillion: 20,
                    accuracy: -25,
                    DataDepth: 80,
                    maxIT: 5_000_000,
                    Deviation: 4f,
                    breadth: 7,
                    Par: new()
                    {
                        WeightW = 1,
                        BiasW = 1,
                        outW = 3,
                        MultFactor = 10f,
                        PFactor = 5f,
                        WBCutOff = float.PositiveInfinity
                    },
                    Verbose: 0,
                    shock: false
                );
                //Console.WriteLine("Worst Accuracy"D.Test(0.3f).Item2);
                //D.N.ShowData();

                D.TestVerbose(0.4f);
                //D.RoundToNearest(0.1f);
                //D.N.ShowData();
                //D.TestVerbose(0.1f);

                //D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                //D.FattenData(0.3f, 10000);


                D.N.toFile(DataDir + "xor.net");
            }
            if (false)
            {
                Director D = new(Builder.Build(8, 16, 8, 5));
                var TD = TrainingData.fromLCSV(new Tools.LoadCSVFromFile(DataDir + "4badder.Hdat", ",", ","));
                D.LoadData(TD);

                D.FattenData(0.0001f, 300);

                D.TrainEvolutionary(
                    concurrentCount: 80,
                    threads: 20,
                    ElitePopulation: 8,
                    EliteDuplication: 16,
                    EpochsPerMillion: 10,
                    accuracy: -25,
                    DataDepth: 200,
                    maxIT: 20_000_000,
                    Deviation: 3f,
                    breadth: 5,
                    Par: new()
                    {
                        WeightW = 1,
                        BiasW = 1,
                        outW = 3,
                        MultFactor = 10f,
                        PFactor = 5f,
                        WBCutOff = float.PositiveInfinity
                    },
                    Verbose: 0,
                    shock: false
                );
                //Console.WriteLine("Worst Accuracy"D.Test(0.3f).Item2);
                //D.N.ShowData();

                D.TestVerbose(0.3f);
                //D.RoundToNearest(0.1f);
                //D.N.ShowData();
                //D.TestVerbose(0.1f);

                //D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                //D.FattenData(0.3f, 10000);


                D.N.toFile(DataDir + "xor.net");
            }
            if (false)
            {
                //Director D = new(Builder.Build(3, 6, 4, 1));
                Director D = new(Network.fromFile(DataDir + "3BParity.net"));
                var TD = TrainingData.fromLCSV(new Tools.LoadCSVFromFile(DataDir + "3bitparity.Hdat", ", ", " "));
                D.LoadData(TD);

                D.FattenData(0.3f, 400);
                D.TrainEvolutionary(
                    concurrentCount: 20,
                    threads: 20,
                    ElitePopulation: 5,
                    EliteDuplication: 2,
                    EpochsPerMillion: 40,
                    accuracy: -25,
                    DataDepth: 200,
                    maxIT: 2000000,
                    Deviation: 2f,
                    breadth: 5,
                    Par: new()
                    {
                        WeightW = 1,
                        BiasW = 1,
                        outW = 3,
                        MultFactor = 10f,
                        PFactor = 5f,
                        WBCutOff = float.PositiveInfinity
                    },
                    Verbose: 0,
                    shock: false
                );
                D.TestVerbose(0.1f, (0.3f, 1200), true);
                //D.RoundToNearest(0.1f);
                //D.N.ShowData();
                //D.TestVerbose(0.1f);

                //D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                //D.FattenData(0.3f, 10000);


                D.N.toFile(DataDir + "3BParity.net");
            }
            if (true)
            {
                Director D = new(Builder.Build(2, 4, 4, 2));
                //Director D = new(Network.fromFile(DataDir + "xor.net"));
                D.LoadData(TrainingData.fromLCSV(new LoadCSVFromFile(DataDir + "xor.Hdat", ", ", " ")));


                D.FattenData(0.3f, 800);
                D.TrainEvolutionary(
                    concurrentCount: 20,
                    threads: 20,
                    ElitePopulation: 2,
                    EliteDuplication: 10,
                    EpochsPerMillion: 40,
                    accuracy: -20,
                    DataDepth: 20,
                    maxIT: 2000000,
                    Deviation: 3f,
                    breadth: 5,
                    Par: new()
                    {
                        WeightW = 5,
                        BiasW = 5,
                        outW = 1,
                        MultFactor = 10f,
                        PFactor = 5f,
                        WBCutOff = 1
                    },
                    Verbose: 0,
                    shock: false
                );


                //Compare(D, 2, 0, "xor", D => D.N.removeNode(2,0));
                Compare(D, 2, 0, "xor", D => D.N.AddNode(2));
                //D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                //D.FattenData(0.3f, 10000);



            }
            if (false)
            {
                Director D = new(Builder.Build(2, 3, 4, 2));
                D.LoadData(DataDir + "xor.dat");

                D.TrainStaggerEvolutionary(
                    origin: 0f,
                    destination: 0.3f,
                    attempts: 6,
                    DataCount: 1200,
                    concurrentCount: 100,
                    threads: 20,
                    ElitePopulation: 40,
                    EliteDuplication: 80,
                    EpochsPerMillion: 16,
                    accuracy: -20,
                    DataDepth: 300,
                    maxIT: 2000000,
                    Deviation: 3f,
                    breadth: 7,
                    Par: new()
                    {
                        WeightW = 1,
                        BiasW = 1,
                        outW = 5,
                        MultFactor = 10f,
                        PFactor = 2f,
                        WBCutOff = 0.01f
                    },
                    Verbose: 0,
                    shock: false
                );
                D.N.toFile(DataDir + "xor.net");
            }

            if (false)
            {
                Director D = new Director(Builder.Build(2, 3, 4, 2));
                D.N.toFile(DataDir + "xor.net");
                D.N.ShowData();
                Director D2 = new Director(Network.fromFile(DataDir + "xor.net"));
                D2.N.ShowData();
            }
            void Compare(Director D, int Layer, int Node, string NetName, Action<Director> Payload)
            {
                ProcessStartInfo PSI = new(
                    fileName: "E:\\Vis\\bin\\Release\\net9.0-windows\\Vis.exe",
                    arguments: $"{DataDir + NetName + ".Hdat"} {DataDir + NetName + ".net"}"
                );
                D.N.toFile(DataDir + $"{NetName}.net");
                var Viewer = Process.Start(PSI);
                //Viewer.WaitForExit();
                Payload(D);
                D.N.toFile(DataDir + $"{NetName}2.net");
                PSI.Arguments = $"{DataDir + NetName + ".Hdat"} {DataDir + NetName + "2.net"}";
                Process.Start(PSI);
            }
        }
        
    }
}
