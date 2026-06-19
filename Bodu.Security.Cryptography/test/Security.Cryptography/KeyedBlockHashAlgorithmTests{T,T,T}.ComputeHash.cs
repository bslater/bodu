// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedBlockHashAlgorithmTests{T,T,T}.ComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

public abstract partial class KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that the algorithm's key remains unchanged after hashing.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void ComputeHash_WhenHashing_ShouldRespectKeyRetentionPolicy(TVariant variant)
    {
        if (GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
        {
            Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithm(variant);
        byte[] key = GenerateUniqueKey(specification.MinKeyLength);
        byte[] data = new byte[256];

        algorithm.Key = key;

        _ = algorithm.ComputeHash(data);

        // Validate key is unchanged
        if (algorithm.CanReuseTransform)

            // Reusable hash algorithm: key should remain unchanged
            CollectionAssert.AreEqual(key, algorithm.Key, "Key was unexpectedly modified by reusable algorithm.");
        else

            // One-shot: key should be cleared
            CollectionAssert.AreNotEqual(key, algorithm.Key, "Key was not cleared after one-shot MAC computation.");
    }

    /// <summary>
    /// Verifies that supplying a non-empty <see cref="Skein{T}.Key" /> causes the digest to differ from the plain,
    /// unkeyed hash of the same input — confirming that the preliminary <c>KEY</c> UBI phase actually influences the
    /// chaining value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void ComputeHash_WhenKeyIsSet_ShouldDifferFromUnkeyedHashOfSameInput(TVariant variant)
    {
        if (GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
        {
            Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
            return;
        }

        byte[] data = (byte[])CryptoTestUtilities.ByteSequence256.Clone();

        byte[] plainHash;
        using (TAlgorithm plain = CreateAlgorithm(variant))
            plainHash = plain.ComputeHash(data);

        byte[] macHash;
        byte[] key = TestHelpers.GenerateRandomNonZeroBytes(Math.Max(1, specification.MinKeyLength));
        using (TAlgorithm mac = CreateAlgorithm(variant, key))
            macHash = mac.ComputeHash(data);

        CollectionAssert.AreNotEqual(plainHash, macHash);
    }

    /// <summary>
    /// Verifies that <see cref="KeyedBlockHashAlgorithm.ComputeHash" /> returns the same result when called multiple times with the same key and input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void ComputeHash_WhenSameKeyAndInputUsed_ShouldReturnIdenticalResults(TVariant variant)
    {
        if (GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
        {
            Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
            return;
        }

        byte[] key = GenerateUniqueKey(specification.MinKeyLength);
        byte[] data = new byte[256];

        using TAlgorithm algorithm1 = CreateAlgorithm(variant);
        using TAlgorithm algorithm2 = CreateAlgorithm(variant);
        algorithm1.Key = algorithm2.Key = key;

        byte[] hash1 = algorithm1.ComputeHash(data);
        byte[] hash2 = algorithm2.ComputeHash(data);

        CollectionAssert.AreEqual(hash1, hash2, "Hashes differ when using the same key and input.");
    }

    /// <summary>
    /// Verifies that <see cref="KeyedBlockHashAlgorithm.ComputeHash" /> returns different results when different keys are used with the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void ComputeHash_WhenDifferentKeysUsed_ShouldReturnDifferentResults(TVariant variant)
    {
        if (GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
        {
            Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
            return;
        }

        byte[] key1 = GenerateUniqueKey(specification.MinKeyLength);
        byte[] key2 = GenerateUniqueKey(specification.MinKeyLength);
        byte[] data = new byte[256];

        byte[] hash1, hash2;

        using TAlgorithm algorithm1 = CreateAlgorithm(variant);
        algorithm1.Key = key1;
        hash1 = algorithm1.ComputeHash(data);

        using TAlgorithm algorithm2 = CreateAlgorithm(variant);
        algorithm2.Key = key2;
        hash2 = algorithm2.ComputeHash(data);

        CollectionAssert.AreNotEqual(hash1, hash2);
    }

    /// <summary>
    /// Verifies that two valid keys with different lengths produce different digests for the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void ComputeHash_WhenValidKeysHaveDifferentLengths_ShouldProduceDifferentDigests(TVariant variant)
    {
        if (GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
        {
            Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping variable key length test.");
            return;
        }

        if (!specification.AcceptsVariableLengthKeys)
        {
            Assert.Inconclusive($"[{variant}] Algorithm does not support variable key lengths; skipping.");
            return;
        }

        byte[] data = (byte[])CryptoTestUtilities.ByteSequence256.Clone();
        int keyLength = Math.Max(1, specification.MinKeyLength);

        if (keyLength + 1 > specification.MaxKeyLength)
        {
            Assert.Inconclusive($"[{variant}] Algorithm does not expose two valid non-empty key lengths.");
            return;
        }
        byte[] shortKey = TestHelpers.GenerateRepeatingIncrementalByteSequence(1, keyLength);
        byte[] longerKey = TestHelpers.GenerateRepeatingIncrementalByteSequence(1, keyLength + 1);

        byte[] hash1;
        using (TAlgorithm algorithm = CreateAlgorithm(variant))
        {
            algorithm.Key = shortKey;
            hash1 = algorithm.ComputeHash(data);
        }

        byte[] hash2;
        using (TAlgorithm algorithm = CreateAlgorithm(variant))
        {
            algorithm.Key = longerKey;
            hash2 = algorithm.ComputeHash(data);
        }

        CollectionAssert.AreNotEqual(
            hash1,
            hash2,
            $"[{variant}] Keys with different valid lengths must produce different digests.");
    }

    /// <summary>
    /// Verifies that representative valid key lengths produce distinct digests for the same input.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void ComputeHash_WhenRepresentativeVariableKeyLengthsUsed_ShouldProduceDistinctDigests(TVariant variant)
    {
        if (GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
        {
            Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping variable key length test.");
            return;
        }

        if (!specification.AcceptsVariableLengthKeys)
        {
            Assert.Inconclusive($"[{variant}] Algorithm does not support variable key lengths; skipping.");
            return;
        }

        using TAlgorithm probe = CreateAlgorithm(variant);

        int[] lengths =
        [
            0,
            1,
            specification.MinKeyLength,
            probe.InputBlockSize - 1,
            probe.InputBlockSize,
            probe.InputBlockSize + 1,
            specification.MaxKeyLength
        ];

        int[] validLengths = lengths
            .Where(length => length >= 0)
            .Where(length => length >= specification.MinKeyLength || !EnforcesMinimumKeyLength)
            .Where(length => length <= specification.MaxKeyLength)
            .Distinct()
            .OrderBy(length => length)
            .ToArray();

        if (validLengths.Length < 2)
        {
            Assert.Inconclusive($"[{variant}] Algorithm does not expose multiple representative valid key lengths.");
            return;
        }

        byte[] data = (byte[])CryptoTestUtilities.ByteSequence256.Clone();
        var hashesByLength = new Dictionary<int, string>();

        foreach (int length in validLengths)
        {
            byte[] key = TestHelpers.GenerateRepeatingIncrementalByteSequence(1, length);

            using TAlgorithm algorithm = CreateAlgorithm(variant);
            algorithm.Key = key;

            byte[] hash = algorithm.ComputeHash(data);
            hashesByLength.Add(length, Convert.ToHexString(hash));
        }

        Assert.AreEqual(
            hashesByLength.Count,
            hashesByLength.Values.Distinct(StringComparer.Ordinal).Count(),
            $"[{variant}] Representative valid key lengths should produce distinct digests.");
    }
}
