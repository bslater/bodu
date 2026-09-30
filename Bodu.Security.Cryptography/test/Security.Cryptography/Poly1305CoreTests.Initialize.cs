// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.Initialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that a key that is not 32 bytes long is rejected with <see cref="ArgumentOutOfRangeException" /> naming
    /// the parameter.
    /// </summary>
    /// <param name="length">The rejected key length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    public void Initialize_WhenKeyIsNot32Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        byte[] key = new byte[length];
        Poly1305Core core = default;

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            core.Initialize(key);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that initializing a core part way through a message discards everything it held - key, accumulator and
    /// partial block - so the next tag is the one a fresh core computes under the new key.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCoreHoldsState_ShouldStartAFreshAuthentication()
    {
        var random = new Random(0x1305_0001);
        byte[] firstKey = NextBytes(random, Poly1305Core.KeyBytes);
        byte[] secondKey = NextBytes(random, Poly1305Core.KeyBytes);
        byte[] message = NextBytes(random, 45);

        Poly1305Core core = default;
        core.Initialize(firstKey);
        core.Update(NextBytes(random, 23));
        core.Initialize(secondKey);
        core.Update(message);

        byte[] tag = new byte[Poly1305Core.TagBytes];
        core.Finish(tag);

        CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(secondKey, message), tag);
    }

    /// <summary>
    /// Verifies that the key half <c>r</c> is clamped as RFC 8439 requires: a key whose <c>r</c> is all ones
    /// authenticates exactly as one whose <c>r</c> already has the clamped bits cleared.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenRHasClampedBitsSet_ShouldClearThem()
    {
        byte[] unclamped = new byte[Poly1305Core.KeyBytes];
        Array.Fill(unclamped, (byte)0xFF);

        // r &= 0x0ffffffc0ffffffc0ffffffc0fffffff (RFC 8439, Section 2.5), a little-endian byte at a time.
        byte[] clamped = (byte[])unclamped.Clone();
        foreach (int index in new[] { 3, 7, 11, 15 })
            clamped[index] &= 0x0F;

        foreach (int index in new[] { 4, 8, 12 })
            clamped[index] &= 0xFC;

        byte[] message = NextBytes(new Random(0x1305_0002), 100);

        CollectionAssert.AreEqual(ComputeTag(clamped, message), ComputeTag(unclamped, message));
    }
}
