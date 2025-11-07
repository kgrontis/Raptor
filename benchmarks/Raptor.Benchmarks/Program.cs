using BenchmarkDotNet.Running;
using Raptor.Benchmarks;

namespace Raptor.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
        else
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}

