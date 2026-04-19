using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base
{
    public static class GPUSupport
    {
        public static void GPU()
        {
            var timer = Stopwatch.StartNew();

            // Initialize ILGPU.
            Context context = Context.CreateDefault();
            Accelerator accelerator = context.CreateCudaAccelerator(0);

            Console.WriteLine("init - "+timer.Elapsed); timer = Stopwatch.StartNew();

            // Load the data.
            var Data = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var Output = new int[1000000000];
            var deviceData = accelerator.Allocate1D(Data);
            var deviceOutput = accelerator.Allocate1D<int>(Output);

            Console.WriteLine("Data Copied - " + timer.Elapsed); timer = Stopwatch.StartNew();

            // load / compile the kernel
            var Kernel = accelerator.LoadAutoGroupedStreamKernel(

            (Index1D i, ArrayView<int> data, ArrayView<int> output) =>
            {
                output[i] = data[i % data.Length]+i;
            });

            // tell the accelerator to start computing the kernel
            Kernel((int)deviceOutput.Length, deviceData.View, deviceOutput.View);
            accelerator.Synchronize();

            Console.WriteLine("GPU Finished - "+timer.Elapsed); timer = Stopwatch.StartNew();

            //Copy data back to memory
            deviceOutput.CopyToCPU(Output);

            Console.WriteLine("CPU Recieved Data - "+timer.Elapsed);

            Console.WriteLine(Output[^1]);

            //dispose of stuff
            timer.Stop();
            accelerator.Dispose();
            context.Dispose();
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
}
