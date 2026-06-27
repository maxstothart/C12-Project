using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Tools;


namespace Base
{
    public static class GPUSupport
    {
        public static List<TimeSpan> GPUTest(int ArraySize)
        {
            List<TimeSpan> Points = new();
            var timer = Stopwatch.StartNew();

            // Initialize ILGPU.
            Context context = Context.CreateDefault();
            Accelerator accelerator = context.CreateCudaAccelerator(0);

            // Load the data.
            var Data = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var Output = new int[(int)float.Pow(10,ArraySize)];
            var deviceData = accelerator.Allocate1D(Data);
            var deviceOutput = accelerator.Allocate1D<int>(Output);

            Points.Add(timer.Elapsed);

            // load / compile the kernel
            var Kernel = accelerator.LoadAutoGroupedStreamKernel(

            (Index1D i, ArrayView<int> data, ArrayView<int> output) =>
            {
                output[i] = data[i % data.Length]+i;
            });

            // tell the accelerator to start computing the kernel
            Kernel((int)deviceOutput.Length, deviceData.View, deviceOutput.View);
            accelerator.Synchronize();

            Points.Add(timer.Elapsed);

            //Copy data back to memory
            deviceOutput.CopyToCPU(Output);

            Points.Add(timer.Elapsed);

            Console.WriteLine(Output[^1]);

            //dispose of stuff
            timer.Stop();
            accelerator.Dispose();
            context.Dispose();
            return Points;
        }
        public static List<TimeSpan> CPUTest(int ArraySize, int threads = 10)
        {
            List<TimeSpan> Points = new();
            var timer = Stopwatch.StartNew();

            // Load the data.
            var Data = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var Output = new int[(int)float.Pow(10, ArraySize)];
            int pointsPerThread = (Output.Length / threads);

            Points.Add(timer.Elapsed);

            _ = Parallel.For(0, threads, thread =>
            {
                for (int i = (Output.Length / threads) * (thread); i < (Output.Length/threads)*(thread+1); i++)
                {
                    Output[i] = Data[i % Data.Length] + i;
                }
            });

            Points.Add(timer.Elapsed);

            Console.WriteLine(Output[^1]);

            //dispose of stuff
            timer.Stop();
            return Points;
        }

        public static void printResults(List<TimeSpan> r, string title)
        {
            var T = StepWise(r);
            T.Item1.Add(T.Item2);
            ConsoleTools.Print(T.Item1.Select(i => i.ToString()).ToArray());
        }
        private static (List<TimeSpan>, TimeSpan) StepWise(List<TimeSpan> T)
        {
            TimeSpan Total = T[^1];
            for (int i = T.Count-1; i > 0; i--)
            {
                TimeSpan x = T[0];
                for (int j = 1; j < i; j++)
                {
                    x += T[j];
                }
                T[i] = x;
            }
            return (T, Total);
        }
        public static void listDevices()
        {
            // Builds a context that has all possible accelerators.
            using Context context = Context.CreateDefault();
            // Prints all accelerators.
            foreach (Device d in context)
            {
                using Accelerator accelerator = d.CreateAccelerator(context);
                Console.WriteLine(accelerator);

                StringWriter infoString = new StringWriter();
                accelerator.PrintInformation(infoString);
                Console.WriteLine(infoString.ToString());
            }
        }
    }
    public class ProcessCost : IDisposable
    {
        Context context;
        Accelerator accelerator;

        MemoryBuffer1D<float, Stride1D.Dense> GInputs;
        MemoryBuffer1D<float, Stride1D.Dense> GOutputs;
        MemoryBuffer1D<int, Stride1D.Dense> GDimensions;

        MemoryBuffer1D<float, Stride1D.Dense> NWeights;
        MemoryBuffer1D<float, Stride1D.Dense> NBiases;
        MemoryBuffer1D<int, Stride1D.Dense> NIndex;
        MemoryBuffer1D<int, Stride1D.Dense> NStructure;

        public ProcessCost(DirectorGPU.FlattenedData TD, Network DefaultNetwork)
        {
            context = Context.CreateDefault();
            accelerator = context.CreateCudaAccelerator(0);

            //Load Data from FlattenedData into GPU
            GInputs = TD.Inputs;
            GOutputs = TD.Outputs;
            GDimensions = accelerator.Allocate1D(new int[] { TD.DataCounts., TD.outputCount });

            loadNetwork(DefaultNetwork);

        }
        
        public void loadNetwork(Network network)
        {
            NWeights = accelerator.Allocate1D(network.Weights);
            NBiases = accelerator.Allocate1D(network.Biases);
            NIndex = accelerator.Allocate1D(network.Index);
            NStructure = accelerator.Allocate1D(network.Structure);
        }

        public float Run(Network.PCParams Par)
        {
            return 0.0f;
        }

        public void Dispose()
        {
            accelerator.Dispose();
            context.Dispose();
        }
    }
    
}
