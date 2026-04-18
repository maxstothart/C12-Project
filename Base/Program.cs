using NAudio.CoreAudioApi;
using System.Data;
using System.Diagnostics;
using CT = Tools.ConsoleTools;
using OP = Tools.Operations;

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
                Director D = new(Builder.Build(2, 3, 4, 2));
                D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));

                D.FattenData(0.3f, 200);
                D.TrainEvolutionary(
                    concurrentCount: 100,
                    threads: 20,
                    ElitePopulation: 10,
                    EpochsPerMillion: 16,
                    accuracy: -10,
                    DataDepth: 300,
                    maxIT: 4000000,
                    Deviation: 3f,
                    breadth: 7,
                    Par: new()
                    {
                        WeightW = 1,
                        BiasW = 1,
                        outW = 3,
                        MultFactor = 10f,
                        PFactor = 3f,
                        WBCutOff = 0.01f
                    },
                    Verbose: true,
                    shock: true
                );
                D.N.ShowData();
                D.TestVerbose(0.2f);
                D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                D.FattenData(0.3f, 1000);
                D.TestVerbose(0.3f);

                D.N.toFile(DataDir + "xor.net");
            }
            if (false)
            {
                Director D = new(Builder.Build(2, 3, 4, 2));
                D.LoadData(DataDir + "xor.dat");

                D.EvolutionStaggerTrain(
                    origin: 0.2f,
                    destination: 0.4f, 
                    attempts: 6, 
                    concurrentCount: 100,
                    threads: 20,
                    ElitePopulation: 10,
                    EpochsPerMillion: 16,
                    accuracy: -20,
                    DataDepth: 200,
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
                Director D = new(Builder.Build(2, 3, 4, 2));
                D.LoadData(TrainingData.fromFile(DataDir + "xor.dat"));
                D.FattenData(0.3f, 300);
                D.TestVerbose(0.1f);
            }
            
        }
        
    }
}
