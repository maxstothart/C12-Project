using System.Diagnostics;

namespace Base
{
    internal class Program
    {
        static void Main(string[] args)
        {
            String DataDir = "/mnt/e/Base/";
            DataDir = "E:\\Base\\";


            if (true)
            {
                Director D = new(Builder.Build(3, 5, 5, 3, 1));
                var TD = TrainingData.fromLCSV(new Tools.LoadCSVFromFile(DataDir + "3bitparity.Hdat", ", ", " "), 3);
                D.LoadData(TD);

                D.FattenData(0.3f, 800);
                D.TrainEvolutionary(
                    concurrentCount: 20,
                    threads: 20,
                    ElitePopulation: 4,
                    EpochsPerMillion: 40,
                    accuracy: -25,
                    DataDepth: 200,
                    maxIT: 2000000,
                    Deviation: 3f,
                    breadth: 7,
                    Par: new()
                    {
                        WeightW = 3,
                        BiasW = 3,
                        outW = 1,
                        MultFactor = 10f,
                        PFactor = 5f,
                        WBCutOff = 0.01f
                    },
                    Verbose: false,
                    shock: true
                );
                Console.WriteLine(D.Test(0.3f).Item2);
                //D.N.ShowData();

                //D.TestVerbose(0.1f);
                //D.RoundToNearest(0.1f);
                //D.N.ShowData();
                //D.TestVerbose(0.1f);

                //D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                //D.FattenData(0.3f, 10000);


                D.N.toFile(DataDir + "xor.net");
            }
            if (false)
            {
                Director D = new(Builder.Build(2, 3, 4, 2));
                //Director D = new(Network.fromFile(DataDir + "xor.net"));
                D.LoadData(TrainingData.fromLCSV(new Tools.LoadCSVFromFile(DataDir + "xor.Hdat", ", ", " "), 2));

                D.FattenData(0.3f, 800);
                D.TrainEvolutionary(
                    concurrentCount: 20,
                    threads: 20,
                    ElitePopulation: 4,
                    EpochsPerMillion: 40,
                    accuracy: -25,
                    DataDepth: 10,
                    maxIT: 2000000,
                    Deviation: 3f,
                    breadth: 5,
                    Par: new()
                    {
                        WeightW = 3,
                        BiasW = 3,
                        outW = 1,
                        MultFactor = 10f,
                        PFactor = 5f,
                        WBCutOff = 0.01f
                    },
                    Verbose: false,
                    shock: false
                );
                Console.WriteLine(D.Test(0.3f).Item2);
                //D.N.ShowData();
               
                //D.TestVerbose(0.1f);
                //D.RoundToNearest(0.1f);
                //D.N.ShowData();
                //D.TestVerbose(0.1f);
                
                //D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                //D.FattenData(0.3f, 10000);
                

                D.N.toFile(DataDir + "xor.net");
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
                    Verbose: true,
                    shock: false
                );
                D.N.toFile(DataDir + "xor.net");
            }
            if (false)
            {
                Director D = new(Network.fromFile(DataDir+"xor.net"));
                D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                D.RoundToNearest(5);
                D.N.ShowData();
                D.TestVerbose(0.1f);
                //D.EvolutionStaggerTrain(
                //    origin: 0.2f,
                //    destination: 0.4f,
                //    attempts: 6,
                //    concurrentCount: 100,
                //    threads: 20,
                //    ElitePopulation: 10,
                //    EpochsPerMillion: 16,
                //    accuracy: -20,
                //    DataDepth: 200,
                //    maxIT: 2000000,
                //    Deviation: 3f,
                //    breadth: 7,
                //    Par: new()
                //    {
                //        WeightW = 1,
                //        BiasW = 1,
                //        outW = 5,
                //        MultFactor = 10f,
                //        PFactor = 2f,
                //        WBCutOff = 0.01f
                //    },
                //    Verbose: true,
                //    shock: false
                //);
                //D.TestVerbose(0.1f);
            }
            if (false)
            {
                Director D = new Director(Builder.Build(2, 3, 4, 2));
                D.N.toFile(DataDir + "xor.net");
                D.N.ShowData();
                Director D2 = new Director(Network.fromFile(DataDir + "xor.net"));
                D2.N.ShowData();
            }
            
        }
        
    }
}
