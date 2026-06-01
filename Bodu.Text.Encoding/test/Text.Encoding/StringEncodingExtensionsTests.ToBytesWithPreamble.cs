// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.ToBytesWithPreamble.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytesWithPreamble(string, System.Text.Encoding)" />
    /// prefixes the preamble when the encoding emits one.
    /// </summary>
    [TestMethod]
    public void ToBytesWithPreamble_WhenEncodingHasPreamble_ShouldPrefixPreamble()
    {
        System.Text.Encoding utf8WithBom = new System.Text.UTF8Encoding(true);

        var actual = MultiByteText.ToBytesWithPreamble(utf8WithBom);

        var expected = utf8WithBom.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(MultiByteText))
            .ToArray();
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytesWithPreamble(string, System.Text.Encoding)" />
    /// returns the encoded bytes only when the encoding has no preamble.
    /// </summary>
    [TestMethod]
    public void ToBytesWithPreamble_WhenEncodingHasNoPreamble_ShouldReturnEncodedBytesOnly()
    {
        System.Text.Encoding utf8NoBom = new System.Text.UTF8Encoding(false);

        var actual = SampleText.ToBytesWithPreamble(utf8NoBom);

        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(SampleText), actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytesWithPreamble(string, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBytesWithPreamble_WhenTextIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.ToBytesWithPreamble(null!, System.Text.Encoding.UTF8);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytesWithPreamble(string, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBytesWithPreamble_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.ToBytesWithPreamble(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
