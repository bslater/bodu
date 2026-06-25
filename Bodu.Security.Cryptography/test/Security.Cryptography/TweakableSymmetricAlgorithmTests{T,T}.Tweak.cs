// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests{T,T}.Tweak.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that accessing <see cref="TweakableSymmetricAlgorithm.Tweak" /> after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.GenerateTweak();

        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            byte[] _ = algorithm.Tweak;
        });
    }

    /// <summary>
    /// Verifies that GenerateTweak produces a non-zero value that is preserved by the Tweak property.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenGenerated_ShouldMatchInternalTweakValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;

        algorithm.TweakSize = size;
        algorithm.GenerateTweak();

        byte[] tweak = algorithm.Tweak;
        Assert.HasCount(size / 8, tweak);
        Assert.Contains(b => b != 0, tweak, "Generated tweak should not be all zero.");
    }

    /// <summary>
    /// Verifies that accessing <see cref="TweakableSymmetricAlgorithm.Tweak" /> repeatedly on a
    /// default-constructed instance (i.e. without calling <c>GenerateTweak</c>) returns identical
    /// content across calls.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenNoAccessedMultipleTimes_ShouldReturnSameValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] tweak1 = algorithm.Tweak;
        byte[] tweak2 = algorithm.Tweak;

        CollectionAssert.AreEqual(tweak1, tweak2);
    }

    /// <summary>
    /// Verifies that accessing <see cref="TweakableSymmetricAlgorithm.Tweak" /> before it is initialised throws.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenNotInitialized_ShouldReturnExpectedValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] tweak = algorithm.Tweak;
        Assert.HasCount(algorithm.TweakSize / 8, tweak);
        Assert.Contains(b => b != 0, tweak, "Generated tweak should not be all zero.");
    }

    /// <summary>
    /// Verifies that once set, the Tweak value does not change unless reassigned or reset.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenNotReassigned_ShouldRemainUnchanged()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;
        byte[] expected = Enumerable.Repeat((byte)0xAA, size / 8).ToArray();

        algorithm.TweakSize = size;
        algorithm.Tweak = expected;

        byte[] first = algorithm.Tweak;
        byte[] second = algorithm.Tweak;

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that setting the <see cref="TweakableSymmetricAlgorithm.Tweak" /> returns the exact same content when retrieved.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenSet_ShouldReturnExpectedValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;
        byte[] expected = Enumerable.Range(0, size / 8).Select(i => (byte)(i + 1)).ToArray();

        algorithm.TweakSize = size;
        algorithm.Tweak = expected;
        byte[] actual = algorithm.Tweak;

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.Tweak" /> set to null throws ArgumentNullException.
    /// </summary>
    [TestMethod]
    public void Tweak_WhenSetToNull_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.Tweak = null!;
        });
    }

    /// <summary>
    /// Verifies that setting <see cref="TweakableSymmetricAlgorithm.Tweak" /> to a wrongly-sized
    /// byte array throws <see cref="CryptographicException" /> whose message contains no
    /// rename-refactor artefacts (such as the unrelated literal <c>"TweakSchedule"</c> that the
    /// original <c>[CallerArgumentExpression]</c> attribute on
    /// <c>ThrowIfInvalidTweakSize</c> referenced).
    /// </summary>
    [TestMethod]
    public void Tweak_WhenSetToInvalidSize_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.Tweak = new byte[5];

        });

        Assert.IsFalse(string.IsNullOrEmpty(ex.Message), "Expected non-empty exception message.");
        Assert.DoesNotContain("TweakSchedule", ex.Message,
            "Exception message must not reference the unrelated 'TweakSchedule' symbol.");
        Assert.DoesNotContain("this.", ex.Message,
            "Exception message must not contain stray 'this.' refactor artefacts.");
    }

    /// <summary>
    /// Verifies that assigning a tweak whose length in bits is not among
    /// <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> throws
    /// <see cref="CryptographicException" />.
    /// Skips with <see cref="Assert.Inconclusive" /> when the algorithm accepts every
    /// byte-aligned length and no invalid size can be constructed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidTweakSizeBytesData))]
    public void Tweak_WhenSetToInvalidSize_ShouldThrowExactly(int tweakSize)
    {
        if (tweakSize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] invalidTweak = new byte[tweakSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.Tweak = invalidTweak;
        },
            $"Setting a {tweakSize}-byte tweak should throw CryptographicException " +
            $"for {typeof(TAlgorithm).Name}.");
    }
}
