using System.Diagnostics;
using System.Text;
using Tools;
using CT = Tools.ConsoleTools;
using OP = Tools.Operations;

namespace Base
{
    public class Director
    {
        public Network N;
        public TrainingData TD;
        public TrainingData OldData;
        private Random Rand = new();
        public Director(Network _N)
        {
            N = _N;
        }
        public Director() { }
        
        public void LoadData(TrainingData _TD) { TD = _TD; OldData = _TD; }
        public void LoadData(String fname) { TD = TrainingData.fromFile(fname); OldData = TD; }
        public void FattenData(float deviation, int count) { TD = OldData; TD.PermutateFill(deviation, count); }

        public record ProcessResult(float[][] WeightedSums, float[][] PreActivationValues, float[] Outputs);
        public int TrainBackProp(int MaxIterations, float LearnRate)
        {
            //Written in Github, not error checked
            Func<float, float> SigmoidPrime = (a) => MathF.FusedMultiplyAdd(a, -a, a);
            for (int i = 0; i < MaxIterations; i++)
            {
                var point = TD.getPoint();
                float[] NetOut = N.ProcessOutput(point[N.Structure[0]]);
                var Error = new float[N.Structure[^1]];

                List<float[]> UnitError = new();

                // OutputUnitError
                UnitError.Add(new float[N.Structure[^1]]);
                //Calculate Error
                for (int j = 0; j < Error.Length; j++) { UnitError[^1][j] = NetOut[j] * (1 - NetOut[j]) * (point[N.Structure[0] + i] - NetOut[j]); }

                for (int layer = N.Structure.Length - 1; i >= 0; i--)
                {
                    UnitError.Add(new float[N.Structure[layer]]);
                    for (int j = 0; j < Error.Length; j++) { UnitError[^1][j] = NetOut[j] * (1 - NetOut[j]) * (point[N.Structure[0] + i] - NetOut[j]); }
                }
            }
            return 0;
        }
        public record NetworkResult(Network N, float Cost);
        public record MutateParameters(float Deviation, int Breadth, int WBRatio);
        public int TrainEv2(int CountPerThread, int Threads, int Epochs, int IterationsPerEpoch, MutateParameters MPar, Network.PCParams CostParams)
        {


            ///Every thread has it's own island
            ///A number of that island's population is replaced per epoch with elite
            ///Scale the replacement based on the cost of the best network in the island
            ///
            //Initialise Networks
            var Networks = new (Network N, float Cost)[CountPerThread, Threads];
            for (int i = 0; i < CountPerThread; i++)
            {
                for (int j = 0; j < Threads; j++)
                {
                    Networks[i, j] = (N.Copy(), float.PositiveInfinity);
                }
            }

            var IttPerEpPerThread = IterationsPerEpoch / Threads;

            for (int epoch = 0; epoch < Epochs; epoch++) {
                _ = Parallel.For(0, Threads, thread =>
                {
                    for (int i = 0; i < IttPerEpPerThread; i++) 
                    {
                        int CurrIndex = Rand.Next(CountPerThread);
                        Networks[CurrIndex, thread].N.Mutate(MPar.Deviation, MPar.Breadth, MPar.WBRatio);
                        Networks[CurrIndex, thread].Cost = N.ProcessCost(TD, CostParams);
                    }
                });
            }return 0;
        }
        public int TrainEvolutionary(int concurrentCount, int threads, int ElitePopulation, int EliteDuplication, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, int Verbose = 2, bool shock = false)
        {
            int Epochs = EpochsPerMillion * (maxIT / 1000000);
            if (Epochs == 0) { Epochs = 1; }
            var timer = Stopwatch.StartNew();

            int i = 1;
            concurrentCount += concurrentCount % threads;
            int processesPerEpoch = maxIT / Epochs / threads;
            int networksPerThread = concurrentCount / threads;
            int OldID = 0;
            var PrevScore = float.PositiveInfinity;
            var ET = Stopwatch.StartNew();

            var Networks = new NetworkResult[1];
            Networks[0] = new(N.Copy(), float.PositiveInfinity);

            NetworkResult[] Best = GetPopulation(Networks, concurrentCount, 1, concurrentCount);
            Networks = (NetworkResult[])Best.Clone();


            for (int Ep = 0; Ep < Epochs; Ep++)
            {
                Best = GetPopulation(getBest(Networks, true, n => n.ProcessCost(TD, Par)), concurrentCount, ElitePopulation, EliteDuplication).ToArray();
                Networks = (NetworkResult[])Best.Clone();
                float deviation = float.Pow(Deviation, OP.Clamp(float.Log10(Best[0].Cost), -1f, 2));
                //breadth = Math.Max(1, (int)(50 * deviation / 9f));

                if (Verbose >= 1) { Networks[0].N.ShowData(); }
                CT.Print($"EPOCH {CT.WithPadding(Ep, Epochs)} | {CT.WithPadding(i, maxIT, '0')} | Elapsed: {timer.Elapsed} / {ET.Elapsed} | Cost: {Best[0].Cost}");
                ET.Restart();
                PrevScore = Best[0].Cost;
                TrainingData TDRand = TD.RandSubset(TD.Data.Count);

                if (float.Abs(Best[0].Cost) - float.Pow(10, accuracy) <= 0 || Best[0].Cost == 0) { break; }
                _ = Parallel.For(0, threads, thread =>
                {
                    TrainingData TDSubset = TDRand.Subset((DataDepth * thread), DataDepth);
                    for (int j = 0; j < processesPerEpoch; j++)
                    {

                        int CIndex = i;
                        int position = thread * networksPerThread + i % networksPerThread;
                        var NewN = Networks[position].N.Copy(CIndex).Mutate(deviation, breadth, 2);
                        var Cost = NewN.ProcessCost(TDSubset, Par);
                        if (Cost < Networks[position].Cost)// || Rand.NextSingle() < 0.02f)
                        {
                            Networks[position] = new NetworkResult(NewN.Copy(), Cost);
                            _ = Interlocked.Exchange(ref OldID, i);
                            if (Verbose >= 2) { CT.Print($"IT: {Networks[position].N.ID} - {Networks[position].Cost} - {deviation}"); }
                        }
                        Interlocked.Increment(ref i);
                    }
                    Interlocked.Increment(ref i);
                });
            }
            timer.Stop();
            this.N = GetPopulation(getBest(Best, true, n => n.ProcessCost(TD, Par)), 1, 1, 1).ElementAt(0).N;

            CT.Print($"\n{i} Iterations, Final Cost: {N.ProcessCost(TD, Par)}, Time Ellapsed: {timer.Elapsed.ToString()}");
            return i;
        }

