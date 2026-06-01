// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.IntegerOverflow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{
    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> accepts the maximum signed 64-bit integer
    /// boundary value <c>i9223372036854775807e</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIntegerEqualsLongMaxValue_ShouldReturnLongMaxValue()
    {
        var payload = Bytes("i9223372036854775807e");

        BencodedValue value = Bencode.Decode(payload);

        var integer = (BencodedInteger)value;
        Assert.AreEqual(long.MaxValue, integer.Value);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> rejects values one greater than
    /// <see cref="long.MaxValue" /> (<c>i9223372036854775808e</c>) with a <see cref="BencodeFormatException" />.
    /// This is the positive-side boundary that complements the existing <see cref="long.MinValue" /> test in the
    /// known-answer vector catalogue.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIntegerIsOneGreaterThanLongMaxValue_ShouldThrowBencodeFormatException()
    {
        var payload = Bytes("i9223372036854775808e");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(payload);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> rejects values one less than
    /// <see cref="long.MinValue" /> (<c>i-9223372036854775809e</c>) with a <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIntegerIsOneLessThanLongMinValue_ShouldThrowBencodeFormatException()
    {
        var payload = Bytes("i-9223372036854775809e");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(payload);
        });
    }
}
