// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Parse(byte[])" /> rejects a <see langword="null" /> source array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullByteArraySource_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.Parse((byte[])null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.Format(BencodedValue)" /> rejects a <see langword="null" /> value with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullValue_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.Format((BencodedValue)null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.GetFormattedLength(BencodedValue)" /> rejects a <see langword="null" />
    /// value with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenNullValue_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.GetFormattedLength(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryFormat(BencodedValue, Span{byte}, out int)" /> rejects a
    /// <see langword="null" /> value with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenNullValue_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bencode.TryFormat(null!, new byte[16], out _);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

}
