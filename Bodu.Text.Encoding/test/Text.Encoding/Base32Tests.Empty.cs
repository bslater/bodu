// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Empty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array for every variant.
    /// </summary>
    /// <param name="variant">The Base32 variant.</param>
    [TestMethod]
    [DataRow(Base32Variant.Standard)]
    [DataRow(Base32Variant.HexExtended)]
    [DataRow(Base32Variant.Crockford)]
    [DataRow(Base32Variant.ZBase32)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base32Variant variant)
    {
        byte[] actual = Base32.Decode(string.Empty, variant);

        Assert.AreEqual(0, actual.Length);
    }
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> for every variant.
    /// </summary>
    /// <param name="variant">The Base32 variant.</param>
    [TestMethod]
    [DataRow(Base32Variant.Standard)]
    [DataRow(Base32Variant.HexExtended)]
    [DataRow(Base32Variant.Crockford)]
    [DataRow(Base32Variant.ZBase32)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base32Variant variant)
    {
        string actual = Base32.Encode([], variant);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> with an empty source returns <see langword="true" /> and writes
    /// zero bytes regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroBytesRegardlessOfDestination()
    {
        Assert.IsTrue(Base32.TryDecode([], new byte[1], out int t1));
        Assert.AreEqual(0, t1);

        Assert.IsTrue(Base32.TryDecode([], [], out int t2));
        Assert.AreEqual(0, t2);

        Assert.IsTrue(Base32.TryDecode([], new byte[100], out int t3));
        Assert.AreEqual(0, t3);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncode" /> with an empty source returns <see langword="true" /> and writes
    /// zero characters regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroCharsRegardlessOfDestination()
    {
        Assert.IsTrue(Base32.TryEncode([], new char[1], out int t1));
        Assert.AreEqual(0, t1);

        Assert.IsTrue(Base32.TryEncode([], [], out int t2));
        Assert.AreEqual(0, t2);

        Assert.IsTrue(Base32.TryEncode([], new char[100], out int t3));
        Assert.AreEqual(0, t3);
    }

}
