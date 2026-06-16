// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.FromBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue.FromBytes" /> copies the input so later mutation of the source buffer
    /// does not change the value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenSourceMutatedAfterwards_ShouldNotAffectValue()
    {
        byte[] source = new byte[] { 0x01, 0x02, 0x03 };
        var hash = HashValue.FromBytes(source);

        source[0] = 0xFF;

        Assert.AreEqual("010203", hash.ToHexString());
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.FromBytes" /> over an empty span produces the empty value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsEmpty_ShouldProduceEmptyValue()
    {
        var hash = HashValue.FromBytes([]);

        Assert.IsTrue(hash.IsEmpty);
        Assert.AreEqual(0, hash.Length);
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.FromBytes" /> reports the input length and exposes the same bytes through
    /// <see cref="HashValue.AsSpan" /> and <see cref="HashValue.ToArray" />.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsNonEmpty_ShouldExposeSameBytes()
    {
        byte[] source = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D };

        var hash = HashValue.FromBytes(source);

        Assert.AreEqual(source.Length, hash.Length);
        Assert.IsFalse(hash.IsEmpty);
        CollectionAssert.AreEqual(source, hash.ToArray());
        Assert.IsTrue(hash.AsSpan().SequenceEqual(source));
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.ToArray" /> returns a fresh copy whose mutation does not affect the value.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenResultMutated_ShouldNotAffectValue()
    {
        var hash = HashValue.FromBytes([0x10, 0x20]);

        byte[] copy = hash.ToArray();
        copy[0] = 0xFF;

        Assert.AreEqual("1020", hash.ToHexString());
    }
}
