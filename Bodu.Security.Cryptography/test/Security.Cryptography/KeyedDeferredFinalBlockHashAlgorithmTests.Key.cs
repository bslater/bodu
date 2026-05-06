// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedDeferredFinalBlockHashAlgorithmTests.Key.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that the <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> property returns an empty
    /// array when the instance is created without a key.
    /// </summary>
    [TestMethod]
    public void Key_WhenDefaultConstructed_ShouldReturnEmptyArray()
    {
        using TAlgorithm sut = CreateAlgorithm();

        byte[] key = sut.Key;

        Assert.IsNotNull(key);
        Assert.AreEqual(0, key.Length);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm sut = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Key = null!;
        });
    }

    /// <summary>
    /// Verifies that assigning a key whose length exceeds
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.MaximumKeySize" /> throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToKeyLongerThanMaximumKeySize_ShouldThrowCryptographicException()
    {
        using TAlgorithm sut = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            sut.Key = new byte[sut.MaximumKeySize + 1];
        });
    }

    /// <summary>
    /// Verifies that assigning a key of exactly
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.MaximumKeySize" /> bytes is accepted without throwing.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToMaximumSizeKey_ShouldNotThrow()
    {
        using TAlgorithm sut = CreateAlgorithm();
        byte[] key = new byte[sut.MaximumKeySize];

        sut.Key = key;

        CollectionAssert.AreEqual(key, sut.Key);
    }

    /// <summary>
    /// Verifies that the <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> getter returns a defensive
    /// copy so that mutations to the returned array do not affect the algorithm's internal key.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetterInvoked_ShouldReturnDefensiveCopy()
    {
        using TAlgorithm sut = CreateAlgorithm();
        int keySize = Math.Max(1, sut.MaximumKeySize / 2);
        byte[] key = Enumerable.Range(1, keySize).Select(i => (byte)i).ToArray();
        sut.Key = key;

        byte[] retrieved = sut.Key;
        retrieved[0] = 0xFF;

        Assert.AreNotEqual(0xFF, sut.Key[0]);
    }

    /// <summary>
    /// Verifies that mutating the original key array after assigning it does not affect the algorithm's internal
    /// copy.
    /// </summary>
    [TestMethod]
    public void Key_WhenOriginalArrayMutatedAfterSetting_ShouldNotAffectInternalKey()
    {
        using TAlgorithm sut = CreateAlgorithm();
        int keySize = Math.Max(1, sut.MaximumKeySize / 2);
        byte[] key = Enumerable.Range(1, keySize).Select(i => (byte)i).ToArray();
        sut.Key = key;

        byte[] snapshot = sut.Key;
        key[0] = 0xFF;

        CollectionAssert.AreEqual(snapshot, sut.Key);
    }

    /// <summary>
    /// Verifies that assigning an empty array to
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> clears the key and reverts the instance to
    /// unkeyed digest mode.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToEmptyArray_ShouldRevertToUnkeyedMode()
    {
        using TAlgorithm sut = CreateAlgorithm();
        sut.Key = new byte[Math.Max(1, sut.MaximumKeySize / 2)];

        sut.Key = [];

        Assert.AreEqual(0, sut.Key.Length);
    }

    /// <summary>
    /// Verifies that assigning <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> after hashing has
    /// begun throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetAfterHashingBegins_ShouldThrowCryptographicUnexpectedOperationException()
    {
        using TAlgorithm sut = CreateAlgorithm();
        byte[] input = new byte[32];
        sut.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            sut.Key = new byte[Math.Max(1, sut.MaximumKeySize / 2)];
        });
    }

    /// <summary>
    /// Verifies that accessing <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetOnDisposedInstance_ShouldThrowObjectDisposedException()
    {
        TAlgorithm sut = CreateAlgorithm();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.Key;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetOnDisposedInstance_ShouldThrowObjectDisposedException()
    {
        TAlgorithm sut = CreateAlgorithm();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Key = new byte[Math.Max(1, sut.MaximumKeySize / 2)];
        });
    }

    /// <summary>
    /// Verifies that a keyed hash of the same message produces a different digest to the unkeyed hash of the same
    /// message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedAndUnkeyed_ShouldProduceDifferentDigests()
    {
        using TAlgorithm reference = CreateAlgorithm();
        byte[] message = new byte[reference.MaximumKeySize];
        int keySize = Math.Max(1, reference.MaximumKeySize / 2);
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
        int keySize = Math.Max(1, sut.MaximumKeySize / 2);
        byte[] message = new byte[sut.MaximumKeySize * 2];
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
        byte[] message = new byte[reference.MaximumKeySize];
        int keySize = Math.Max(1, reference.MaximumKeySize / 2);
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
        byte[] message = new byte[reference.MaximumKeySize];
        byte[] key = new byte[Math.Max(1, reference.MaximumKeySize / 2)];

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
        int keySize = Math.Max(1, sut.MaximumKeySize / 2);
        sut.Key = Enumerable.Range(0, keySize).Select(i => (byte)i).ToArray();

        byte[] digest = sut.ComputeHash([]);

        Assert.IsNotNull(digest);
        Assert.AreEqual(sut.HashSize / 8, digest.Length);
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
        byte[] message = new byte[sut.MaximumKeySize];
        int keySize = Math.Max(1, sut.MaximumKeySize / 2);
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
        byte[] message = new byte[reference.MaximumKeySize];
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
