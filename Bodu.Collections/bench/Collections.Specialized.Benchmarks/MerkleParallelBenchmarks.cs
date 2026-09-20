// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleParallelBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

using BenchmarkDotNet.Attributes;

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Specialized.Benchmarks;

/// <summary>
/// Measures what parallel leaf hashing actually buys, across the four leaf hashes the RFC 6962 guide tabulates and
/// the three block-mode entry points: the sequential fold, the stream parallel overload, and the in-memory parallel
/// overload.
/// </summary>
/// <remarks>
/// <para>
/// The guide publishes speedup figures for these combinations, and they are the kind of number that quietly rots:
/// they depend on core count, on whether the leaf hash is hardware-accelerated, and on how the runtime schedules the
/// workers. This harness exists so the table can be re-measured rather than trusted.
/// </para>
/// <para>
/// The comparison that matters is <c>Sequential</c> against the two parallel columns at a fixed
/// <see cref="LeafHash" />, not one hash against another. A hardware-accelerated SHA-256 already runs close to memory
/// bandwidth, so it is the case where extra threads have least to recover; the managed digests are where the gain
/// shows. Reading across hashes measures the digests, which is what the cryptography benchmarks are for.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class MerkleParallelBenchmarks
{
    /// <summary>The input size hashed by every benchmark, in bytes.</summary>
    private const int PayloadSize = 64 * 1024 * 1024;

    /// <summary>The block size each input is cut into leaves by, in bytes.</summary>
    private const int BlockSize = 1024 * 1024;

    private byte[] _payload = Array.Empty<byte>();
    private Rfc6962MerkleTree _tree = null!;

    /// <summary>The leaf hash under test. Named rather than typed so the report rows are readable.</summary>
    [Params("SHA-256", "SHA-512", "Tiger", "BLAKE2b")]
    public string LeafHash = "SHA-256";

    /// <summary>
    /// Allocates the payload once and builds the tree for the selected leaf hash.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        Random.Shared.NextBytes(_payload);
        _tree = new Rfc6962MerkleTree(CreateFactory(LeafHash));
    }

    /// <summary>
    /// The sequential baseline: folds the tree as it reads, holding a logarithmic number of hashes.
    /// </summary>
    /// <returns>The Merkle Tree Hash of the payload.</returns>
    [Benchmark(Baseline = true)]
    public byte[] Sequential()
    {
        using var stream = new MemoryStream(_payload, writable: false);
        return _tree.ComputeRootOfBlocks(stream, BlockSize);
    }

    /// <summary>
    /// Parallel leaf hashing over a stream. The read is necessarily sequential, so each block is copied on the
    /// calling thread before a worker can touch it, which caps the achievable gain.
    /// </summary>
    /// <returns>The Merkle Tree Hash of the payload, bit-identical to <see cref="Sequential" />.</returns>
    [Benchmark]
    public byte[] ParallelFromStream()
    {
        using var stream = new MemoryStream(_payload, writable: false);
        return _tree.ComputeBlockedParallel(stream, BlockSize).Root;
    }

    /// <summary>
    /// Parallel leaf hashing over bytes already in memory, where each worker copies <em>and</em> hashes its own
    /// block. This is the overload that scales.
    /// </summary>
    /// <returns>The Merkle Tree Hash of the payload, bit-identical to <see cref="Sequential" />.</returns>
    [Benchmark]
    public byte[] ParallelFromMemory() =>
        _tree.ComputeBlockedParallel(_payload.AsMemory(), BlockSize).Root;

    /// <summary>
    /// Returns a fresh-instance factory for the named leaf hash.
    /// </summary>
    /// <param name="name">The leaf-hash name supplied by <see cref="LeafHash" />.</param>
    /// <returns>A delegate producing a new <see cref="HashAlgorithm" /> on each call.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="name" /> is not a known leaf hash.</exception>
    private static Func<HashAlgorithm> CreateFactory(string name) => name switch
    {
        "SHA-256" => SHA256.Create,
        "SHA-512" => SHA512.Create,
        "Tiger" => () => new Bodu.Security.Cryptography.Tiger(),
        "BLAKE2b" => () => new Bodu.Security.Cryptography.Blake2b(),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown leaf hash."),
    };
}
