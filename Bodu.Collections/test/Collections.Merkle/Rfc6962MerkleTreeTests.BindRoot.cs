// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.BindRoot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Collections.Merkle;

/// <summary>
/// Tests for <see cref="Rfc6962MerkleTree.BindRoot(ReadOnlySpan{byte}, long)" />.
/// </summary>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>
    /// Verifies that binding the input's byte length into the block-mode root reproduces the published bound roots.
    /// </summary>
    /// <param name="kat">The input length and its expected bound root.</param>
    [TestMethod]
    [DynamicData(nameof(BoundRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void BindRoot_WhenBindingTheInputByteLength_ShouldReproducePublishedBoundRoots(ValidKat<int, string> kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(BlockModeEntries(kat.Input));

        Assert.AreEqual(kat.Expected, Hex(tree.BindRoot(root, kat.Input)));
    }

    /// <summary>
    /// Verifies that a bound root is the hash of the <c>0x02</c> prefix, the big-endian bound value and the tree
    /// head, so a consumer can reproduce it independently.
    /// </summary>
    [TestMethod]
    public void BindRoot_WhenGivenARoot_ShouldHashPrefixLengthAndHeadInOrder()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(TakeEntries(5));

        byte[] expectedPreimage = new byte[1 + sizeof(ulong) + root.Length];
        expectedPreimage[0] = 0x02;
        BinaryPrimitives.WriteUInt64BigEndian(expectedPreimage.AsSpan(1), 4_198_400UL);
        root.CopyTo(expectedPreimage.AsSpan(1 + sizeof(ulong)));

        Assert.AreEqual(Hex(SHA256.HashData(expectedPreimage)), Hex(tree.BindRoot(root, 4_198_400)));
    }

    /// <summary>
    /// Verifies that binding different values to the same tree head produces different roots, which is what makes an
    /// understated size fail closed.
    /// </summary>
    [TestMethod]
    public void BindRoot_WhenBoundValuesDiffer_ShouldProduceDifferentRoots()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(BlockModeEntries(16));

        Assert.AreNotEqual(Hex(tree.BindRoot(root, 16)), Hex(tree.BindRoot(root, 12)));
    }

    /// <summary>
    /// Verifies that a bound root is distinct from the unbound head it was derived from, so the two cannot be
    /// substituted for one another.
    /// </summary>
    [TestMethod]
    public void BindRoot_WhenBindingAnyValue_ShouldDifferFromTheUnboundHead()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(TakeEntries(4));

        Assert.AreNotEqual(Hex(root), Hex(tree.BindRoot(root, 4)));
    }

    /// <summary>
    /// Verifies that a root of the wrong width is rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void BindRoot_WhenRootIsNotDigestWidth_ShouldThrowArgumentException()
    {
        Rfc6962MerkleTree tree = CreateTree();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = tree.BindRoot(new byte[31], 1);
        });

        Assert.AreEqual("root", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative bound value is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void BindRoot_WhenBoundValueIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(TakeEntries(2));

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.BindRoot(root, -1);
        });

        Assert.AreEqual("boundValue", ex.ParamName);
    }
}
