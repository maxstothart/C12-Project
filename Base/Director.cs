using ILGPU.IR.Values;
using System.Diagnostics;
using CT = Tools.ConsoleTools;
using OP = Tools.Operations;

namespace Base
{
    public class Director(Network _N)
    {
        public Network N = _N;
        public TrainingData TD;
        public TrainingData OldData;
        private Random Rand = new();
        public void LoadData(TrainingData _TD) { TD = _TD; OldData = _TD; }
        public void LoadData(String fname) { TD = TrainingData.fromFile(fname); OldData = TD; }
        public void FattenData(float deviation, int count) { TD = OldData; TD.PermutateFill(deviation, count); }

        public int TrainBackProp(int MaxIterations, float LearnRate)
        {
            for (int i = 0; i < MaxIterations; i++)
            {
                var point = TD.getPoint();
                float[] NetOut = N.Process(point[N.Structure[0]]);
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
                Debugger.Break();
            }
            return 0;
        }
        public int TrainEvolutionary(int concurrentCount, int threads, int ElitePopulation, int EliteEnd, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, int Verbose = 2, bool shock = false)
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

            (float, Network)[] Best = getBest(new (float, Network)[] { (float.PositiveInfinity, N) }, concurrentCount);
            (float, Network)[] Networks = ((float, Network)[])Best.Clone();


            for (int Ep = 0; Ep < Epochs; Ep++)
            {
                Best = getBest(Networks, concurrentCount, ElitePopulation, EliteEnd, true, n => n.ProcessCost(TD, Par));
                Networks = ((float, Network)[])Best.Clone();
                float deviation = float.Pow(Deviation, OP.Clamp(float.Log10(Best[0].Item1), -1f, 2));
                //breadth = Math.Max(1, (int)(50 * deviation / 9f));

                if (Verbose >= 1) { Networks[0].Item2.ShowData(); }
                CT.Print($"EPOCH {CT.WithPadding(Ep, Epochs)} | {CT.WithPadding(i, maxIT, '0')} | Elapsed: {timer.Elapsed} / {ET.Elapsed} | Cost: {Best[0].Item1}");
                ET.Restart();
                PrevScore = Best[0].Item1;
                TrainingData TDRand = TD.RandSubset(TD.Data.Count);

                if (float.Abs(Best[0].Item1) - float.Pow(10, accuracy) <= 0 || Best[0].Item1 == 0) { break; }
                _ = Parallel.For(0, threads, thread =>
                {
                    TrainingData TDSubset = TDRand.Subset((DataDepth * thread), DataDepth);
                    for (int j = 0; j < processesPerEpoch; j++)
                    {

                        int CIndex = i;
                        int position = thread * networksPerThread + i % networksPerThread;
                        var NewN = Networks[position].Item2.Copy(CIndex).Mutate(deviation, breadth, 2);
                        var Cost = NewN.ProcessCost(TDSubset, Par);
                        if (Cost < Networks[position].Item1)// || Rand.NextSingle() < 0.02f)
                        {
                            Networks[position] = (Cost, NewN.Copy());
                            _ = Interlocked.Exchange(ref OldID, i);
                            if (Verbose >= 2) { CT.Print($"IT: {Networks[position].Item2.ID} - {Networks[position].Item1} - {deviation}"); }
                        }
                        Interlocked.Increment(ref i);
                    }
                    Interlocked.Increment(ref i);
                });
            }
            timer.Stop();
            this.N = getBest(Best, 1, 1, 1, true, n => n.ProcessCost(TD, Par))[0].Item2;

            CT.Print($"\n{i} Iterations, Final Cost: {N.ProcessCost(TD, Par)}, Time Ellapsed: {timer.Elapsed.ToString()}");
            return i;
        }

