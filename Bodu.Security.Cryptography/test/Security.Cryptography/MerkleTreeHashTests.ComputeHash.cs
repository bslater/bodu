// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the synchronous <c>ComputeHash(Stream)</c> overload — exposed only by
/// <see cref="MerkleTreeHash" />. The parallel implementation offers a stream path solely
/// through <c>ComputeHashAsync</c>.
/// </summary>
public partial class MerkleTreeHashTests
{
    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <see langword="null" /> stream raises <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamIsNull_ShouldThrowExactly()
    {
        using MerkleTreeHash hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<ArgumentNullException>(() => hasher.ComputeHash((Stream)null!));
    }

    // ─── Result shape ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the stream overload returns a non-null hash of the expected length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamProvided_ShouldReturnHashOfExpectedLength()
    {
        using MerkleTreeHash hasher = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using var stream = new IncrementingByteStream(8);

        var result = hasher.ComputeHash(stream);

        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.Length); // MonitoringHashAlgorithm: sizeof(uint)
    }

    // ─── Equivalence with in-memory overloads ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that hashing a <see cref="MemoryStream" /> of the same bytes produces the same
    /// result as the span overload.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamMatchesSpan_ShouldProduceIdenticalResult()
    {
        var data = MakeData(13);

        using MerkleTreeHash h1 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using MerkleTreeHash h2 = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        using var ms = new MemoryStream(data);
        var fromStream = h1.ComputeHash(ms);
        var fromSpan = h2.ComputeHash(data.AsSpan());

        CollectionAssert.AreEqual(fromSpan, fromStream);
    }

    // ─── Partial-read delivery ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a stream delivering data in partial chunks (<see cref="IncrementingByteStream" />
    /// returns at most half of its remaining bytes per read) produces the same hash as the
    /// equivalent span-based call.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenStreamDeliversPartialReads_ShouldProduceSameResultAsSpan()
    {
        const int length = 37; // prime; exercises every partial-read boundary

        using MerkleTreeHash h1 = Construct(Factory, DefaultBlockSize, DefaultFanOut);
        using MerkleTreeHash h2 = Construct(Factory, DefaultBlockSize, DefaultFanOut);

        using var stream = new IncrementingByteStream(length);
        var fromStream = h1.ComputeHash(stream);
        var fromSpan = h2.ComputeHash(new IncrementingByteStream(length).ToArray().AsSpan());

        CollectionAssert.AreEqual(fromStream, fromSpan);
    }
}
