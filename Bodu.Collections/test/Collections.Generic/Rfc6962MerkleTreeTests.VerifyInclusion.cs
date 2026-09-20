// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.VerifyInclusion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic;

/// <summary>
/// Tests for
/// <see cref="Rfc6962MerkleTree.VerifyInclusion(ReadOnlySpan{byte}, long, long, ReadOnlySpan{byte}, IReadOnlyList{ReadOnlyMemory{byte}})" />
/// and its leaf-hash overload.
/// </summary>
/// <remarks>
/// The negative cases follow the systematic mutation matrix that production RFC 6962 implementations use — corrupt
/// the index, the tree size, the root, the leaf, and every position of the path — rather than a fixed handful of
/// scenarios, because a verifier's failures hide in the cases nobody thought to enumerate.
/// </remarks>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>
    /// Verifies that a published path carries its leaf to the published root.
    /// </summary>
    /// <param name="kat">The tree size, leaf index and path.</param>
    [TestMethod]
    [DynamicData(nameof(ReferenceInclusionPaths), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void VerifyInclusion_WhenGivenAPublishedPath_ShouldAccept(MerkleInclusionKat kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(TakeEntries(kat.TreeSize));
        IReadOnlyList<ReadOnlyMemory<byte>> path = kat.Path.Select(step => (ReadOnlyMemory<byte>)Convert.FromHexString(step)).ToArray();

        Assert.IsTrue(tree.VerifyInclusion(root, kat.TreeSize, kat.LeafIndex, ReferenceEntries[kat.LeafIndex], path));
    }

    /// <summary>
    /// Verifies that a generated path verifies for every leaf of every tree size from one to sixty-four.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void VerifyInclusion_WhenPathIsGeneratedForAnyLeaf_ShouldRoundTrip()
    {
        Rfc6962MerkleTree tree = CreateTree();

        for (int treeSize = 1; treeSize <= 64; treeSize++)
        {
            ReadOnlyMemory<byte>[] entries = new ReadOnlyMemory<byte>[treeSize];
            for (int index = 0; index < treeSize; index++)
                entries[index] = new byte[] { (byte)index, (byte)(index >> 8) };

            byte[] root = tree.ComputeRoot(entries);

            for (int leafIndex = 0; leafIndex < treeSize; leafIndex++)
            {
                IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, leafIndex));

                Assert.IsTrue(
                    tree.VerifyInclusion(root, treeSize, leafIndex, entries[leafIndex].Span, path),
                    $"size {treeSize} leaf {leafIndex} must verify");
            }
        }
    }

    /// <summary>
    /// Verifies that a one-entry tree's empty path verifies against the entry's own leaf hash as the root.
    /// </summary>
    [TestMethod]
    public void VerifyInclusion_WhenTreeHasOneEntryAndPathIsEmpty_ShouldAccept()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] entry = [0x61, 0x62, 0x63];

        Assert.IsTrue(tree.VerifyInclusion(tree.HashLeaf(entry), 1, 0, entry, []));
    }

    /// <summary>
    /// Verifies that a one-entry tree rejects an empty path against a root that is not the entry's leaf hash.
    /// </summary>
    [TestMethod]
    public void VerifyInclusion_WhenTreeHasOneEntryAndRootIsWrong_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyInclusion(tree.HashLeaf([0x61]), 1, 0, [0x62], []));
    }

    /// <summary>
    /// Verifies that leaf 6 of a seven-entry tree verifies on its two-step path, so a short path in a non-perfect
    /// tree is accepted rather than mistaken for a truncated one.
    /// </summary>
    [TestMethod]
    public void VerifyInclusion_WhenPathIsShortBecauseTheTreeIsNotPerfect_ShouldAccept()
    {
        Rfc6962MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(7);
        IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, 6));

        Assert.AreEqual(2, path.Count);
        Assert.IsTrue(tree.VerifyInclusion(tree.ComputeRoot(entries), 7, 6, ReferenceEntries[6], path));
    }

    /// <summary>
    /// Verifies that every systematic corruption of a valid proof is rejected, across every leaf of several tree
    /// sizes, and that none of them throws.
    /// </summary>
    /// <param name="treeSize">The number of entries in the tree.</param>
    /// <remarks>
    /// The mutations are the ones a production RFC 6962 verifier is held to: shifted and flipped leaf indices, doubled
    /// and halved tree sizes, an empty and a wrong root, a wrong leaf, garbage and the root itself prepended or
    /// appended to the path, each step bit-flipped, each step removed, a step inserted at each position, and a step of
    /// the wrong width. Injecting the <em>root</em> as a path step is included deliberately: a verifier that treats it
    /// as an ordinary sibling would otherwise accept.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    public void VerifyInclusion_WhenProofIsCorrupted_ShouldRejectWithoutThrowing(int treeSize)
    {
        Rfc6962MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(treeSize);
        byte[] root = tree.ComputeRoot(entries);

        for (int leafIndex = 0; leafIndex < treeSize; leafIndex++)
        {
            byte[][] valid = tree.AuthenticationPath(entries, leafIndex);

            foreach ((string label, Func<bool> probe) in CorruptInclusionProbes(tree, root, treeSize, leafIndex, valid))
            {
                try
                {
                    Assert.IsFalse(probe(), $"size {treeSize} leaf {leafIndex} [{label}] must be rejected");
                }
                catch (AssertFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Assert.Fail($"size {treeSize} leaf {leafIndex} [{label}] threw {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a correct path presented against a different leaf's bytes is rejected, which is the property the
    /// possession challenge rests on: the path alone proves nothing.
    /// </summary>
    [TestMethod]
    public void VerifyInclusion_WhenPathIsCorrectButEntryIsAnotherLeafs_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(8);
        byte[] root = tree.ComputeRoot(entries);
        IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, 3));

        Assert.IsTrue(tree.VerifyInclusion(root, 8, 3, ReferenceEntries[3], path));
        Assert.IsFalse(tree.VerifyInclusion(root, 8, 3, ReferenceEntries[4], path));
    }

    /// <summary>
    /// Verifies that a zero tree size is rejected for any index rather than throwing, since it can arrive as a wire
    /// value.
    /// </summary>
    /// <param name="leafIndex">The claimed leaf index.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(-1L)]
    public void VerifyInclusion_WhenTreeSizeIsZero_ShouldReject(long leafIndex)
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyInclusion(new byte[32], 0, leafIndex, [0x61], []));
    }

    /// <summary>
    /// Verifies that a root of the wrong width is rejected rather than throwing.
    /// </summary>
    /// <param name="rootLength">The wrong root length, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    [DataRow(64)]
    public void VerifyInclusion_WhenRootIsNotDigestWidth_ShouldReject(int rootLength)
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyInclusion(new byte[rootLength], 1, 0, [0x61], []));
    }

    /// <summary>
    /// Verifies that an internal node offered as the proved entry is rejected — the second-preimage attack applied to
    /// proof verification, where an attacker presents a node's preimage as a leaf and truncates the path.
    /// </summary>
    [TestMethod]
    public void VerifyInclusion_WhenEntryIsAnInternalNodePreimage_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(4);
        byte[] root = tree.ComputeRoot(entries);

        // The left subtree's node preimage: the concatenated leaf hashes of entries 0 and 1.
        byte[] nodePreimage = [.. tree.HashLeaf(ReferenceEntries[0]), .. tree.HashLeaf(ReferenceEntries[1])];

        // Its sibling is the right subtree's root, which would be the only step a two-leaf tree's path needs.
        byte[] rightSubtree = tree.HashNode(tree.HashLeaf(ReferenceEntries[2]), tree.HashLeaf(ReferenceEntries[3]));

        Assert.IsFalse(tree.VerifyInclusion(root, 2, 0, nodePreimage, [rightSubtree]));
        Assert.IsFalse(tree.VerifyInclusion(root, 4, 0, nodePreimage, [rightSubtree]));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> path throws, because a null argument is a caller error rather than a
    /// value that can arrive over a wire.
    /// </summary>
    [TestMethod]
    public void VerifyInclusion_WhenPathIsNull_ShouldThrowArgumentNullException()
    {
        Rfc6962MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.VerifyInclusion(new byte[32], 1, 0, [0x61], null!);
        });
    }

    /// <summary>
    /// Verifies that the leaf-hash overload accepts the same proofs as the entry overload.
    /// </summary>
    [TestMethod]
    public void VerifyInclusionOfLeafHash_WhenGivenAValidProof_ShouldAgreeWithTheEntryOverload()
    {
        Rfc6962MerkleTree tree = CreateTree();
        IReadOnlyList<ReadOnlyMemory<byte>> entries = TakeEntries(7);
        byte[] root = tree.ComputeRoot(entries);

        for (int leafIndex = 0; leafIndex < 7; leafIndex++)
        {
            IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(tree.AuthenticationPath(entries, leafIndex));

            Assert.IsTrue(
                tree.VerifyInclusionOfLeafHash(
                    root, 7, leafIndex, tree.HashLeaf(ReferenceEntries[leafIndex]), path),
                $"leaf {leafIndex}");
        }
    }

    /// <summary>
    /// Verifies that the leaf-hash overload rejects a leaf hash of the wrong width rather than throwing.
    /// </summary>
    [TestMethod]
    public void VerifyInclusionOfLeafHash_WhenLeafHashIsNotDigestWidth_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyInclusionOfLeafHash(new byte[32], 1, 0, new byte[16], []));
    }

    /// <summary>
    /// Enumerates every corruption of a valid inclusion proof, each paired with a label for failure reporting.
    /// </summary>
    /// <param name="tree">The tree to verify with.</param>
    /// <param name="root">The correct root.</param>
    /// <param name="treeSize">The correct tree size.</param>
    /// <param name="leafIndex">The correct leaf index.</param>
    /// <param name="valid">The correct path.</param>
    /// <returns>The labelled probes, each of which must return <see langword="false" />.</returns>
    private static IEnumerable<(string Label, Func<bool> Probe)> CorruptInclusionProbes(
        Rfc6962MerkleTree tree,
        byte[] root,
        int treeSize,
        int leafIndex,
        byte[][] valid)
    {
        byte[] leaf = ReferenceEntries[leafIndex];
        IReadOnlyList<ReadOnlyMemory<byte>> path = ToPath(valid);

        // Corrupt the leaf index.
        yield return ("leafIndex - 1", () => tree.VerifyInclusion(root, treeSize, leafIndex - 1, leaf, path));
        yield return ("leafIndex + 1", () => tree.VerifyInclusion(root, treeSize, leafIndex + 1, leaf, path));
        yield return ("leafIndex ^ 2", () => tree.VerifyInclusion(root, treeSize, leafIndex ^ 2, leaf, path));

        // Corrupt the tree size.
        yield return ("treeSize * 2", () => tree.VerifyInclusion(root, treeSize * 2L, leafIndex, leaf, path));
        yield return ("treeSize / 2", () => tree.VerifyInclusion(root, treeSize / 2L, leafIndex, leaf, path));
        yield return ("treeSize = 0", () => tree.VerifyInclusion(root, 0, leafIndex, leaf, path));

        // Corrupt the root.
        yield return ("empty root", () => tree.VerifyInclusion(new byte[root.Length], treeSize, leafIndex, leaf, path));
        yield return ("wrong root", () => tree.VerifyInclusion(Flip(root, 0), treeSize, leafIndex, leaf, path));

        // Corrupt the leaf.
        yield return ("wrong leaf", () => tree.VerifyInclusion(root, treeSize, leafIndex, Flip(leaf.Length == 0 ? [0x00] : leaf, 0), path));

        // Inject extra steps, including the root itself, before and after the path.
        yield return ("trailing garbage", () => tree.VerifyInclusion(root, treeSize, leafIndex, leaf, Append(valid, new byte[root.Length])));
        yield return ("trailing root", () => tree.VerifyInclusion(root, treeSize, leafIndex, leaf, Append(valid, root)));
        yield return ("preceding garbage", () => tree.VerifyInclusion(root, treeSize, leafIndex, leaf, Prepend(valid, new byte[root.Length])));
        yield return ("preceding root", () => tree.VerifyInclusion(root, treeSize, leafIndex, leaf, Prepend(valid, root)));

        for (int step = 0; step < valid.Length; step++)
        {
            int captured = step;

            yield return ($"modified proof[{captured}] bit 3", () =>
                tree.VerifyInclusion(root, treeSize, leafIndex, leaf, Replace(valid, captured, Flip(valid[captured], 3))));

            yield return ($"removed proof[{captured}]", () =>
                tree.VerifyInclusion(root, treeSize, leafIndex, leaf, RemoveAt(valid, captured)));

            yield return ($"inserted at proof[{captured}]", () =>
                tree.VerifyInclusion(root, treeSize, leafIndex, leaf, InsertAt(valid, captured, valid[captured])));

            yield return ($"proof[{captured}] wrong width", () =>
                tree.VerifyInclusion(root, treeSize, leafIndex, leaf, Replace(valid, captured, valid[captured][..16])));
        }
    }

    /// <summary>Returns a copy of <paramref name="value" /> with one bit flipped.</summary>
    /// <param name="value">The bytes to copy.</param>
    /// <param name="bit">The bit position to flip within the first byte.</param>
    /// <returns>The altered copy.</returns>
    private static byte[] Flip(ReadOnlySpan<byte> value, int bit)
    {
        byte[] copy = value.ToArray();
        copy[0] ^= (byte)(1 << bit);
        return copy;
    }

    /// <summary>Returns the path with <paramref name="step" /> appended.</summary>
    /// <param name="path">The original path.</param>
    /// <param name="step">The step to append.</param>
    /// <returns>The extended path.</returns>
    private static IReadOnlyList<ReadOnlyMemory<byte>> Append(byte[][] path, byte[] step) =>
        ToPath([.. path, step]);

    /// <summary>Returns the path with <paramref name="step" /> prepended.</summary>
    /// <param name="path">The original path.</param>
    /// <param name="step">The step to prepend.</param>
    /// <returns>The extended path.</returns>
    private static IReadOnlyList<ReadOnlyMemory<byte>> Prepend(byte[][] path, byte[] step) =>
        ToPath([step, .. path]);

    /// <summary>Returns the path with the step at <paramref name="index" /> replaced.</summary>
    /// <param name="path">The original path.</param>
    /// <param name="index">The step to replace.</param>
    /// <param name="step">The replacement step.</param>
    /// <returns>The altered path.</returns>
    private static IReadOnlyList<ReadOnlyMemory<byte>> Replace(byte[][] path, int index, byte[] step)
    {
        byte[][] copy = (byte[][])path.Clone();
        copy[index] = step;
        return ToPath(copy);
    }

    /// <summary>Returns the path with the step at <paramref name="index" /> removed.</summary>
    /// <param name="path">The original path.</param>
    /// <param name="index">The step to remove.</param>
    /// <returns>The shortened path.</returns>
    private static IReadOnlyList<ReadOnlyMemory<byte>> RemoveAt(byte[][] path, int index)
    {
        List<byte[]> copy = [.. path];
        copy.RemoveAt(index);
        return ToPath(copy);
    }

    /// <summary>Returns the path with <paramref name="step" /> inserted at <paramref name="index" />.</summary>
    /// <param name="path">The original path.</param>
    /// <param name="index">The position to insert at.</param>
    /// <param name="step">The step to insert.</param>
    /// <returns>The extended path.</returns>
    private static IReadOnlyList<ReadOnlyMemory<byte>> InsertAt(byte[][] path, int index, byte[] step)
    {
        List<byte[]> copy = [.. path];
        copy.Insert(index, step);
        return ToPath(copy);
    }
}
