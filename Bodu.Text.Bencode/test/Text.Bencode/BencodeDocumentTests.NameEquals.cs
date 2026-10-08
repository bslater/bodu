// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.NameEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="BencodeProperty.NameEquals(ReadOnlySpan{byte})" />: that it compares a dictionary key's exact
/// bytes.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that the comparison tells apart two keys that are not valid UTF-8, which
    /// <see cref="BencodeProperty.Name" /> reads alike.
    /// </summary>
    [TestMethod]
    public void NameEquals_WhenKeysAreNotValidUtf8_ShouldMatchOnlyTheExactBytes()
    {
        using var document = BencodeDocument.Parse(Bytes("d1:þi1e1:ÿi2ee"));

        BencodeProperty[] properties = [.. document.RootElement.EnumerateObject()];

        Assert.IsTrue(properties[0].NameEquals([0xFE]));
        Assert.IsFalse(properties[0].NameEquals([0xFF]));
        Assert.IsTrue(properties[1].NameEquals([0xFF]));
    }

    /// <summary>
    /// Verifies that a key matches the UTF-8 encoding of its text and nothing longer or shorter.
    /// </summary>
    [TestMethod]
    public void NameEquals_WhenBytesDifferInLength_ShouldReturnFalse()
    {
        using var document = BencodeDocument.Parse(Bytes("d3:keyi1ee"));
        BencodeProperty property = document.RootElement.EnumerateObject().First();

        Assert.IsTrue(property.NameEquals("key"u8));
        Assert.IsFalse(property.NameEquals("ke"u8));
        Assert.IsFalse(property.NameEquals("keys"u8));
    }

    /// <summary>
    /// Verifies that comparing the key bytes of a default <see cref="BencodeProperty" />, which belongs to no document,
    /// throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void NameEquals_WhenPropertyIsDefault_ShouldThrowInvalidOperationException()
    {
        BencodeProperty property = default;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = property.NameEquals("key"u8);
        });
    }
}
