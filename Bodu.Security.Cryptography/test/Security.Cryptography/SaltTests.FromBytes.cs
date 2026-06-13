// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SaltTests.FromBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SaltTests
{
    /// <summary>
    /// Verifies that <see cref="Salt.FromBytes" /> copies the input so later mutation of the source buffer does
    /// not change the value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenSourceMutatedAfterwards_ShouldNotAffectValue()
    {
        var source = new byte[] { 0x01, 0x02, 0x03 };
        var salt = Salt.FromBytes(source);

        source[0] = 0xFF;

        Assert.AreEqual("010203", salt.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Salt.FromBytes" /> over an empty span produces the empty value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsEmpty_ShouldProduceEmptyValue()
    {
        var salt = Salt.FromBytes([]);

        Assert.IsTrue(salt.IsEmpty);
        Assert.AreEqual(0, salt.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Salt.FromBytes" /> reports the input length and exposes the same bytes through
    /// <see cref="Salt.AsSpan" /> and <see cref="Salt.ToArray" />.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsNonEmpty_ShouldExposeSameBytes()
    {
        var source = new byte[] { 0x0A, 0x0B, 0x0C };

        var salt = Salt.FromBytes(source);

        Assert.AreEqual(source.Length, salt.Length);
        CollectionAssert.AreEqual(source, salt.ToArray());
        Assert.IsTrue(salt.AsSpan().SequenceEqual(source));
    }
}
