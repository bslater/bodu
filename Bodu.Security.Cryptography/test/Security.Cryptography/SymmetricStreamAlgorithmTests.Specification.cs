// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.Specification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> instance reports the key size declared in
    /// <see cref="SymmetricStreamAlgorithmSpecification.DefaultKeySizeBits" />.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricStreamAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.DefaultKeySizeBits, algorithm.KeySize);
    }

    /// <summary>
    /// Verifies that a freshly constructed <typeparamref name="TAlgorithm" /> instance reports the nonce size declared
    /// in <see cref="SymmetricStreamAlgorithmSpecification.NonceSizeBits" />.
    /// </summary>
    [TestMethod]
    public void NonceSize_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricStreamAlgorithmSpecification spec = GetSpecification();
        Assert.AreEqual(spec.NonceSizeBits, algorithm.NonceSize);
    }

    /// <summary>
    /// Verifies that the discrete key sizes enumerated by <see cref="SymmetricStreamAlgorithm.LegalKeySizes" /> match
    /// the set declared in <see cref="SymmetricStreamAlgorithmSpecification.LegalKeySizesBits" />.
    /// </summary>
    [TestMethod]
    public void LegalKeySizes_WhenCreated_ShouldMatchSpecification()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SymmetricStreamAlgorithmSpecification spec = GetSpecification();

        HashSet<int> actual = [];
        foreach (KeySizes keySize in algorithm.LegalKeySizes)
        {
            var step = keySize.SkipSize == 0 ? int.MaxValue : keySize.SkipSize;
            for (var size = keySize.MinSize; size <= keySize.MaxSize; size += step)
            {
                actual.Add(size);
                if (step == int.MaxValue)
                    break;
            }
        }

        HashSet<int> expected = [.. spec.LegalKeySizesBits];
        CollectionAssert.AreEquivalent(expected.ToArray(), actual.ToArray());
    }
}
