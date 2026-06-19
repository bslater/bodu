// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests{T,T}.LegalKeySizes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the cipher's default <see cref="SymmetricStreamAlgorithm.KeySize" /> is reported as one of its
    /// <see cref="SymmetricStreamAlgorithm.LegalKeySizes" />.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenQueried_ShouldContainDefaultKeySize()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        KeySizes[] legal = cipher.LegalKeySizes;

        bool covered = false;
        foreach (KeySizes size in legal)
        {
            if (cipher.KeySize < size.MinSize || cipher.KeySize > size.MaxSize)
                continue;

            covered = size.SkipSize == 0
                ? cipher.KeySize == size.MinSize
                : (cipher.KeySize - size.MinSize) % size.SkipSize == 0;

            if (covered)
                break;
        }

        Assert.IsTrue(covered, "The default key size should be one of the legal key sizes.");
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.LegalKeySizes" /> returns a new array instance each call, so
    /// mutating the returned array cannot affect the cipher.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenQueriedTwice_ShouldReturnIndependentArrays()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreNotSame(cipher.LegalKeySizes, cipher.LegalKeySizes);
    }

    /// <summary>
    /// Verifies that every <see cref="KeySizes" /> entry declared on the cipher has a non-decreasing
    /// <see cref="KeySizes.MinSize" />/<see cref="KeySizes.MaxSize" /> range and a non-negative
    /// <see cref="KeySizes.SkipSize" />.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenDefined_ShouldHaveValidRanges()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        foreach (KeySizes keySize in cipher.LegalKeySizes)
        {
            Assert.IsTrue(keySize.MinSize <= keySize.MaxSize, "MinSize must be less than or equal to MaxSize.");
            Assert.IsTrue(keySize.SkipSize >= 0, "SkipSize must be greater than or equal to zero.");
        }
    }

    /// <summary>
    /// Verifies that the discrete key sizes enumerated by <see cref="SymmetricStreamAlgorithm.LegalKeySizes" /> are
    /// non-overlapping and unique across all declared ranges.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenDefined_ShouldHaveNonOverlappingValues()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        HashSet<int> uniqueSizes = [];

        foreach (KeySizes keySize in cipher.LegalKeySizes)
        {
            for (int size = keySize.MinSize; size <= keySize.MaxSize; size += keySize.SkipSize == 0 ? int.MaxValue : keySize.SkipSize)
            {
                Assert.IsTrue(uniqueSizes.Add(size), $"Duplicate or overlapping key size detected: {size}.");
            }
        }
    }
}