        public void TrainStaggerEvolutionary(float origin, float destination, int attempts, int DataCount, int concurrentCount, int threads, int ElitePopulation, int EliteEnd, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, int Verbose = 2, bool shock = false)
        {
            float increment = (destination - origin) / attempts;
            for (int i = 0; i < attempts; i++)
            {
                CT.Print(origin + increment * i);
                TD.refreshData(origin + increment * i, DataCount);
                TrainEvolutionary(concurrentCount, threads, ElitePopulation, EliteEnd, EpochsPerMillion, accuracy, maxIT, DataDepth, Deviation, breadth, Par, Verbose, shock);
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


        public (float, Network)[] getBest(
            (float, Network)[] input,
            int length,
            int subset = 1,
            int subsetEnd = 0,
            bool recheck = false,
        Func<Network, float>? CostFunction = null)
            {
                (float, int)[] LowestCost = new (float, int)[length];

                for (int i = 0; i < LowestCost.Length; i++)
                {
                    LowestCost[i] = (float.PositiveInfinity, 0);
                }

                for (int i = 0; i < input.Length; i++)
                {
                    float trueCost = input[i].Item1;   //Input Cost per network

                    if (recheck)
                    {
                        trueCost = CostFunction(input[i].Item2); //set cost via cost function(recheck)
                    }

                    for (int j = 0; j < LowestCost.Length; j++) //For size of resulting array
                    {
                        if (trueCost < LowestCost[j].Item1) //If better network found than best network found so far
                        {
                            for (int k = LowestCost.Length - 1; k > j; k--) //Shift everything down
                            {
                                LowestCost[k] = LowestCost[k - 1];
                            }

                            LowestCost[j] = (trueCost, i); //Add Network to Top?
                            break;
                        }
                    }
                }

            Debugger.Break();

                List<(float, Network)> output = new();

                for (int i = 0; i < subsetEnd; i++)
                {
                    output.Add(input[LowestCost[i % subset].Item2]);
                }

                int remaining = length - output.Count;

                for (int i = 0; i < remaining; i++)
                {
                    output.Add(input[LowestCost[subset + (i % (LowestCost.Length - subset))].Item2]);
                }

                return output.ToArray();
            }

        //public (float, Network)[] getBest((float, Network)[] input, int length, int subset = 1, int subsetEnd = 1, bool recheck = false, Func<Network, float>? CostFunction = null)
        //{
        //    var T = Stopwatch.StartNew();
        //    int remainingCount = length - subsetEnd;
        //    int requiredBest = subset + remainingCount;
        //
        //    (float, int)[] LowestCost = new (float, int)[length];
        //    //(float, int)[] LowestCost = new (float, int)[subset];
        //    for (int i = 0; i < LowestCost.Length; i++)
        //    {
        //        LowestCost[i] = (float.PositiveInfinity, 0);
        //    }
        //
        //    for (int i = 0; i < input.Length; i++)
        //    {
        //        float trueCost = input[i].Item1;
        //        if (recheck) trueCost = CostFunction(input[i].Item2);
        //
        //        for (int j = 0; j < LowestCost.Length; j++)
        //        {
        //            if (trueCost < LowestCost[j].Item1)
        //            {
        //                // Shift worse results down
        //                for (int k = LowestCost.Length - 1; k > j; k--)
        //                {
        //                    LowestCost[k] = LowestCost[k - 1];
        //                }
        //
        //                LowestCost[j] = (trueCost, i);
        //                break;
        //            }
        //        }
        //    }
        //    //Console.WriteLine(LowestCost.Count());
        //    List<(float, Network)> output = new();
        //    for (int i = 0; i < subsetEnd; i++)
        //    {
        //            output.Add(input[LowestCost[i%subset].Item2]);
        //    }
        //    int remaining = length - output.Count;
        //    for (int i = 0; i < length - subsetEnd; i++)
        //    {
        //        output.Add(input[LowestCost[(subset + i)].Item2]);
        //    }
        //    T.Stop();
        //    //Console.WriteLine(T.Elapsed);
        //    return output.ToArray();
        //}
        public (bool Pass, float Accuracy, List<(float[], float[], float[], bool)> DataPoints) Test(float passAccuracy = 0.001f)
        {
            List<(float[], float[], float[], bool)> Output = new();
            bool NPassed = true;
            float largestDrift = 0;
            foreach (var line in TD.Data)
            {
                float[] Expected = line[TD.inputs..];
                float[] Recieved = N.Process(line[..TD.inputs]);
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
        public void TestVerbose(float passAccuracy = 0.001f)
        {
            var Test = this.Test(passAccuracy);
            CT.Print(Test.Item3.Select(a => $"{a.Item4} - ({CT.ToString(CT.toNSD(a.Item1, 2))}), ({CT.ToString(CT.toNSD(a.Item2, 2))}), ({CT.ToString(CT.toNSD(a.Item3, 2))})").ToArray(), null, "Results: ", 5);
            CT.Print("________Final Results_______");
            CT.Print("Worst Accuracy | " + Test.Item2);
            CT.Print("Test Passed    | " + Test.Item1);
        }
    }
    public static class DirectorExtensions
    {

    }

}
