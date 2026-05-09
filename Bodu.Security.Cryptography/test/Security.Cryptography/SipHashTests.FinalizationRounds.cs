// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.FinalizationRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that setting an invalid hashValue for <see cref="SipHash.FinalizationRounds" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void FinalizationRounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.FinalizationRounds = value;
        });
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="SipHash.FinalizationRounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    public void FinalizationRounds_WhenSetToValidValue_ShouldUpdateCorrectly(int size)
    {
        using var algorithm = CreateAlgorithm();
        int original = algorithm.FinalizationRounds;
        algorithm.FinalizationRounds = size;

        Assert.AreEqual(size, algorithm.FinalizationRounds);
        Assert.AreNotEqual(original, algorithm.FinalizationRounds);
    }

    /// <summary>
    /// Verifies that using different <see cref="SipHash.FinalizationRounds" /> values results in different hash outputs for the same input.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenDifferent_ShouldProduceDifferentHash()
    {
        byte[] input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        byte[] hashWithRounds4;
        byte[] hashWithRounds8;

        using (var algorithm = CreateAlgorithm())
        {
            algorithm.FinalizationRounds = 4;
            hashWithRounds4 = algorithm.ComputeHash(input);
        }

        using (var algorithm = CreateAlgorithm())
        {
            algorithm.FinalizationRounds = 8;
            hashWithRounds8 = algorithm.ComputeHash(input);
        }

        CollectionAssert.AreNotEqual(hashWithRounds4, hashWithRounds8, "Hashes should differ when finalization rounds are different.");
    }

    /// <summary>
    /// Verifies that reading <see cref="SipHash{T}.FinalizationRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning the cleared (0) backing field.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.FinalizationRounds;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.FinalizationRounds" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than silently mutating cleared state.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.FinalizationRounds = 8;
        });
    }

    /// <summary>
    /// Verifies that mutating <see cref="SipHash{T}.FinalizationRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently
    /// reconfiguring the round count mid-computation.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[16];
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.FinalizationRounds = 8;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="SipHash{T}.FinalizationRounds" /> to extreme values around
    /// the boundary throws <see cref="ArgumentOutOfRangeException" /> only for values strictly below
    /// <see cref="SipHash{T}.MinFinalizationRounds" /> (and never some other unexpected exception).
    /// </summary>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void FinalizationRounds_WhenSetBelowMinimum_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.FinalizationRounds = value;
        });
    }
}
