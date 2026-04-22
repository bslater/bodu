// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.TweakSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that getting the <see cref="TweakableSymmetricAlgorithm.Tweak" /> returns a new array each time.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenGetMultipleTimes_ShouldReturnDistinctCopies()
    {
        using var algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.TweakSize = size;
        algorithm.GenerateTweak();
        byte[] first = algorithm.Tweak;
        byte[] second = algorithm.Tweak;
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
        using var algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;
        byte[] original = Enumerable.Range(0, size / 8).Select(i => (byte)i).ToArray();
        byte[] input = (byte[])original.Clone();
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
        using var algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;

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
        using var algorithm = CreateAlgorithm();
        int validSize = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.TweakSize = validSize;
        Assert.AreEqual(validSize, algorithm.TweakSize);
    }
}
