// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthenticationTagTests.FixedTimeEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public sealed partial class AuthenticationTagTests
{
    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.FixedTimeEquals(AuthenticationTag)" /> returns
    /// <see langword="true" /> for identical tags.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenTagsAreIdentical_ShouldReturnTrue()
    {
        var left = AuthenticationTag.FromBytes([0xAA, 0xBB, 0xCC, 0xDD]);
        var right = AuthenticationTag.FromBytes([0xAA, 0xBB, 0xCC, 0xDD]);

        Assert.IsTrue(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.FixedTimeEquals(AuthenticationTag)" /> returns
    /// <see langword="false" /> when a single bit of the tag is tampered.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenSingleBitTampered_ShouldReturnFalse()
    {
        byte[] original = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var tag = AuthenticationTag.FromBytes(original);

        var tampered = AuthenticationTag.FromBytes(Tamper.FlipBit(original, 9));

        Assert.IsFalse(tag.FixedTimeEquals(tampered));
    }

    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.FixedTimeEquals(AuthenticationTag)" /> returns
    /// <see langword="false" /> when the lengths differ.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenLengthsDiffer_ShouldReturnFalse()
    {
        var left = AuthenticationTag.FromBytes([0xAA, 0xBB]);
        var right = AuthenticationTag.FromBytes([0xAA, 0xBB, 0xCC]);

        Assert.IsFalse(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that the span overload of <see cref="AuthenticationTag.FixedTimeEquals(ReadOnlySpan{byte})" />
    /// compares the tag against raw transform output.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_ForSpanOverload_ShouldCompareAgainstRawBytes()
    {
        byte[] bytes = new byte[] { 0x10, 0x20, 0x30 };
        var tag = AuthenticationTag.FromBytes(bytes);

        Assert.IsTrue(tag.FixedTimeEquals(bytes.AsSpan()));
        Assert.IsFalse(tag.FixedTimeEquals(Tamper.FlipByte(bytes, 1).AsSpan()));
    }
}
