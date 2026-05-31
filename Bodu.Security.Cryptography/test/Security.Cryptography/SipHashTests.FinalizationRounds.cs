// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.FinalizationRounds.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="SipHash.FinalizationRounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(32)]
    public void FinalizationRounds_WhenSetToValidValue_ShouldUpdateCorrectly(int size)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var original = algorithm.FinalizationRounds;
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
        var input = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        byte[] hashWithRounds4;
        byte[] hashWithRounds8;

        using (TAlgorithm algorithm = CreateAlgorithm())
        {
            algorithm.FinalizationRounds = 4;
            hashWithRounds4 = algorithm.ComputeHash(input);
        }

        using (TAlgorithm algorithm = CreateAlgorithm())
        {
            algorithm.FinalizationRounds = 8;
            hashWithRounds8 = algorithm.ComputeHash(input);
        }

        CollectionAssert.AreNotEqual(hashWithRounds4, hashWithRounds8, "Hashes should differ when finalization rounds are different.");
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
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.FinalizationRounds = value;
        });
    }
}
