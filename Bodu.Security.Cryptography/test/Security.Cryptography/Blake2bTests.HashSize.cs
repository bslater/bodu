// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bTests.HashSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Blake2bTests
{
    /// <summary>
    /// Verifies that assigning <see cref="Blake2b.HashSize" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently mutating
    /// the digest length mid-computation.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = new Blake2b();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.HashSize = 256;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="Blake2b.HashSize" /> to an unsupported value throws
    /// <see cref="ArgumentOutOfRangeException" /> with no side effects on the existing
    /// <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(513)]
    [DataRow(-1)]
    public void HashSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using var algorithm = new Blake2b();
        int originalSize = algorithm.HashSize;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.HashSize = value;
        });

        Assert.AreEqual(originalSize, algorithm.HashSize,
            "Failed assignment must not mutate HashSize.");
    }

    /// <summary>
    /// Verifies that changing <see cref="Blake2b.HashSize" /> after construction re-encodes the RFC 7693 parameter
    /// block, so the next digest matches the one produced by constructing the algorithm at the new size directly.
    /// Before the fix the setter left the parameter block baked at construction, yielding a wrong digest.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void HashSize_WhenChangedAfterConstruction_ShouldProduceDigestForNewSize()
    {
        byte[] message = System.Text.Encoding.ASCII.GetBytes("abc");

        using var reference = new Blake2b(512);
        byte[] expected = reference.ComputeHash(message);

        using var resized = new Blake2b(256);
        resized.HashSize = 512;
        byte[] actual = resized.ComputeHash(message);

        CollectionAssert.AreEqual(expected, actual,
            "A digest after a HashSize change must match one produced at that size directly.");
    }
}
