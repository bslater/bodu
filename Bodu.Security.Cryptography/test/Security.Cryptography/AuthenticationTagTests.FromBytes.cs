// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthenticationTagTests.FromBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class AuthenticationTagTests
{
    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.FromBytes" /> copies the input so later mutation of the source
    /// buffer does not change the value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenSourceMutatedAfterwards_ShouldNotAffectValue()
    {
        var source = new byte[] { 0x01, 0x02, 0x03 };
        var tag = AuthenticationTag.FromBytes(source);

        source[0] = 0xFF;

        Assert.AreEqual("010203", tag.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.FromBytes" /> over an empty span produces the empty value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsEmpty_ShouldProduceEmptyValue()
    {
        var tag = AuthenticationTag.FromBytes([]);

        Assert.IsTrue(tag.IsEmpty);
        Assert.AreEqual(0, tag.Length);
    }

    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.FromBytes" /> reports the input length and exposes the same bytes
    /// through <see cref="AuthenticationTag.AsSpan" /> and <see cref="AuthenticationTag.ToArray" />.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsNonEmpty_ShouldExposeSameBytes()
    {
        var source = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D };

        var tag = AuthenticationTag.FromBytes(source);

        Assert.AreEqual(source.Length, tag.Length);
        CollectionAssert.AreEqual(source, tag.ToArray());
        Assert.IsTrue(tag.AsSpan().SequenceEqual(source));
    }
}
