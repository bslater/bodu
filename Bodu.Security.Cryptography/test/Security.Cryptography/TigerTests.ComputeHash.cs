// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that <see cref="Tiger.ComputeHash" />, when VariantIsDifferent, ProduceDifferentHash.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenVariantIsDifferent_ShouldProduceDifferentHash()
    {
        var variants = Enum.GetValues<Bodu.Security.Cryptography.TigerHashingVariant>().ToArray();
        if (variants.Length < 2)
            Assert.Inconclusive("Not enough variants to test.");

        byte[] input = new byte[0];
        var actual = new List<byte[]>();
        foreach (var variant in variants)
        {
            using var algorithm = CreateAlgorithm();
            algorithm.Variant = variant;

            actual.Add(algorithm.ComputeHash(input));
        }

        CollectionAssert.AllItemsAreUnique(actual, "Hash results should be unique for different variants.");
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale or zeroed digest.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.ComputeHash(new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same <see cref="Tiger" /> instance produce identical digests for identical
    /// input — guarding against state leakage across reuse.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }
}
