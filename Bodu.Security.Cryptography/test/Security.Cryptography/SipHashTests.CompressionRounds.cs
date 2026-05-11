// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.CompressionRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="SipHash.CompressionRounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    public void CompressionRounds_WhenSetToValidValue_ShouldUpdateCorrectly(int size)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
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

        using (TAlgorithm algorithm = CreateAlgorithm())
        {
            algorithm.CompressionRounds = 4;
            hashWithRounds4 = algorithm.ComputeHash(input);
        }

        using (TAlgorithm algorithm = CreateAlgorithm())
        {
            algorithm.CompressionRounds = 8;
            hashWithRounds8 = algorithm.ComputeHash(input);
        }

        CollectionAssert.AreNotEqual(hashWithRounds4, hashWithRounds8, "Hashes should differ when compression rounds are different.");
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
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.CompressionRounds = value;
        });
    }
}
