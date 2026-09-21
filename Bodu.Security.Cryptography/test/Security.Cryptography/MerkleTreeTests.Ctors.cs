// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the <see cref="MerkleTree" /> constructors.
/// </summary>
public partial class MerkleTreeTests
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

        Assert.AreEqual(expectedHashLength, new MerkleTree(factory).HashLength);
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
            _ = new MerkleTree(null!);
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
            _ = new MerkleTree(() => null!);
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
        MerkleTree tree = CreateTree();
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

    /// <summary>
    /// Verifies that the default construction is RFC 6962's binary tree hashed on the calling thread.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsAreDefaulted_ShouldBeBinaryAndSequential()
    {
        MerkleTree tree = CreateTree();

        Assert.AreEqual(2, tree.FanOut);
        Assert.IsTrue(tree.IsBinary);
        Assert.AreEqual(1, tree.MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Verifies that the fan-out and degree of parallelism supplied at construction are exposed unchanged.
    /// </summary>
    /// <param name="fanOut">The fan-out to configure.</param>
    /// <param name="maxDegreeOfParallelism">The degree to configure.</param>
    [TestMethod]
    [DataRow(2, -1)]
    [DataRow(3, 1)]
    [DataRow(4, 8)]
    public void Ctor_WhenOptionsAreSupplied_ShouldExposeThem(int fanOut, int maxDegreeOfParallelism)
    {
        var tree = new MerkleTree(SHA256.Create, fanOut, maxDegreeOfParallelism);

        Assert.AreEqual(fanOut, tree.FanOut);
        Assert.AreEqual(fanOut == 2, tree.IsBinary);
        Assert.AreEqual(maxDegreeOfParallelism, tree.MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Verifies that a fan-out below two is rejected with the parameter named.
    /// </summary>
    /// <param name="fanOut">The invalid fan-out.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenFanOutIsBelowTwo_ShouldThrowArgumentOutOfRangeException(int fanOut)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MerkleTree(SHA256.Create, fanOut);
        });

        Assert.AreEqual("fanOut", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a degree of parallelism of zero or below minus one is rejected with the parameter named.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The invalid degree.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenDegreeOfParallelismIsInvalid_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MerkleTree(SHA256.Create, maxDegreeOfParallelism: maxDegreeOfParallelism);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }
}
