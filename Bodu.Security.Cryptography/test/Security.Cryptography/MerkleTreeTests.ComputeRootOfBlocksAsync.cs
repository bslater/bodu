// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.ComputeRootOfBlocksAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleTree.ComputeRootOfBlocksAsync(Stream, int, MerkleTreeDiagnostics, CancellationToken)" />.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that the asynchronous root-only computation reproduces the published block-mode roots at every degree.
    /// </summary>
    /// <param name="kat">The input length and the published root.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public async Task ComputeRootOfBlocksAsync_WhenReadingAStream_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        foreach (int degree in AllDegrees)
        {
            using var stream = new MemoryStream(BlockModeInput(kat.Input));

            Assert.AreEqual(kat.Expected, Hex(await CreateParallelTree(degree).ComputeRootOfBlocksAsync(stream, VectorBlockSize)), $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that the asynchronous root-only computation agrees with the leaf-retaining asynchronous computation
    /// and with the synchronous root across a batch boundary.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeRootOfBlocksAsync_WhenComparedWithTheOtherPaths_ShouldAgree()
    {
        byte[] input = BlockModeInput(1025);
        string expected = Hex(CreateTree().ComputeRootOfBlocks(input.AsSpan(), 1));

        foreach (int degree in AllDegrees)
        {
            MerkleTree tree = CreateParallelTree(degree);

            Assert.AreEqual(expected, Hex(await tree.ComputeRootOfBlocksAsync(new MemoryStream(input), 1)), $"root-only, degree {degree}");
            Assert.AreEqual(expected, Hex((await tree.ComputeBlockedAsync(new MemoryStream(input), 1)).Root), $"blocked, degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a supplied recorder receives a validating trace ending at the asynchronous root.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeRootOfBlocksAsync_WhenDiagnosticsAreSupplied_ShouldRecordAValidatingTraceEndingAtTheRoot()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var stream = new MemoryStream(BlockModeInput(37));

        byte[] root = await CreateParallelTree(-1).ComputeRootOfBlocksAsync(stream, VectorBlockSize, diagnostics);

        Assert.IsTrue(diagnostics.Validate(SHA256.Create, out IReadOnlyList<string> errors), string.Join("; ", errors));
        Assert.IsNotNull(diagnostics.Root);
        Assert.AreEqual(Hex(root), Hex(diagnostics.Root.Hash));
        Assert.AreEqual(10, diagnostics.GetLevel(0).Count);
    }

    /// <summary>
    /// Verifies that a pre-cancelled token surfaces as a plain <see cref="OperationCanceledException" /> at every
    /// degree.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeRootOfBlocksAsync_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        foreach (int degree in AllDegrees)
        {
            MerkleTree tree = CreateParallelTree(degree);
            using var stream = new MemoryStream(BlockModeInput(256));

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            {
                _ = await tree.ComputeRootOfBlocksAsync(stream, VectorBlockSize, cancellationToken: cts.Token);
            });
        }
    }

    /// <summary>
    /// Verifies that an empty stream yields the empty tree's root asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task ComputeRootOfBlocksAsync_WhenStreamIsEmpty_ShouldReturnTheEmptyTreeRoot()
    {
        Assert.AreEqual(Hex(SHA256.HashData([])), Hex(await CreateTree().ComputeRootOfBlocksAsync(new MemoryStream(), VectorBlockSize)));
    }
}
