using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.ScanReduceOperations;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Base.Network;
using CT = Tools.ConsoleTools;
using OP = Tools.Operations;

namespace Base
{
    public class DirectorGPU
    {
        Context context;
        Accelerator accelerator;
        FlattenedNetwork N;
        FlattenedData TD;

        public struct FlattenedNetwork
        {
            public MemoryBuffer1D<float, Stride1D.Dense> Weights;
            public MemoryBuffer1D<float, Stride1D.Dense> Biases;
            public MemoryBuffer1D<int, Stride1D.Dense> Index;
            public MemoryBuffer1D<int, Stride1D.Dense> Structure;
            public FlattenedNetwork(Network IN, Accelerator A)
            {
                Weights = A.Allocate1D(IN.Weights);
                Biases = A.Allocate1D(IN.Biases);
                Index = A.Allocate1D(IN.Index);
                Structure = A.Allocate1D(IN.Structure);
            }
            public Network AsNetwork()
            {
                var O = new Network();
                Weights.CopyToCPU(O.Weights);
                Biases.CopyToCPU(O.Biases);
                Index.CopyToCPU(O.Index);
                Structure.CopyToCPU(O.Structure);
                return O;
            }
        }
        public struct FlattenedData
        {
            public MemoryBuffer1D<float, Stride1D.Dense> Inputs;
            public MemoryBuffer1D<float, Stride1D.Dense> Outputs;
            public MemoryBuffer1D<int, Stride1D.Dense> DataCounts;//Inputs, Outputs, TotalDataCount(Inputs.Length/DataCounts[0])
            public FlattenedData(TrainingData TD, Accelerator A)
            {
                List<float> inputVals = new();
                List<float> outputVals = new();
                foreach (var dat in TD.Data)
                {
                    inputVals.AddRange(dat[..TD.inputs]);
                    outputVals.AddRange(dat[TD.inputs..]);
                }
                Inputs = A.Allocate1D(inputVals.ToArray());
                Outputs = A.Allocate1D(outputVals.ToArray());
                DataCounts = A.Allocate1D(new int[] { TD.inputs, TD.outputs, TD.Data.Count });
            }
            public FlattenedData((float[] _inputs, float[] _outputs, int[] _DataCounts) Input, Accelerator A)
            {
                Inputs = A.Allocate1D(Input._inputs);
                Outputs = A.Allocate1D(Input._outputs);
                DataCounts = A.Allocate1D(Input._DataCounts);
            }
            public TrainingData AsTrainingData()
            {
                var O = new TrainingData();
                var Temp = ExportData();
                List<float[]> Data = new();
                for (int i = 0; i < Temp._inputs.Length/Temp._DataCounts[0]; i++)
                {
                    Data.Add(Temp._inputs[(i * Temp._DataCounts[0])..((i + 1) * Temp._DataCounts[0])].Concat(Temp._outputs[(i * Temp._DataCounts[1])..((i + 1) * Temp._DataCounts[1])]).ToArray());
                }
                O.Data = Data;
                O.inputs = Temp._DataCounts[0];
                O.outputs = Temp._DataCounts[1];
                return O;
            }
            public (float[] _inputs, float[] _outputs, int[] _DataCounts) ExportData()
            {
                (float[] _inputs, float[] _outputs, int[] _DataCounts) output = new();
                Inputs.CopyToCPU(output._inputs);
                Outputs.CopyToCPU(output._outputs);
                DataCounts.CopyToCPU(output._DataCounts);
                return output;
            }
        }

