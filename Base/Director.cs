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
        public int TrainEvolutionary(int concurrentCount, int threads, int ElitePopulation, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, bool Verbose = true, bool shock = false)
        {
            int Epochs = EpochsPerMillion * (maxIT / 1000000);
            if (Epochs == 0) { Epochs = 1; }
            var timer = Stopwatch.StartNew();
            


            (float, Network)[] Best = getBest(new (float, Network)[] { (N.ProcessCost(TD, DataDepth, Par), N) }, concurrentCount);

            int i = 1;
            concurrentCount += concurrentCount % threads;
            int processesPerEpoch = maxIT / Epochs / threads;
            int pointsPerThread = concurrentCount / threads;
            int OldID = 0;

            for (int Ep = 0; Ep < Epochs; Ep++)
            {
                //if (Verbose) { CT.Print("____________EPOCH________________"); }

                Best = getBest(Best, concurrentCount, ElitePopulation);
                float deviation = float.Pow(Deviation, OP.Clamp(float.Log10(Best[0].Item1), -1f, 2));


                if (i - 100000 > OldID && shock)
                {
                    for (int B = 4; B < Best.Length; B++)
                    {
                        //Best[B].Item2.Add(Rand.Next(Best[B].Item2.Structure.Count-2)+1);
                        Best[B].Item2.Mutate(Deviation*2, breadth*2, 2);
                        Best[B].Item1 = Best[B].Item2.ProcessCost(TD, DataDepth, Par);
                    }
                }
                if (float.Abs(Best[0].Item1) - float.Pow(10, accuracy) <= 0 || Best[0].Item1 == 0) { break; }
                _ = Parallel.For(0, threads, thread =>
                {

                    for (int j = 0; j < processesPerEpoch; j++)
                    {

                        int CIndex = i;
                        int position = thread * pointsPerThread + i % pointsPerThread;
                        var NewN = Best[position].Item2.Copy(CIndex).Mutate(deviation, breadth, 2);
                        var Cost = NewN.ProcessCost(TD, DataDepth, Par);
                        if (Cost < Best[position].Item1)
                        {
                            Best[position] = (Cost, NewN.Copy());
                            _ = Interlocked.Exchange(ref OldID, i);
                            if (Verbose) { CT.Print($"IT: {Best[position].Item2.ID} - {Best[position].Item1} - {deviation}"); }
                        }
                        Interlocked.Increment(ref i);
                    }
                    Interlocked.Increment(ref i);
                });
                }
                timer.Stop();
                this.N = getBest(Best, 1)[0].Item2;
                CT.Print($"{i} Iterations, Final Cost: {N.ProcessCost(TD, 200, Par)}, Time Ellapsed: {timer.Elapsed.ToString()}");
                return i;
            }

        public void EvolutionStaggerTrain(float origin, float destination, int attempts, int concurrentCount, int threads, int ElitePopulation, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, bool Verbose = true, bool shock = false)
        {
            float increment = (destination - origin) / attempts;
            for (int i = 0; i < attempts; i++)
            {
                CT.Print(origin + increment * i);
                TD.refreshData(origin + increment * i, DataDepth/2);
                TrainEvolutionary(concurrentCount, threads, ElitePopulation, EpochsPerMillion, accuracy, ((i+1 == attempts) ? maxIT: maxIT*2), DataDepth, Deviation, breadth, Par, Verbose, shock);
            }
        }





        public static (float, Network)[] getBest((float, Network)[] input, int length, int subset = 1)
        {
            (float, int)[] LowestCost = new (float, int)[subset];
            for (int i = 0; i < subset; i++)
            {
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
                output.Add(input[LowestCost[i % subset].Item2]);
            }
            return output.ToArray();
        }
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
            CT.Print("Worst Accuracy | "+Test.Item2);
            CT.Print("Test Passed    | "+Test.Item1);
        }
    }
}
