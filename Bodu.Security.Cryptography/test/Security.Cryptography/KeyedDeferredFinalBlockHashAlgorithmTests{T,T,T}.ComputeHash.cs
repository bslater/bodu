// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedDeferredFinalBlockHashAlgorithmTests.Key.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that a keyed hash of the same message produces a different digest to the unkeyed hash of the same
    /// message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedAndUnkeyed_ShouldProduceDifferentDigests()
    {
        using TAlgorithm reference = CreateAlgorithm();
        int maxKeyBytes = reference.MaximumKeySize / 8;
        byte[] message = new byte[maxKeyBytes];
        int keySize = Math.Max(1, maxKeyBytes / 2);
        byte[] key = Enumerable.Range(1, keySize).Select(i => (byte)i).ToArray();

        byte[] unkeyedHash;
        byte[] keyedHash;

        using (TAlgorithm unkeyed = CreateAlgorithm())
            unkeyedHash = unkeyed.ComputeHash(message);

        using (TAlgorithm keyed = CreateAlgorithm())
        {
            keyed.Key = key;
            keyedHash = keyed.ComputeHash(message);
        }

        Assert.AreNotEqual(Convert.ToHexString(unkeyedHash), Convert.ToHexString(keyedHash));
    }

    /// <summary>
    /// Verifies that computing the keyed hash twice with the same key and message yields identical digests.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithSameKeyAndInput_ShouldProduceConsistentDigest()
    {
        using TAlgorithm sut = CreateAlgorithm();
        int maxKeyBytes = sut.MaximumKeySize / 8;
        int keySize = Math.Max(1, maxKeyBytes / 2);
        byte[] message = new byte[maxKeyBytes * 2];
        byte[] key = Enumerable.Range(1, keySize).Select(i => (byte)i).ToArray();
        sut.Key = key;

        byte[] hash1 = sut.ComputeHash(message);
        byte[] hash2 = sut.ComputeHash(message);

        CollectionAssert.AreEqual(hash1, hash2);
    }

    /// <summary>
    /// Verifies that two different keys produce different digests for the same message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenDifferentKeysUsed_ShouldProduceDifferentDigests()
    {
        using TAlgorithm reference = CreateAlgorithm();
        int maxKeyBytes = reference.MaximumKeySize / 8;
        byte[] message = new byte[maxKeyBytes];
        int keySize = Math.Max(1, maxKeyBytes / 2);
        byte[] key1 = Enumerable.Range(0, keySize).Select(i => (byte)i).ToArray();
        byte[] key2 = Enumerable.Range(0, keySize).Select(i => (byte)(i ^ 0xFF)).ToArray();

        byte[] hash1;
        byte[] hash2;

        using (TAlgorithm sut1 = CreateAlgorithm())
        {
            sut1.Key = key1;
            hash1 = sut1.ComputeHash(message);
        }

        using (TAlgorithm sut2 = CreateAlgorithm())
        {
            sut2.Key = key2;
            hash2 = sut2.ComputeHash(message);
        }

        Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2));
    }

    /// <summary>
    /// Verifies that clearing the key (by assigning an empty array) after keyed hashing causes the algorithm to
    /// revert to unkeyed mode and produce the same digest as a fresh unkeyed instance.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyCleared_ShouldMatchUnkeyedDigest()
    {
        using TAlgorithm reference = CreateAlgorithm();
        int maxKeyBytes = reference.MaximumKeySize / 8;
        byte[] message = new byte[maxKeyBytes];
        byte[] key = new byte[Math.Max(1, maxKeyBytes / 2)];

        byte[] unkeyedHash;
        byte[] clearedKeyHash;

        using (TAlgorithm unkeyed = CreateAlgorithm())
            unkeyedHash = unkeyed.ComputeHash(message);

        using (TAlgorithm sut = CreateAlgorithm())
        {
            sut.Key = key;
            _ = sut.ComputeHash(message); // keyed hash (discarded)

            sut.Key = [];                 // revert to unkeyed
            clearedKeyHash = sut.ComputeHash(message);
        }

        CollectionAssert.AreEqual(unkeyedHash, clearedKeyHash);
    }

    /// <summary>
    /// Verifies that computing the keyed hash over an empty message produces a non-empty digest consistent with
    /// the BLAKE2 keyed MAC specification (RFC 7693 Section 2.8), where the key block alone constitutes the
    /// padded message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithEmptyInput_ShouldReturnNonEmptyDigest()
    {
        using TAlgorithm sut = CreateAlgorithm();
        int keySize = Math.Max(1, (sut.MaximumKeySize / 8) / 2);
        sut.Key = Enumerable.Range(0, keySize).Select(i => (byte)i).ToArray();

        byte[] digest = sut.ComputeHash([]);

        Assert.IsNotNull(digest);
        Assert.HasCount(sut.HashSize / 8, digest);
    }

    /// <summary>
    /// Verifies that changing the key between successive
    /// <see cref="System.Security.Cryptography.HashAlgorithm.ComputeHash(byte[])" /> calls updates the digest
    /// output for the same message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyChangedBetweenCalls_ShouldProduceDifferentDigests()
    {
        using TAlgorithm sut = CreateAlgorithm();
        int maxKeyBytes = sut.MaximumKeySize / 8;
        byte[] message = new byte[maxKeyBytes];
        int keySize = Math.Max(1, maxKeyBytes / 2);
        byte[] key1 = Enumerable.Range(0, keySize).Select(i => (byte)i).ToArray();
        byte[] key2 = Enumerable.Range(0, keySize).Select(i => (byte)(255 - i)).ToArray();

        sut.Key = key1;
        byte[] hash1 = sut.ComputeHash(message);

        sut.Key = key2;
        byte[] hash2 = sut.ComputeHash(message);

        Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2));
    }

    /// <summary>
    /// Verifies that a key of one byte is accepted and produces a digest distinct from the unkeyed hash of the
    /// same message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyIsMinimalOneByteKey_ShouldDifferFromUnkeyedDigest()
    {
        using TAlgorithm reference = CreateAlgorithm();
        byte[] message = new byte[reference.MaximumKeySize / 8];
        byte[] key = [0x42];

        byte[] unkeyedHash;
        byte[] keyedHash;

        using (TAlgorithm unkeyed = CreateAlgorithm())
            unkeyedHash = unkeyed.ComputeHash(message);

        using (TAlgorithm keyed = CreateAlgorithm())
        {
            keyed.Key = key;
            keyedHash = keyed.ComputeHash(message);
        }

        Assert.AreNotEqual(Convert.ToHexString(unkeyedHash), Convert.ToHexString(keyedHash));
    }

    /// <summary>
    /// Verifies that the keyed MAC digest matches the known-answer test vector for the given variant, key, and
    /// input. Vectors are supplied by <see cref="KeyedDeferredFinalBlockHashAlgorithmTests{TTest,TAlgorithm,TVariant}.GetKeyedTestVectors" />
    /// and must agree with an authoritative reference implementation (e.g., Python's <c>hashlib</c> or the
    /// official BLAKE2 test-vector files).
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(KeyedKnownAnswerTestData))]
    public void ComputeHash_WhenKeyedWithKnownAnswerVector_ShouldMatchExpected(
        TVariant variant,
        string name,
        byte[] input,
        byte[] key,
        byte[] expected)
    {
        using TAlgorithm sut = CreateAlgorithm(variant);
        sut.Key = key;

        byte[] actual = sut.ComputeHash(input);

        CollectionAssert.AreEqual(expected, actual,
            $"Keyed KAT mismatch for '{name}' using variant '{variant}'.  " +
            $"Expected: {Convert.ToHexString(expected)}  " +
            $"Actual:   {Convert.ToHexString(actual)}");
    }
}
