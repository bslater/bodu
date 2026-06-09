// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public partial class BencodeTests
{
    /// <summary>
    /// Verifies that an integer token with no terminating <c>e</c> at end of input throws
    /// <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIntegerUnterminated_ShouldThrow()
    {
        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Parse("i"u8);
        });
    }

    /// <summary>
    /// Verifies that an integer token consisting only of a minus sign throws <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIntegerHasOnlyMinusSign_ShouldThrow()
    {
        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Parse("i-"u8);
        });
    }

    /// <summary>
    /// Verifies that a dictionary whose key is not a byte string throws <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDictionaryKeyIsNotString_ShouldThrow()
    {
        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Parse("di1ei2ee"u8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodedDictionary.Items" /> exposes the parsed key/value pairs.
    /// </summary>
    [TestMethod]
    public void Items_ShouldExposeParsedPairs()
    {
        var dictionary = (BencodedDictionary)Bencode.Parse("d3:keyi5ee"u8);

        Assert.AreEqual(1, dictionary.Items.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.ToString" /> returns the UTF-8 decoding of the byte content.
    /// </summary>
    [TestMethod]
    public void BencodedString_ToString_ShouldReturnUtf8Text()
    {
        var text = (BencodedString)Bencode.Parse("5:hello"u8);

        Assert.AreEqual("hello", text.ToString());
    }
}
