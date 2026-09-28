// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Benchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Measures an Argon2id derivation at RFC 9106's second recommended cost (64 MiB, three passes, four lanes) and at a
/// small memory size, one at a time and four at once, on each compression path the host can take.
/// </summary>
/// <remarks>
/// <para>
/// The three jobs select the compression path through the runtime's own switches rather than anything in the library,
/// so the same build measures all of them: <c>Default</c> takes the widest vector path the host supports,
/// <c>NoAvx2</c> (<c>DOTNET_EnableAVX2=0</c>) takes the 128-bit path on x64, and <c>Scalar</c>
/// (<c>DOTNET_EnableHWIntrinsic=0</c>) takes the scalar path. On ARM64, <c>NoAvx2</c> is the same as <c>Default</c>.
/// </para>
/// <para>
/// The <c>OnOneThread</c> rows confine each derivation to its calling thread with a bound of one, as 1.0.0 always ran;
/// they are absent when the class is built against a published package that has no bound.
/// </para>
/// <para>
/// A 64 MiB derivation costs tens to hundreds of milliseconds, so BenchmarkDotNet's default iteration counts make this
/// class slow; filter it (<c>--filter *Argon2Benchmarks*</c>) and consider <c>--job short</c> for a quick look. The
/// allocation column is the one ARG-N-004 is judged by, and the four-at-once rows report the time per derivation.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[Config(typeof(PathConfig))]
public class Argon2Benchmarks
{
    /// <summary>The number of derivations the concurrent benchmark runs at once.</summary>
    private const int Concurrency = 4;

    private readonly byte[] _password = "correct horse battery staple"u8.ToArray();
    private readonly byte[] _salt = "0123456789abcdef"u8.ToArray();
    private Argon2Parameters _parameters = null!;
#if !BODU_CRYPTO_BASELINE
    private Argon2id _oneThread = null!;
#endif

    /// <summary>The memory cost, in KiB: a small size, and RFC 9106's 64 MiB.</summary>
    [Params(256, 65536)]
    public int MemoryKiB;

    /// <summary>
    /// Builds the parameters for the selected memory cost.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _parameters = new Argon2Parameters { MemoryKiB = MemoryKiB, Iterations = 3, Parallelism = 4, TagLength = 32 };
#if !BODU_CRYPTO_BASELINE
        _oneThread = new Argon2id(_parameters, maxDegreeOfParallelism: 1);
#endif
    }

    /// <summary>
    /// Derives one tag on the calling thread's behalf, as an interactive unlock does.
    /// </summary>
    /// <returns>The derived tag.</returns>
    [Benchmark(Baseline = true)]
    public byte[] OneAtATime() =>
        Argon2id.DeriveKey(_password, _salt, _parameters);

    /// <summary>
    /// Derives four tags at once, as a service verifying several logins does, and reports the time per derivation.
    /// </summary>
    [Benchmark(OperationsPerInvoke = Concurrency)]
    public void FourAtOnce() =>
        Parallel.For(0, Concurrency, new ParallelOptions { MaxDegreeOfParallelism = Concurrency }, _ => Argon2id.DeriveKey(_password, _salt, _parameters));
#if !BODU_CRYPTO_BASELINE

    /// <summary>
    /// Derives one tag confined to the calling thread.
    /// </summary>
    /// <returns>The derived tag.</returns>
    [Benchmark]
    public byte[] OneAtATimeOnOneThread() =>
        _oneThread.GetBytes(_password, _salt);

    /// <summary>
    /// Derives four tags at once, each confined to its calling thread, and reports the time per derivation.
    /// </summary>
    [Benchmark(OperationsPerInvoke = Concurrency)]
    public void FourAtOnceOnOneThreadEach() =>
        Parallel.For(0, Concurrency, new ParallelOptions { MaxDegreeOfParallelism = Concurrency }, _ => _oneThread.GetBytes(_password, _salt));
#endif

    /// <summary>
    /// Selects the compression path per job through the runtime's hardware-intrinsic switches.
    /// </summary>
    private sealed class PathConfig : ManualConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PathConfig" /> class with the default, 128-bit, and scalar jobs.
        /// </summary>
        public PathConfig()
        {
            AddJob(Job.Default.WithId("Default"));
            AddJob(Job.Default.WithEnvironmentVariables(new EnvironmentVariable("DOTNET_EnableAVX2", "0")).WithId("NoAvx2"));
            AddJob(Job.Default.WithEnvironmentVariables(new EnvironmentVariable("DOTNET_EnableHWIntrinsic", "0")).WithId("Scalar"));
        }
    }
}
