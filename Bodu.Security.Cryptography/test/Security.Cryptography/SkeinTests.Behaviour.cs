// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.Behaviour.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class SkeinTests
{
    /// <summary>
    /// Verifies that the default-configured <see cref="Skein256" /> instance produces a 256-bit digest.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenDefaultConstructed_ForSkein256_ShouldBe256Bits()
    {
        using var skein = new Skein256();

        Assert.AreEqual(256, skein.HashSize);
    }

    /// <summary>
    /// Verifies that the default-configured <see cref="Skein512" /> instance produces a 512-bit digest.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenDefaultConstructed_ForSkein512_ShouldBe512Bits()
    {
        using var skein = new Skein512();

        Assert.AreEqual(512, skein.HashSize);
    }

    /// <summary>
    /// Verifies that the default-configured <see cref="Skein1024" /> instance produces a 1024-bit digest.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenDefaultConstructed_ForSkein1024_ShouldBe1024Bits()
    {
        using var skein = new Skein1024();

        Assert.AreEqual(1024, skein.HashSize);
    }

    /// <summary>
    /// Verifies that <see cref="Skein256.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenSkein256ConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein256(224);

        Assert.AreEqual("Skein-256-224", skein.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="Skein512.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenSkein512ConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein512(384);

        Assert.AreEqual("Skein-512-384", skein.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="Skein1024.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenSkein1024ConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein1024(512);

        Assert.AreEqual("Skein-1024-512", skein.AlgorithmName);
    }

    /// <summary>
    /// Verifies that constructing a Skein-256 instance with an output size that is not in the permitted set throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenHashSizeIsUnsupported_ForSkein256_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Skein256(100);
        });
    }

    /// <summary>
    /// Verifies that constructing a Skein-512 instance with an output size that is not in the permitted set throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenHashSizeIsUnsupported_ForSkein512_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Skein512(100);
        });
    }

    /// <summary>
    /// Verifies that constructing a Skein-1024 instance with an output size that is not in the permitted set throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenHashSizeIsUnsupported_ForSkein1024_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Skein1024(256);
        });
    }

    /// <summary>
    /// Verifies that Skein produces a digest whose length in bytes matches the configured <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" /> divided by eight.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(160)]
    [DataRow(224)]
    [DataRow(256)]
    public void ComputeHash_WhenSkein256ConfiguredWithOutputSize_ShouldProduceDigestOfThatLength(int hashSize)
    {
        using var skein = new Skein256(hashSize);

        byte[] digest = skein.ComputeHash(BuildDeterministicInput(100));

        Assert.AreEqual(hashSize / 8, digest.Length);
    }

    /// <summary>
    /// Verifies that Skein-512 supports every advertised output size and produces a digest of the matching length.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(160)]
    [DataRow(224)]
    [DataRow(256)]
    [DataRow(384)]
    [DataRow(512)]
    public void ComputeHash_WhenSkein512ConfiguredWithOutputSize_ShouldProduceDigestOfThatLength(int hashSize)
    {
        using var skein = new Skein512(hashSize);

        byte[] digest = skein.ComputeHash(BuildDeterministicInput(100));

        Assert.AreEqual(hashSize / 8, digest.Length);
    }

    /// <summary>
    /// Verifies that Skein-1024 supports every advertised output size and produces a digest of the matching length.
    /// </summary>
    [TestMethod]
    [DataRow(384)]
    [DataRow(512)]
    [DataRow(1024)]
    public void ComputeHash_WhenSkein1024ConfiguredWithOutputSize_ShouldProduceDigestOfThatLength(int hashSize)
    {
        using var skein = new Skein1024(hashSize);

        byte[] digest = skein.ComputeHash(BuildDeterministicInput(100));

        Assert.AreEqual(hashSize / 8, digest.Length);
    }

    /// <summary>
    /// Verifies that changing the Skein output size changes the digest: Skein output extraction is counter-driven
    /// via the <c>OUT</c> UBI chain, so a shorter output is not merely a prefix of the longer output.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSkein512OutputSizeDiffers_ShouldProduceDistinctDigests()
    {
        byte[] input = BuildDeterministicInput(64);

        using var skein256Out = new Skein512(256);
        using var skein512Out = new Skein512(512);

        byte[] digest256 = skein256Out.ComputeHash(input);
        byte[] digest512 = skein512Out.ComputeHash(input);

        // The shorter digest must not be a prefix of the longer one — Skein's OUT UBI feeds the output size into
        // the configuration block, so distinct output sizes yield distinct hash functions rather than truncations.
        bool isPrefix = digest512.AsSpan(0, digest256.Length).SequenceEqual(digest256);
        Assert.IsFalse(isPrefix, "Skein-512-256 must not be a prefix of Skein-512-512.");
    }

    /// <summary>
    /// Verifies that hashing the same input in random streaming chunk sizes produces the same digest as a single-pass
    /// <see cref="System.Security.Cryptography.HashAlgorithm.ComputeHash(byte[])" /> — the residual-buffer lag inside the Skein UBI engine must
    /// not be observable.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(31)]
    [DataRow(32)]
    [DataRow(33)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(127)]
    [DataRow(128)]
    [DataRow(129)]
    public void ComputeHash_WhenSkein512InputStreamedInChunks_ShouldMatchSinglePassResult(int chunkSize)
    {
        byte[] input = BuildDeterministicInput(517);

        byte[] expected;
        using (var reference = new Skein512())
            expected = reference.ComputeHash(input);

        using var algorithm = new Skein512();

        int offset = 0;
        while (offset < input.Length)
        {
            int count = Math.Min(chunkSize, input.Length - offset);
            algorithm.TransformBlock(input, offset, count, null, 0);
            offset += count;
        }

        algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that re-hashing the same input on a reused instance after <see cref="System.Security.Cryptography.HashAlgorithm.Initialize" />
    /// reproduces the original digest, confirming that <see cref="Skein.Initialize" /> restores both the chaining
    /// value and the UBI book-keeping state.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInstanceReusedAfterInitialize_ShouldProduceIdenticalDigest()
    {
        byte[] input = BuildDeterministicInput(200);

        using var skein = new Skein512();
        byte[] first = skein.ComputeHash(input);
        byte[] second = skein.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that setting a non-empty <see cref="Skein.Key" /> switches Skein into Skein-MAC mode and produces a
    /// digest that differs from the unkeyed hash of the same message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyIsSet_ShouldDifferFromUnkeyedHashOfSameInput()
    {
        byte[] input = BuildDeterministicInput(80);
        byte[] key = Enumerable.Range(0, 16).Select(i => (byte)(i + 1)).ToArray();

        byte[] plainHash;
        using (var plain = new Skein512())
            plainHash = plain.ComputeHash(input);

        byte[] macHash;
        using (var mac = new Skein512 { Key = key })
            macHash = mac.ComputeHash(input);

        CollectionAssert.AreNotEqual(plainHash, macHash);
    }

    /// <summary>
    /// Verifies that distinct keys produce distinct Skein-MAC tags over the same input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeysDiffer_ShouldProduceDistinctMacs()
    {
        byte[] input = BuildDeterministicInput(80);
        byte[] keyA = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        byte[] keyB = Enumerable.Range(0, 32).Select(i => (byte)(i + 0x80)).ToArray();

        byte[] macA;
        using (var mac = new Skein512 { Key = keyA })
            macA = mac.ComputeHash(input);

        byte[] macB;
        using (var mac = new Skein512 { Key = keyB })
            macB = mac.ComputeHash(input);

        CollectionAssert.AreNotEqual(macA, macB);
    }

    /// <summary>
    /// Verifies that Skein-MAC with a key whose length exceeds one state block correctly processes a multi-block
    /// <c>KEY</c> UBI phase without corruption (exercises the key-chunking loop in <see cref="Skein" />).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyExceedsBlockSize_ShouldProduceStableMac()
    {
        byte[] input = BuildDeterministicInput(50);
        byte[] longKey = Enumerable.Range(0, 150).Select(i => (byte)(i ^ 0xA5)).ToArray();

        byte[] first;
        using (var mac = new Skein512 { Key = longKey })
            first = mac.ComputeHash(input);

        byte[] second;
        using (var mac = new Skein512 { Key = longKey })
            second = mac.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
        Assert.AreEqual(64, first.Length);
    }

    /// <summary>
    /// Verifies that the <see cref="Skein.Key" /> getter returns a defensive copy and does not expose the internal
    /// key storage to external mutation.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetterCalledTwice_ShouldReturnIndependentCopies()
    {
        using var skein = new Skein512();
        skein.Key = new byte[] { 1, 2, 3, 4 };

        byte[] first = skein.Key;
        first[0] = 0xFF;

        byte[] second = skein.Key;

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, second);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to <see cref="Skein.Key" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenAssignedNull_ShouldThrowArgumentNullException()
    {
        using var skein = new Skein512();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            skein.Key = null!;
        });
    }

    /// <summary>
    /// Verifies that <see cref="Skein.InputBlockSize" /> exposes the Skein state block size for each variant.
    /// </summary>
    [TestMethod]
    public void InputBlockSize_WhenSkeinVariantIsUsed_ShouldReturnVariantBlockSize()
    {
        using var skein256 = new Skein256();
        using var skein512 = new Skein512();
        using var skein1024 = new Skein1024();

        Assert.AreEqual(32, skein256.InputBlockSize);
        Assert.AreEqual(64, skein512.InputBlockSize);
        Assert.AreEqual(128, skein1024.InputBlockSize);
    }

    /// <summary>
    /// Verifies that calling <see cref="System.Security.Cryptography.HashAlgorithm.ComputeHash(byte[])" /> on a disposed Skein instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInstanceIsDisposed_ShouldThrowObjectDisposedException()
    {
        var skein = new Skein512();
        skein.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = skein.ComputeHash(Array.Empty<byte>());
        });
    }

    /// <summary>
    /// Verifies that disposing a Skein instance twice is safe and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var skein = new Skein512();
        skein.Dispose();
        skein.Dispose();
    }
}
