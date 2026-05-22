// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashBenchmarks.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Security.Cryptography.Benchmarks;

/// <summary>
/// Measures hashing throughput for the Snefru, CubeHash, and Whirlpool algorithms through the public
/// <c>HashAlgorithm.ComputeHash(byte[])</c> surface.
/// </summary>
[MemoryDiagnoser]
public class HashBenchmarks
{
    private byte[] _payload = Array.Empty<byte>();
    private Snefru128 _snefru128 = null!;
    private Snefru256 _snefru256 = null!;
    private CubeHash _cubeHash = null!;
    private Whirlpool _whirlpool = null!;

    /// <summary>
    /// The size, in bytes, of the input buffer hashed by each benchmark.
    /// </summary>
    [Params(256, 65536, 1048576)]
    public int PayloadSize;

    /// <summary>
    /// Allocates the input buffer and constructs one reusable instance of each algorithm.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        new Random(20260522).NextBytes(_payload);

        _snefru128 = new Snefru128();
        _snefru256 = new Snefru256();
        _cubeHash = new CubeHash();
        _whirlpool = new Whirlpool();
    }

    /// <summary>
    /// Hashes the input buffer with <see cref="Snefru128" />.
    /// </summary>
    /// <returns>The computed digest.</returns>
    [Benchmark]
    public byte[] Snefru128_ComputeHash() =>
        _snefru128.ComputeHash(_payload);

    /// <summary>
    /// Hashes the input buffer with <see cref="Snefru256" />.
    /// </summary>
    /// <returns>The computed digest.</returns>
    [Benchmark]
    public byte[] Snefru256_ComputeHash() =>
        _snefru256.ComputeHash(_payload);

    /// <summary>
    /// Hashes the input buffer with <see cref="CubeHash" />.
    /// </summary>
    /// <returns>The computed digest.</returns>
    [Benchmark]
    public byte[] CubeHash_ComputeHash() =>
        _cubeHash.ComputeHash(_payload);

    /// <summary>
    /// Hashes the input buffer with <see cref="Whirlpool" />.
    /// </summary>
    /// <returns>The computed digest.</returns>
    [Benchmark]
    public byte[] Whirlpool_ComputeHash() =>
        _whirlpool.ComputeHash(_payload);
}
