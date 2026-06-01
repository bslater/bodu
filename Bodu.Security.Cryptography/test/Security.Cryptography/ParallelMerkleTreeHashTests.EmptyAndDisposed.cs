// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.EmptyAndDisposed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Behaviours specific to <see cref="ParallelMerkleTreeHash" /> that the sequential class does
/// not share — specifically, empty-input handling (throws
/// <see cref="InvalidOperationException" />) and post-dispose access (throws
/// <see cref="ObjectDisposedException" />).
/// </summary>
public partial class ParallelMerkleTreeHashTests
{
    // ─── Empty input ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty span raises <see cref="InvalidOperationException" /> — no leaves
    /// can be produced from zero bytes, so the implementation rejects the call rather than
    /// returning an arbitrary "zero-leaves" hash.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsEmpty_ShouldThrowExactly()
    {
        using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            hasher.ComputeHash([]));
    }

    // ─── Post-dispose access ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <c>ComputeHash(ReadOnlySpan&lt;byte&gt;)</c> after <c>Dispose</c> raises
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInstanceIsDisposed_ShouldThrowExactly()
    {
        var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        hasher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
            hasher.ComputeHash(MakeData(4).AsSpan()));
    }

    /// <summary>
    /// Verifies that <c>ComputeHashAsync(Stream)</c> after <c>Dispose</c> raises
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public async Task ComputeHashAsync_WhenInstanceIsDisposed_ShouldThrowExactly()
    {
        var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
        hasher.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
            await hasher.ComputeHashAsync(new MemoryStream(MakeData(4))));
    }
}
