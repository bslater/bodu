// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2FirstCallBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Measures the first Argon2id derivation in a fresh process - what a command-line unlock waits through - at RFC 9106's
/// second recommended cost.
/// </summary>
/// <remarks>
/// Each launch is a new process that derives once, so the measurement includes the JIT compiling the fill loop and the
/// operating system supplying the matrix's pages for the first time. A warm derivation is measured by
/// <see cref="Argon2Benchmarks" />.
/// </remarks>
[SimpleJob(RunStrategy.ColdStart, launchCount: 10, warmupCount: 0, iterationCount: 1)]
public class Argon2FirstCallBenchmarks
{
    private readonly byte[] _password = "correct horse battery staple"u8.ToArray();
    private readonly byte[] _salt = "0123456789abcdef"u8.ToArray();
    private readonly Argon2Parameters _parameters = new() { MemoryKiB = 65536, Iterations = 3, Parallelism = 4, TagLength = 32 };

    /// <summary>
    /// Derives the process's first tag.
    /// </summary>
    /// <returns>The derived tag.</returns>
    [Benchmark]
    public byte[] FirstDerivation() =>
        Argon2id.DeriveKey(_password, _salt, _parameters);
}
