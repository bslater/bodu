// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.VerifyInclusionBound.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for
/// <see cref="MerkleTree.VerifyInclusionBound(ReadOnlySpan{byte}, long, long, long, ReadOnlySpan{byte}, IReadOnlyList{ReadOnlyMemory{byte}})" />,
/// including the tree-size ambiguity it exists to close.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that the unbound verifier accepts an <em>understated</em> tree size against a four-block tree's
    /// first path, and that the bound verifier rejects the same claim.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the requirements document's appendix D, and both halves are required behaviour. The acceptance on line
    /// one is <strong>not a defect to be fixed</strong> — it is RFC 6962's verifier working exactly as specified. A
    /// four-block tree's path for block 0 has precisely the length a three-block tree's first path wants and walks to
    /// the same head, so the size check alone cannot tell them apart.
    /// </para>
    /// <para>
    /// The consequence is concrete: a holder of a four-block object that has lost block 3 could declare a three-block
    /// object, never be asked for block 3, and answer every challenge for ever. Binding the byte length into the
    /// published root is what closes it, because a disagreeing length produces a different root.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void VerifyInclusionBound_WhenTreeSizeIsUnderstated_ShouldRejectWhereTheUnboundVerifierAccepts()
    {
        MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(16);
        IReadOnlyList<ReadOnlyMemory<byte>> entries = BlockModeEntries(16);

        byte[] head = tree.ComputeRoot(entries);
        IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, 0));
        ReadOnlySpan<byte> block0 = input.AsSpan(0, 4);

        Assert.AreEqual("516c43cb9e4f82fe70437703f0a41649c5fd963ba5f4be8b736dca20bae05bdd", Hex(head));
        CollectionAssert.AreEqual(
            new[]
            {
                "5ab0680027d25adbc39635ae41b3bfdbd9b3c4bd829899d1ba9c845155e604f0",
                "d5ebbc39e8bd0dceb9f7120ba588f91a386714bf51e320373f667927231666da",
            },
            path.Select(step => Hex(step.Span)).ToArray(),
            "the published appendix D path");

        // The unbound walk accepts the true size and the understated one alike.
        Assert.IsTrue(tree.VerifyInclusion(head, 4, 0, block0, path), "the true size must verify");
        Assert.IsTrue(
            tree.VerifyInclusion(head, 3, 0, block0, path),
            "RFC 6962's verifier accepts the understated size; this is specified behaviour, not a defect");

        // The bound root names one tree and no other.
        byte[] boundSixteen = tree.BindRoot(head, 16);
        Assert.AreEqual("f5ed505e0cb1f0f1cb6909f10211fefe06a7d976b18f9c38e9bdf497802f2b04", Hex(boundSixteen));

        Assert.IsTrue(tree.VerifyInclusionBound(boundSixteen, 16, 4, 0, block0, path), "the true length must verify");
        Assert.IsFalse(
            tree.VerifyInclusionBound(boundSixteen, 12, 3, 0, block0, path),
            "understating the length by a whole block must fail closed");
        Assert.IsFalse(tree.VerifyInclusionBound(boundSixteen, 15, 4, 0, block0, path));
        Assert.IsFalse(tree.VerifyInclusionBound(boundSixteen, 17, 4, 0, block0, path));
    }

    /// <summary>
    /// Verifies that the bound roots of a sixteen-byte and a twelve-byte input differ, which is the arithmetic the
    /// fail-closed behaviour rests on.
    /// </summary>
    [TestMethod]
    public void VerifyInclusionBound_WhenComparedAcrossLengths_ShouldProduceDistinctBoundRoots()
    {
        MerkleTree tree = CreateTree();

        string boundSixteen = Hex(tree.BindRoot(tree.ComputeRoot(BlockModeEntries(16)), 16));
        string boundTwelve = Hex(tree.BindRoot(tree.ComputeRoot(BlockModeEntries(12)), 12));

        Assert.AreEqual("f5ed505e0cb1f0f1cb6909f10211fefe06a7d976b18f9c38e9bdf497802f2b04", boundSixteen);
        Assert.AreEqual("29f46d734672b404873bd2548b2ab1cf66cf22c5c412fb150331b344cd50332a", boundTwelve);
        Assert.AreNotEqual(boundSixteen, boundTwelve);
    }

    /// <summary>
    /// Verifies that a valid proof verifies against the bound root for every leaf of every tree size from one to
    /// thirty-two, binding the entry count.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void VerifyInclusionBound_WhenBindingTheEntryCount_ShouldRoundTrip()
    {
        MerkleTree tree = CreateTree();

        for (int treeSize = 1; treeSize <= 32; treeSize++)
        {
            ReadOnlyMemory<byte>[] entries = new ReadOnlyMemory<byte>[treeSize];
            for (int index = 0; index < treeSize; index++)
                entries[index] = new byte[] { (byte)index, 0x5A };

            byte[] bound = tree.BindRoot(tree.ComputeRoot(entries), treeSize);

            for (int leafIndex = 0; leafIndex < treeSize; leafIndex++)
            {
                IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, leafIndex));

                Assert.IsTrue(
                    tree.VerifyInclusionBound(bound, treeSize, treeSize, leafIndex, entries[leafIndex].Span, path),
                    $"size {treeSize} leaf {leafIndex} must verify against the bound root");

                Assert.IsFalse(
                    tree.VerifyInclusionBound(bound, treeSize + 1, treeSize, leafIndex, entries[leafIndex].Span, path),
                    $"size {treeSize} leaf {leafIndex} must reject a bound value the publisher did not commit to");
            }
        }
    }

    /// <summary>
    /// Verifies that a negative bound value is rejected rather than throwing, since it can arrive as a wire value.
    /// </summary>
    [TestMethod]
    public void VerifyInclusionBound_WhenBoundValueIsNegative_ShouldReject()
    {
        MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyInclusionBound(new byte[32], -1, 1, 0, [0x61], []));
    }

    /// <summary>
    /// Verifies that a bound root of the wrong width is rejected rather than throwing.
    /// </summary>
    [TestMethod]
    public void VerifyInclusionBound_WhenBoundRootIsNotDigestWidth_ShouldReject()
    {
        MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyInclusionBound(new byte[16], 1, 1, 0, [0x61], []));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> path throws rather than being treated as an empty one.
    /// </summary>
    [TestMethod]
    public void VerifyInclusionBound_WhenPathIsNull_ShouldThrowArgumentNullException()
    {
        MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.VerifyInclusionBound(new byte[32], 1, 1, 0, [0x61], null!);
        });
    }
}
