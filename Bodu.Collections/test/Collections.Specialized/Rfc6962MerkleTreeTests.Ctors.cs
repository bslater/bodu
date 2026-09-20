// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Collections.Specialized;

/// <summary>
/// Tests for the <see cref="Rfc6962MerkleTree" /> constructors.
/// </summary>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>
    /// Verifies that the digest width is established at construction from the supplied algorithm.
    /// </summary>
    /// <param name="algorithmName">The algorithm to construct over.</param>
    /// <param name="expectedHashLength">The expected digest width, in bytes.</param>
    [TestMethod]
    [DataRow("SHA1", 20)]
    [DataRow("SHA256", 32)]
    [DataRow("SHA384", 48)]
    [DataRow("SHA512", 64)]
    public void Ctor_WhenGivenAFactory_ShouldExposeTheAlgorithmDigestWidth(string algorithmName, int expectedHashLength)
    {
        Func<HashAlgorithm> factory = algorithmName switch
        {
            "SHA1" => SHA1.Create,
            "SHA384" => SHA384.Create,
            "SHA512" => SHA512.Create,
            _ => SHA256.Create,
        };

        Assert.AreEqual(expectedHashLength, new Rfc6962MerkleTree(factory).HashLength);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> factory delegate is rejected with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFactoryDelegateIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Rfc6962MerkleTree(null!);
        });
    }

    /// <summary>
    /// Verifies that a factory returning <see langword="null" /> fails at construction rather than at the first
    /// computation, so the defect surfaces where it was introduced.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFactoryReturnsNull_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Rfc6962MerkleTree(() => null!);
        });

        Assert.AreEqual("algorithmFactory", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the instance is safe for concurrent use, as a verifier serving challenges from multiple
    /// connection handlers requires.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInstanceIsSharedAcrossThreads_ShouldProduceConsistentRoots()
    {
        Rfc6962MerkleTree tree = CreateTree();
        string expected = Hex(tree.ComputeRoot(TakeEntries(7)));

        string[] results = new string[64];
        Parallel.For(0, results.Length, index =>
        {
            results[index] = Hex(tree.ComputeRoot(TakeEntries(7)));
        });

        CollectionAssert.AreEqual(
            Enumerable.Repeat(expected, results.Length).ToArray(),
            results,
            "every concurrent computation must agree with the sequential root");
    }
}
