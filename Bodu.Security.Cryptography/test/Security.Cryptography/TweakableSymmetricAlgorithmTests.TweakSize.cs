// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.TweakSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that getting the <see cref="TweakableSymmetricAlgorithm.Tweak" /> returns a new array each time.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenGetMultipleTimes_ShouldReturnDistinctCopies()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.TweakSize = size;
        algorithm.GenerateTweak();
        var first = algorithm.Tweak;
        var second = algorithm.Tweak;
        Assert.AreNotSame(first, second);
        CollectionAssert.AreEqual(first, second);
        first[0]++;
        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that setting the <see cref="TweakableSymmetricAlgorithm.Tweak" /> uses a defensive copy to isolate internal state.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenSet_ShouldBeIsolatedFromInput()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalTweakSizes[0].MinSize;
        var original = Enumerable.Range(0, size / 8).Select(i => (byte)i).ToArray();
        var input = (byte[])original.Clone();
        algorithm.TweakSize = size;
        algorithm.Tweak = input;
        input[0]++;
        CollectionAssert.AreEqual(original, algorithm.Tweak);
        CollectionAssert.AreNotEqual(input, algorithm.Tweak);
    }

    /// <summary>
    /// Verifies that setting <see cref="TweakableSymmetricAlgorithm.TweakSize" /> clears any previously configured tweak value.
    /// </summary>
    [TestMethod]
    public void TweakSize_WhenChanged_ShouldResetTweak()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalTweakSizes[0].MinSize;

        algorithm.TweakSize = size;
        algorithm.GenerateTweak();
        var previousTweak = algorithm.Tweak;

        algorithm.TweakSize = size; // Reassign same size to trigger reset
        CollectionAssert.AreNotEqual(previousTweak, algorithm.Tweak);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TweakSize" /> is correctly assigned when set to a valid value.
    /// </summary>
    [TestMethod]
    public void TweakSize_WhenSetToValidValue_ShouldUpdateInternalValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var validSize = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.TweakSize = validSize;
        Assert.AreEqual(validSize, algorithm.TweakSize);
    }

    /// <summary>
    /// Verifies that assigning <see cref="TweakableSymmetricAlgorithm.TweakSize" /> to <c>0</c> throws
    /// <see cref="CryptographicException" /> rather than leaving the algorithm in an invalid configuration.
    /// </summary>
    [TestMethod]
    public void TweakSize_WhenSetToZero_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.TweakSize = 0;
        });
    }

    /// <summary>
    /// Verifies that assigning a wrongly-sized <see cref="TweakableSymmetricAlgorithm.TweakSize" /> throws
    /// <see cref="CryptographicException" /> for the invalid bit-widths supplied by
    /// <see cref="InvalidTweakSizeBitsData" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidTweakSizeBitsData))]
    public void TweakSize_WhenSetToInvalidValue_ShouldThrowExactly(int tweakSize)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.TweakSize = tweakSize;
        });
    }
}
