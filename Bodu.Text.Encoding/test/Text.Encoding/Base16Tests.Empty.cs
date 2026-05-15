// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> with default options.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString()
    {
        string actual = Base16.Encode(Array.Empty<byte>());

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that encoding an empty input with <see cref="BaseFormattingOptions.IncludePrefix" /> still emits the
    /// prefix.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyAndIncludePrefix_ShouldReturnPrefixOnly()
    {
        string actual = Base16.Encode(ReadOnlySpan<byte>.Empty, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("0x", actual);
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray()
    {
        byte[] actual = Base16.Decode(string.Empty);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that decoding an empty span returns an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmptySpan_ShouldReturnEmptyByteArray()
    {
        byte[] actual = Base16.Decode(ReadOnlySpan<char>.Empty);

        Assert.AreEqual(0, actual.Length);
    }
}