        public void TrainStaggerEvolutionary(float origin, float destination, int attempts, int DataCount, int concurrentCount, int threads, int ElitePopulation, int EliteDuplication, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, int Verbose = 2, bool shock = false)
        {
            float increment = (destination - origin) / attempts;
            for (int i = 0; i < attempts; i++)
            {
                CT.Print(origin + increment * i);
                TD.refreshData(origin + increment * i, DataCount);
                TrainEvolutionary(concurrentCount, threads, ElitePopulation, EliteDuplication, EpochsPerMillion, accuracy, maxIT, DataDepth, Deviation, breadth, Par, Verbose, shock);
            }
        }
        public void RoundToNearest(float x)
        {
            float scalingFactor = 1 / x;
            for (int i = 0; i < N.Weights.Length; i++)
            {
                N.Weights[i] = MathF.Round(N.Weights[i] * scalingFactor) / scalingFactor;
            }
            for (int i = 0; i < N.Biases.Length; i++)
            {
                N.Biases[i] = MathF.Round(N.Biases[i] * scalingFactor) / scalingFactor;
            }
        }

        public void GlobalDiv(int x)
        {
            for (int i = 0; i < N.Weights.Length; i++)
            {
                N.Weights[i] /= x;
            }
            for (int i = 0; i < N.Biases.Length; i++)
            {
                N.Biases[i] /= x;
            }
            N.ScalingFactor = x;
        }

