// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Harness.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Reproduces the measurement method in the appendix of FallbackPlan's Argon2 requirements, so figures taken here line
/// up with the table in its section 2.1: wall time, CPU time, cores used, allocation, gen2 collections, and GC pause per
/// derivation, one at a time and four at once.
/// </summary>
/// <remarks>
/// <para>
/// Run with <c>--argon2-harness</c>. Built with <c>-p:BoduCryptoBaseline=1.0.0</c>, the same source measures the
/// published package, which is how a baseline is taken on the machine under test.
/// </para>
/// <para>
/// <c>--argon2-harness --first-call</c> derives once and prints how long the process's first derivation took. Run it
/// several times from a shell to sample what a command-line unlock waits through; in-process, only the first call of a
/// process can be measured.
/// </para>
/// <para>
/// <c>--argon2-harness --sweep</c> measures where threads pay instead: matrices of 256 KiB to 32 MiB at p = 4, each with
/// the default bound and, where the build has one, confined to one thread; and below 3 MiB, where the library keeps a
/// derivation on one thread, with every slice divided among threads regardless.
/// </para>
/// </remarks>
internal static class Argon2Harness
{
    /// <summary>The number of untimed derivations before each measurement.</summary>
    private const int WarmUpCount = 6;

    /// <summary>The number of timed derivations in each measurement.</summary>
    private const int RunCount = 10;

    /// <summary>The number of threads, and derivations per thread, in the four-at-once measurement.</summary>
    private const int Concurrency = 4;

    private static readonly byte[] Password = "correct horse battery staple"u8.ToArray();
    private static readonly byte[] Salt = "0123456789abcdef"u8.ToArray();

    /// <summary>
    /// Runs the harness.
    /// </summary>
    /// <param name="args">
    /// The arguments after <c>--argon2-harness</c>: <c>--first-call</c> selects the first-call mode, and <c>--sweep</c>
    /// the thread sweep.
    /// </param>
    internal static void Run(string[] args)
    {
        var parameters = new Argon2Parameters { MemoryKiB = 65536, Iterations = 3, Parallelism = 4, TagLength = 32 };

        if (args.Contains("--first-call"))
        {
            var stopwatch = Stopwatch.StartNew();
            _ = Argon2id.DeriveKey(Password, Salt, parameters);
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"first call {stopwatch.Elapsed.TotalMilliseconds:F1} ms ({Describe()})"));
            return;
        }

        Console.WriteLine(Describe());
        if (args.Contains("--sweep"))
        {
            Sweep(parameters);
            return;
        }

        Measure("p = 4", () => Argon2id.DeriveKey(Password, Salt, parameters));
        Measure("p = 1", () => Argon2id.DeriveKey(Password, Salt, parameters with { Parallelism = 1 }));
        MeasureConcurrent("p = 4, four at once", () => Argon2id.DeriveKey(Password, Salt, parameters));
#if !BODU_CRYPTO_BASELINE

        // The published package has no bound; these rows show what a caller that confines each derivation to its own
        // thread gets.
        var oneThread = new Argon2id(parameters, maxDegreeOfParallelism: 1);
        Measure("p = 4, one thread", () => oneThread.GetBytes(Password, Salt));
        MeasureConcurrent("p = 4, one thread, 4 at once", () => oneThread.GetBytes(Password, Salt));
