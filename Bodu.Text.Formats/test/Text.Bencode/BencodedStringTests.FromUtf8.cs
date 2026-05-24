// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringTests.FromUtf8.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodedStringTests
{

    /// <summary>
    /// Verifies that <see cref="BencodedString.FromUtf8(string)" /> emits multi-byte UTF-8 sequences for accented
    /// characters (here U+00E9 -&gt; 0xC3 0xA9).
    /// </summary>
    [TestMethod]
    public void FromUtf8_WhenAccentedCharacter_ShouldEmitMultiByteSequence()
    {
        var value = BencodedString.FromUtf8("é");

        CollectionAssert.AreEqual(new byte[] { 0xC3, 0xA9 }, value.Bytes.ToArray());
    }
    /// <summary>
    /// Verifies that <see cref="BencodedString.FromUtf8(string)" /> encodes ASCII text exactly as the UTF-8
    /// equivalent.
    /// </summary>
    [TestMethod]
    public void FromUtf8_WhenAsciiText_ShouldEncodeAsAscii()
    {
        var value = BencodedString.FromUtf8("spam");

        CollectionAssert.AreEqual(new byte[] { (byte)'s', (byte)'p', (byte)'a', (byte)'m' }, value.Bytes.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.FromUtf8(string)" /> encodes the empty string to an empty byte
    /// payload.
    /// </summary>
    [TestMethod]
    public void FromUtf8_WhenEmptyString_ShouldProduceEmptyByteArray()
    {
        var value = BencodedString.FromUtf8(string.Empty);

        Assert.AreEqual(0, value.Length);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.FromUtf8(string)" /> rejects a <see langword="null" /> argument
    /// with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FromUtf8_WhenNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BencodedString.FromUtf8(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.FromUtf8(string)" /> emits the correct four-byte UTF-8 sequence
    /// for an astral-plane character represented as a surrogate pair (U+1F600 GRINNING FACE).
    /// </summary>
    [TestMethod]
    public void FromUtf8_WhenSurrogatePair_ShouldEmitFourByteSequence()
    {
        var value = BencodedString.FromUtf8("😀");

        CollectionAssert.AreEqual(new byte[] { 0xF0, 0x9F, 0x98, 0x80 }, value.Bytes.ToArray());
    }

}
