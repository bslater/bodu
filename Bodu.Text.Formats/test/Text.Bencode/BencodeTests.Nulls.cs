// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(byte[])" /> rejects a <see langword="null" /> source array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullByteArraySource_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.Decode((byte[])null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.Encode(BencodedValue)" /> rejects a <see langword="null" /> value with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullValue_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.Encode((BencodedValue)null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetEncodedLength(BencodedValue)" /> rejects a <see langword="null" />
    /// value with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenNullValue_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.GetEncodedLength(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryEncode(BencodedValue, Span{byte}, out int)" /> rejects a
    /// <see langword="null" /> value with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenNullValue_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.TryEncode(null!, new byte[16], out _);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

}
