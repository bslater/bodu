// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonceTests.FromBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class NonceTests
{
    /// <summary>
    /// Verifies that <see cref="Nonce.FromBytes" /> copies the input so later mutation of the source buffer does
    /// not change the value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenSourceMutatedAfterwards_ShouldNotAffectValue()
    {
        var source = new byte[] { 0x01, 0x02, 0x03 };
        var nonce = Nonce.FromBytes(source);

        source[0] = 0xFF;

        Assert.AreEqual("010203", nonce.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Nonce.FromBytes" /> over an empty span produces the empty value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsEmpty_ShouldProduceEmptyValue()
    {
        var nonce = Nonce.FromBytes([]);

        Assert.IsTrue(nonce.IsEmpty);
        Assert.AreEqual(0, nonce.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Nonce.FromBytes" /> reports the input length and exposes the same bytes through
    /// <see cref="Nonce.AsSpan" /> and <see cref="Nonce.ToArray" />.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsNonEmpty_ShouldExposeSameBytes()
    {
        var source = new byte[] { 0x0A, 0x0B, 0x0C };

        var nonce = Nonce.FromBytes(source);

        Assert.AreEqual(source.Length, nonce.Length);
        CollectionAssert.AreEqual(source, nonce.ToArray());
        Assert.IsTrue(nonce.AsSpan().SequenceEqual(source));
    }
}