#endif
    }

    /// <summary>
    /// Measures where threads pay: for matrices of 256 KiB to 32 MiB at p = 4, derivations with the default bound, which
    /// divide a slice among threads once its segments reach the library's threshold (192 blocks, a 3 MiB matrix, where
    /// 1.2.0 sets it), and, where the build has a bound, the same derivations confined to one thread. Below 3 MiB the
    /// build's own derivations also run with every slice divided among threads, through <see cref="Argon2FillDriver" />,
    /// to show whether the threshold could sit lower still.
    /// </summary>
    /// <param name="parameters">The parameters to vary the memory size of.</param>
    private static void Sweep(Argon2Parameters parameters)
    {
#if !BODU_CRYPTO_BASELINE
        Argon2FillDriver driver = Argon2FillDriver.Create();
#endif
        foreach (int kibibytes in new[] { 256, 512, 1024, 2048, 3072, 4096, 8192, 16384, 32768 })
        {
            Argon2Parameters sized = parameters with { MemoryKiB = kibibytes };
            string size = kibibytes < 1024
                ? string.Create(CultureInfo.InvariantCulture, $"{kibibytes} KiB")
                : string.Create(CultureInfo.InvariantCulture, $"{kibibytes / 1024} MiB");
            Measure($"m = {size}, p = 4", () => Argon2id.DeriveKey(Password, Salt, sized));
#if !BODU_CRYPTO_BASELINE
            var oneThread = new Argon2id(sized, maxDegreeOfParallelism: 1);
            Measure($"m = {size}, p = 4, one thread", () => oneThread.GetBytes(Password, Salt));
            if (kibibytes < 3072)
                Measure($"m = {size}, p = 4, threaded", () => driver.DeriveKey(sized, Password, Salt, minimumParallelSegmentLength: 1));
#endif
        }
    }

    /// <summary>
    /// Describes the build and the host the numbers come from.
    /// </summary>
    /// <returns>The package version under test, the runtime, the processor count, and the vector sets the host supports.</returns>
    private static string Describe()
    {
        string version = typeof(Argon2id).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Bodu.Security.Cryptography {version} on {RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture}, {Environment.ProcessorCount} logical processors, AVX2 {System.Runtime.Intrinsics.X86.Avx2.IsSupported}, AVX-512F {System.Runtime.Intrinsics.X86.Avx512F.IsSupported}, AdvSimd {System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported}, library SIMD {(Program.IsSimdDisabled ? "off" : "on")}");
    }

    /// <summary>
    /// Measures one derivation at a time, as the requirements' appendix does.
    /// </summary>
    /// <param name="name">The label printed with the results.</param>
    /// <param name="derive">The derivation to measure.</param>
    private static void Measure(string name, Func<byte[]> derive)
    {
        for (int i = 0; i < WarmUpCount; i++)
            _ = derive();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        TimeSpan pauseBefore = GC.GetTotalPauseDuration();
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        int gen2Before = GC.CollectionCount(2);
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        TimeSpan cpuBefore = process.TotalProcessorTime;

        var walls = new List<double>(RunCount);
        var total = Stopwatch.StartNew();
        for (int i = 0; i < RunCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            _ = derive();
            walls.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        total.Stop();
        process.Refresh();

        double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds / RunCount;
        double allocatedKiB = (GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore) / 1024.0 / RunCount;
        double gen2 = (GC.CollectionCount(2) - gen2Before) / (double)RunCount;
        double pause = (GC.GetTotalPauseDuration() - pauseBefore).TotalMilliseconds / RunCount;
        walls.Sort();

        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{name,-28} wall {walls[walls.Count / 2],7:F1} ms   cpu {cpu,7:F1} ms   cores {cpu / (total.Elapsed.TotalMilliseconds / RunCount),4:F2}   allocated {allocatedKiB,9:F1} KiB   gen2 {gen2,4:F2}   pause {pause,4:F1} ms"));
    }

    /// <summary>
    /// Measures four derivations at once, each on its own thread, each thread deriving four times, as the requirements'
    /// appendix does.
    /// </summary>
    /// <param name="name">The label printed with the results.</param>
    /// <param name="derive">The derivation to measure.</param>
    private static void MeasureConcurrent(string name, Func<byte[]> derive)
    {
        for (int i = 0; i < 2; i++)
            _ = derive();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        int gen2Before = GC.CollectionCount(2);
        using Process process = Process.GetCurrentProcess();
        process.Refresh();
        TimeSpan cpuBefore = process.TotalProcessorTime;

        using var start = new ManualResetEventSlim(false);
        var threads = new Thread[Concurrency];
        for (int t = 0; t < Concurrency; t++)
        {
            threads[t] = new Thread(() =>
            {
                start.Wait();
                for (int i = 0; i < Concurrency; i++)
                    _ = derive();
            });
            threads[t].Start();
        }

        var total = Stopwatch.StartNew();
        start.Set();
        foreach (Thread thread in threads)
            thread.Join();

        total.Stop();
        process.Refresh();

        int derivations = Concurrency * Concurrency;
        double cpu = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds / derivations;
        double gen2 = (GC.CollectionCount(2) - gen2Before) / (double)derivations;

        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{name,-28} {derivations} in {total.Elapsed.TotalMilliseconds,6:F0} ms   {total.Elapsed.TotalMilliseconds / derivations,6:F1} ms each   cpu {cpu,7:F1} ms each   gen2 {gen2,4:F2}"));
    }
}
