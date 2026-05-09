// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.CompressionRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary> <summary> Verifies that setting an invalid hashValue for <see cref="SipHash.CompressionRounds"/> throws <see
    /// cref="ArgumentOutOfRangeException"/>. </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void CompressionRounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.CompressionRounds = value;
        });
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="SipHash.CompressionRounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    public void CompressionRounds_WhenSetToValidValue_ShouldUpdateCorrectly(int size)
    {
        using var algorithm = CreateAlgorithm();
        int original = algorithm.CompressionRounds;
        algorithm.CompressionRounds = size;

        Assert.AreEqual(size, algorithm.CompressionRounds);
        Assert.AreNotEqual(original, algorithm.CompressionRounds);
    }

    /// <summary>
    /// Verifies that using different <see cref="SipHash.CompressionRounds" /> values results in different hash outputs for the same input.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenDifferent_ShouldProduceDifferentHash()
    {
        byte[] input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        byte[] hashWithRounds4;
        byte[] hashWithRounds8;

        using (var algorithm = CreateAlgorithm())
        {
            algorithm.CompressionRounds = 4;
            hashWithRounds4 = algorithm.ComputeHash(input);
        }

        using (var algorithm = CreateAlgorithm())
        {
            algorithm.CompressionRounds = 8;
            hashWithRounds8 = algorithm.ComputeHash(input);
        }

        CollectionAssert.AreNotEqual(hashWithRounds4, hashWithRounds8, "Hashes should differ when compression rounds are different.");
    }

    /// <summary>
    /// Verifies that reading <see cref="SipHash{T}.CompressionRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning the cleared (0) backing field.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CompressionRounds;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.CompressionRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than silently mutating cleared state.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenSetAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.CompressionRounds = 4;
        });
    }

    /// <summary>
    /// Verifies that mutating <see cref="SipHash{T}.CompressionRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently
    /// reconfiguring the round count mid-computation.
    /// </summary>
    [TestMethod]
    public void CompressionRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[16];
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.CompressionRounds = 4;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.CompressionRounds" /> to extreme values around
    /// the boundary throws <see cref="ArgumentOutOfRangeException" /> only for values strictly below
    /// <see cref="SipHash{T}.MinCompressionRounds" /> (and never some other unexpected exception).
    /// </summary>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void CompressionRounds_WhenSetBelowMinimum_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.CompressionRounds = value;
        });
    }
}