        public DirectorGPU(TrainingData _TD, Network _N)
        {
            context = Context.Create(builder => builder.Default().EnableAlgorithms());
            accelerator = context.CreateCudaAccelerator(0);

            N = new FlattenedNetwork(_N, accelerator);
            TD = new FlattenedData(_TD, accelerator);

        }
        public int TrainEvolutionary(int concurrentCount, int threads, int ElitePopulation, int EpochsPerMillion, float accuracy = -20f, int maxIT = 10000, int DataDepth = 200, float Deviation = 3f, int breadth = 2, Network.PCParams Par = default, bool Verbose = true, bool shock = false)
        {
            int Epochs = EpochsPerMillion * (maxIT / 1000000);
            if (Epochs == 0) { Epochs = 1; }
            var timer = Stopwatch.StartNew();


            (float, FlattenedNetwork)[] Best = getBest(new (float, FlattenedNetwork)[] { (ProcessCost(N, DataDepth, Par), N) }, concurrentCount);

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
                        Mutate(Best[B].Item2, Deviation * 2, breadth * 2, 2);
                        Best[B].Item1 = ProcessCost(Best[B].Item2, DataDepth, Par);
                    }
                }
                if (float.Abs(Best[0].Item1) - float.Pow(10, accuracy) <= 0 || Best[0].Item1 == 0) { break; }
                _ = Parallel.For(0, threads, thread =>
                {

                    for (int j = 0; j < processesPerEpoch; j++)
                    {

                        int CIndex = i;
                        int position = thread * pointsPerThread + i % pointsPerThread;
                        var NewN = Best[position].Item2.Copy(CIndex);
                        Mutate(NewN, deviation, breadth, 2);
                        var Cost = ProcessCost(NewN, DataDepth, Par);
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
            CT.Print($"{i} Iterations, Final Cost: {ProcessCost(N, 200, Par)}, Time Ellapsed: {timer.Elapsed.ToString()}");
            return i;
        }
        public static (float, FlattenedNetwork)[] getBest((float, FlattenedNetwork)[] input, int length, int subset = 1)
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
            List<(float, FlattenedNetwork)> output = new();
            for (int i = 0; i < length; i++)
            {
                output.Add(input[LowestCost[i % subset].Item2]);
            }
            return output.ToArray();
        }
        
        public struct ZDriftCost
        {
            struct SquareTransformer : ITransformer<float, float>
            {
                public float Transform(float value) => value * value;
            }
            public ZDriftCost(Accelerator accelerator, MemoryBuffer1D<float, Stride1D.Dense> input, MemoryBuffer1D<float, Stride1D.Dense> Output)
            {
                using var gpuSquared = accelerator.Allocate1D<float>(input.Length);

                accelerator.Transform<float, Stride1D.Dense, SquareTransformer>(
                    accelerator.DefaultStream,
                    input.View,
                    gpuSquared.View,
                    new SquareTransformer());

                // Step 2: sum gpuSquared into gpuResult — stays on GPU
                accelerator.Reduce<float, AddFloat>(
                    accelerator.DefaultStream,
                    gpuSquared.View,
                    Output.View);
            }
            
        }
        struct XorShift128
        {
            private uint _x, _y, _z, _w;

            public XorShift128(uint seed)
            {
                _x = seed;
                _y = 362436069u;
                _z = 521288629u;
                _w = 88675123u;
            }

            public uint NextUInt()
            {
                uint t = _x ^ (_x << 11);
                _x = _y; _y = _z; _z = _w;
                _w = _w ^ (_w >> 19) ^ (t ^ (t >> 8));
                return _w;
            }

            // Float in [0, 1)
            public float NextFloat() => NextUInt() * (1.0f / 4294967296.0f);
        }
        public float ProcessCost(FlattenedNetwork Net, int Iterations, PCParams P)
        {
            float BCost = 0;
            float WCost = 0;
            float OCost = 0;

            var GBCost = accelerator.Allocate1D<float>(1);
            _ = new ZDriftCost(accelerator, Net.Biases, GBCost);

            var GWCost = accelerator.Allocate1D<float>(1);
            _ = new ZDriftCost(accelerator, Net.Weights, GBCost);

            var Kernel = accelerator.LoadAutoGroupedStreamKernel(

            (Index1D i, ArrayView<int> data, ArrayView<int> output) =>
            {
                var point = TD.getPoint();
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
            });

            BCost /= N.Biases.Length;
            WCost /= N.Weights.Length;
            //OCost /= Iterations;

            BCost *= P.BiasW;
            WCost *= P.WeightW;
            OCost *= P.outW;

            if (OCost < P.WBCutOff * P.outW) { return OCost; }

            return OCost + OP.Clamp(BCost + WCost, 0, float.PositiveInfinity);
        }

    }
}
