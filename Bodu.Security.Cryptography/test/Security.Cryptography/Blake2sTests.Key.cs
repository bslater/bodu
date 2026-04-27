// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sTests.Key.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Blake2sTests
{
    /// <summary>
    /// Verifies that the <see cref="Blake2s.Key" /> property returns an empty array when the instance is created
    /// without a key.
    /// </summary>
    [TestMethod]
    public void Key_WhenDefaultConstructed_ShouldReturnEmptyArray()
    {
        using var sut = new Blake2s(256);

        byte[] key = sut.Key;

        Assert.IsNotNull(key);
        Assert.AreEqual(0, key.Length);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to <see cref="Blake2s.Key" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToNull_ShouldThrowArgumentNullException()
    {
        using var sut = new Blake2s(256);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Key = null!;
        });
    }

    /// <summary>
    /// Verifies that assigning a key whose length exceeds <see cref="Blake2s.MaxKeySize" /> throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToKeyLongerThanMaxKeySize_ShouldThrowCryptographicException()
    {
        using var sut = new Blake2s(256);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            sut.Key = new byte[Blake2s.MaxKeySize + 1];
        });
    }

    /// <summary>
    /// Verifies that assigning a key of exactly <see cref="Blake2s.MaxKeySize" /> bytes is accepted without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToMaxSizeKey_ShouldNotThrow()
    {
        using var sut = new Blake2s(256);
        byte[] key = new byte[Blake2s.MaxKeySize];

        sut.Key = key;

        CollectionAssert.AreEqual(key, sut.Key);
    }

    /// <summary>
    /// Verifies that the <see cref="Blake2s.Key" /> getter returns a defensive copy so that mutations to the
    /// returned array do not affect the algorithm's internal key.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetterInvoked_ShouldReturnDefensiveCopy()
    {
        using var sut = new Blake2s(256);
        byte[] key = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
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
        using var sut = new Blake2s(256);
        byte[] key = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        sut.Key = key;

        byte[] snapshot = sut.Key;
        key[0] = 0xFF;

        CollectionAssert.AreEqual(snapshot, sut.Key);
    }

    /// <summary>
    /// Verifies that assigning an empty array to <see cref="Blake2s.Key" /> clears the key and reverts the
    /// instance to unkeyed digest mode.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToEmptyArray_ShouldRevertToUnkeyedMode()
    {
        using var sut = new Blake2s(256);
        byte[] someKey = new byte[16];
        sut.Key = someKey;

        sut.Key = [];

        Assert.AreEqual(0, sut.Key.Length);
    }

    /// <summary>
    /// Verifies that assigning <see cref="Blake2s.Key" /> after hashing has begun throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetAfterHashingBegins_ShouldThrowCryptographicUnexpectedOperationException()
    {
        using var sut = new Blake2s(256);
        byte[] input = new byte[32];
        sut.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            sut.Key = new byte[16];
        });
    }

    /// <summary>
    /// Verifies that accessing <see cref="Blake2s.Key" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetOnDisposedInstance_ShouldThrowObjectDisposedException()
    {
        var sut = new Blake2s(256);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.Key;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="Blake2s.Key" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetOnDisposedInstance_ShouldThrowObjectDisposedException()
    {
        var sut = new Blake2s(256);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Key = new byte[16];
        });
    }

    /// <summary>
    /// Verifies that a keyed hash of the same message produces a different digest to the unkeyed hash of the
    /// same message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedAndUnkeyed_ShouldProduceDifferentDigests()
    {
        byte[] message = new byte[32];
        byte[] key = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        byte[] unkeyedHash;
        byte[] keyedHash;

        using (var unkeyed = new Blake2s(256))
            unkeyedHash = unkeyed.ComputeHash(message);

        using (var keyed = new Blake2s(256) { Key = key })
            keyedHash = keyed.ComputeHash(message);

        Assert.AreNotEqual(Convert.ToHexString(unkeyedHash), Convert.ToHexString(keyedHash));
    }

    /// <summary>
    /// Verifies that computing the keyed hash twice with the same key and message yields identical digests.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithSameKeyAndInput_ShouldProduceConsistentDigest()
    {
        byte[] message = new byte[64];
        byte[] key = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        using var sut = new Blake2s(256) { Key = key };

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
        byte[] message = new byte[32];
        byte[] key1 = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        byte[] key2 = Enumerable.Range(0, 16).Select(i => (byte)(i ^ 0xFF)).ToArray();

        byte[] hash1;
        byte[] hash2;

        using (var sut1 = new Blake2s(256) { Key = key1 })
            hash1 = sut1.ComputeHash(message);

        using (var sut2 = new Blake2s(256) { Key = key2 })
            hash2 = sut2.ComputeHash(message);

        Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2));
    }

    /// <summary>
    /// Verifies that clearing the key (by assigning an empty array) after keyed hashing causes the algorithm to
    /// revert to unkeyed mode and produce the same digest as a fresh unkeyed instance.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyCleared_ShouldMatchUnkeyedDigest()
    {
        byte[] message = new byte[32];
        byte[] key = new byte[16];

        byte[] unkeyedHash;
        byte[] clearedKeyHash;

        using (var unkeyed = new Blake2s(256))
            unkeyedHash = unkeyed.ComputeHash(message);

        using (var sut = new Blake2s(256))
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
    /// the BLAKE2s keyed MAC specification (RFC 7693 Section 2.8), where the key block alone constitutes the
    /// padded message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithEmptyInput_ShouldReturnNonEmptyDigest()
    {
        byte[] key = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

        using var sut = new Blake2s(256) { Key = key };
        byte[] digest = sut.ComputeHash([]);

        Assert.IsNotNull(digest);
        Assert.AreEqual(32, digest.Length);
    }

    /// <summary>
    /// Verifies that the keyed BLAKE2s-256 digest of an empty message with a 32-byte sequential key matches the
    /// known-answer value from the BLAKE2 reference test vectors (blake2s-kat.txt, first entry).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithEmptyInputAndFullKey_ShouldMatchKnownReferenceVector()
    {
        // Key: 32 sequential bytes 0x00..0x1f; input: empty.
        // Reference: https://github.com/BLAKE2/BLAKE2/blob/master/testvectors/blake2s-kat.txt
        byte[] key = Enumerable.Range(0, Blake2s.MaxKeySize).Select(i => (byte)i).ToArray();
        const string expected = "48A8997DA407876B3D79C0D92325AD3B89CBB754D86AB71AEE047AD345FD2C49";

        using var sut = new Blake2s(256) { Key = key };
        byte[] digest = sut.ComputeHash([]);

        Assert.AreEqual(expected, Convert.ToHexString(digest));
    }

    /// <summary>
    /// Verifies that changing the key between successive <see cref="Blake2s.ComputeHash" /> calls updates the
    /// digest output for the same message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyChangedBetweenCalls_ShouldProduceDifferentDigests()
    {
        byte[] message = new byte[32];
        byte[] key1 = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        byte[] key2 = Enumerable.Range(0, 16).Select(i => (byte)(255 - i)).ToArray();

        using var sut = new Blake2s(256);

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
        byte[] message = new byte[32];
        byte[] key = [0x42];

        byte[] unkeyedHash;
        byte[] keyedHash;

        using (var unkeyed = new Blake2s(256))
            unkeyedHash = unkeyed.ComputeHash(message);

        using (var keyed = new Blake2s(256) { Key = key })
            keyedHash = keyed.ComputeHash(message);

        Assert.AreNotEqual(Convert.ToHexString(unkeyedHash), Convert.ToHexString(keyedHash));
    }
}
