// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Unit tests for <see cref="ParallelMerkleTreeHash" />.
/// </summary>
/// <remarks>
/// Inherits the shared test suite from <see cref="MerkleTreeHashTestsBase{THasher}" />, which
/// covers constructor validation, dispose semantics, and every byte-oriented <c>ComputeHash</c>
/// scenario that applies equally to the sequential and parallel implementations. Tests that are
/// specific to the parallel implementation — asynchronous streaming (<c>ComputeHashAsync</c>),
/// diagnostics, parity with the sequential reference, stress/concurrency, performance, and the
/// per-instance-call-count invariants of <c>MonitoringHashAlgorithm</c> — live in partial files
/// on this class.
/// </remarks>
[TestClass]
public partial class ParallelMerkleTreeHashTests : MerkleTreeHashTestsBase<ParallelMerkleTreeHash>
{
    // ─── Factory thunks — adapt the base class to ParallelMerkleTreeHash's ctor/overloads ─────

    /// <inheritdoc />
    protected override ParallelMerkleTreeHash Construct(
        Func<HashAlgorithm>? factory, int? blockSize = null, int? fanOut = null) =>
        new ParallelMerkleTreeHash(
            factory!,
            blockSize: blockSize ?? MerkleTestData.DefaultBlockSize,
            fanOut: fanOut ?? MerkleTestData.DefaultFanOut);

    /// <inheritdoc />
    protected override byte[] ComputeHash(ParallelMerkleTreeHash hasher, byte[] data) =>
        hasher.ComputeHash(data);

    /// <inheritdoc />
    protected override byte[] ComputeHash(ParallelMerkleTreeHash hasher, byte[] data, int offset, int count) =>
        hasher.ComputeHash(data, offset, count);

    /// <inheritdoc />
    protected override byte[] ComputeHash(ParallelMerkleTreeHash hasher, ReadOnlySpan<byte> data) =>
        hasher.ComputeHash(data);
}