        public NetworkResult[] GetPopulation(NetworkResult[] orderedInput, int outputLength, int eliteCount, int EliteReproduction)
        {
            //MIKE:
            var EliteResults = orderedInput.Take(eliteCount);
            var MinClonesPerElite = EliteReproduction;
            var EliteCloneArmy = EliteResults.SelectMany(r => Enumerable.Repeat(new NetworkResult(r.N.Copy(), r.Cost), MinClonesPerElite)).ToArray();
            var ArmySize = EliteCloneArmy.Length;

            if (outputLength < ArmySize)
                return EliteCloneArmy[..outputLength];

            var output = new List<NetworkResult>(EliteCloneArmy);

            for (int i = ArmySize; i < outputLength; i++)
            {
                var r = Rand.Next(orderedInput.Length);
                output.Add(new NetworkResult(orderedInput[r].N.Copy(), orderedInput[r].Cost));
            }

            return output.ToArray();


            // MAX:
            //List<NetworkResult> output = new();
            //if (eliteCount >= orderedInput.Length) { eliteCount = orderedInput.Length; }
            //
            //for (int i = 0; i < eliteCount; i++)
            //{
            //    for (int j = 0; j < EliteReproduction; j++)
            //    {
            //        output.Add(new(orderedInput[i].N.Copy(), orderedInput[i].Cost));
            //    }
            //}
            //for (int i = eliteCount * EliteReproduction; i < outputLength; i++)
            //{
            //    int index = eliteCount + Rand.Next(orderedInput.Count() - eliteCount);
            //    output.Add(new(orderedInput[index].N.Copy(), orderedInput[index].Cost));
            //}
            //
            //return output.ToArray();
        }

        public NetworkResult[] getBest(
            NetworkResult[] input,
            bool recheck = false,
            Func<Network, float>? CostFunction = null)
        {
            NetworkResult[] LowestCost = input.OrderBy(r => r.Cost).ToArray();
            return LowestCost;
        }

        public IEnumerable<NetworkResult> GetPopulation(
            IEnumerable<NetworkResult> input,
            int length,
            int subset = 1,
            int subsetDuplication = 1,
            bool recheck = false,
            Func<Network, float>? CostFunction = null)
        {
            var T = Stopwatch.StartNew();
            List<NetworkResult> output = new();
            for (int i = 0; i < subset; i++)
            {
                for (int j = 0; j < subsetDuplication; j++)
                {
                    output.Add(input.ElementAt(i));
                }
            }
            for (int i = 0; i < length - subset*subsetDuplication; i++)
            {
                output.Add(input.ElementAt((subset + i)%input.Count()));
            }
            T.Stop();
            return output;
        }

        public (bool Pass, float Accuracy, List<(float[] Expected, float[] Recieved, float[] Distance, bool Passed)> DataPoints) Test(float passAccuracy = 0.001f)
        {
            List<(float[], float[], float[], bool)> Output = new();
            bool NPassed = true;
            float largestDrift = 0;
            foreach (var line in TD.Data)
            {
                float[] Expected = line[TD.inputs..];
                float[] Recieved = N.ProcessOutput(line[..TD.inputs]);
                float[] Distance = new float[Expected.Length];
                bool TPassed = true;

                for (int i = 0; i < Expected.Length; i++)
                {
                    float DistanceFromExpected = float.Abs(Expected[i] - Recieved[i]);
                    Distance[i] = DistanceFromExpected - passAccuracy;
                    if (DistanceFromExpected > largestDrift) { largestDrift = DistanceFromExpected; }

                    if (!(DistanceFromExpected <= passAccuracy)) { TPassed = false; NPassed = false; }
                    //CT.Print($"{Expected[i]}, {Recieved[i]}, {DistanceFromExpected}, {Distance[i]}"); 

                }
                if (!TPassed) { Output.Add((line, Recieved, Distance, TPassed)); }
            }
            return (NPassed, largestDrift, Output);
        }
        public String[] TestVerbose(float passAccuracy = 0.001f, (float Accuracy, float Count)? DataPerm = null, bool print = true)
        {
            List<string> Output = new();
            if (DataPerm != null) { TD.PermutateFill(DataPerm.Value.Accuracy, (int)DataPerm.Value.Count); }
            var Test = this.Test(passAccuracy);

            Output.AddRange(CT.Print(Test.Item3.Select(a => $"{a.Item4} - ({CT.ToString(CT.toNSD(a.Item1, 2))}), ({CT.ToString(CT.toNSD(a.Item2, 2))}), ({CT.ToString(CT.toNSD(a.Item3, 2))})").ToArray(), null, "Results: (I1, I2....O1, O2....), (Recieved), (Distance)", 5, false));
            Output.Add("________Final Results_______:");
            Output.Add("Worst Accuracy | " + Test.Item2);
            Output.Add("Test Passed    | " + Test.Item1);

            if (print)
            {
                foreach (var line in Output)
                {
                    Console.WriteLine(line);
                }
            }
            return Output.ToArray();
        }
    }
    public static class DirectorExtensions
    {

    }

}
