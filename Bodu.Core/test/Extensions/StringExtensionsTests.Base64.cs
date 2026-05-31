// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Base64.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToBase64(string, Encoding)" /> with the default UTF-8
    /// encoding matches <see cref="Convert.ToBase64String(byte[])" /> over the same byte sequence.
    /// </summary>
    [TestMethod]
    public void ToBase64_WhenEncodingDefaultsToUtf8_ShouldMatchConvertOverUtf8Bytes()
    {
        const string input = "héllo — world";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

        Assert.AreEqual(expected, input.ToBase64());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToBase64(string, Encoding)" /> respects an explicit encoding.
    /// </summary>
    [TestMethod]
    public void ToBase64_WhenAsciiEncodingSupplied_ShouldEncodeBytesFromAscii()
    {
        const string input = "hello";
        var expected = Convert.ToBase64String(Encoding.ASCII.GetBytes(input));

        Assert.AreEqual(expected, input.ToBase64(Encoding.ASCII));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToBase64(string, Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBase64_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.ToBase64(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.FromBase64ToString(string, Encoding)" /> round-trips against
    /// <see cref="StringExtensions.ToBase64(string, Encoding)" />.
    /// </summary>
    [TestMethod]
    public void FromBase64ToString_WhenInvokedOnToBase64Output_ShouldRoundTrip()
    {
        const string original = "héllo — Привет — 你好";

        var actual = original.ToBase64().FromBase64ToString();

        Assert.AreEqual(original, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.FromBase64ToString(string, Encoding)" /> respects an explicit
    /// encoding when interpreting the decoded bytes.
    /// </summary>
    [TestMethod]
    public void FromBase64ToString_WhenAsciiEncodingSupplied_ShouldDecodeAsAscii()
    {
        var encoded = "hello".ToBase64(Encoding.ASCII);

        var actual = encoded.FromBase64ToString(Encoding.ASCII);

        Assert.AreEqual("hello", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.FromBase64ToString(string, Encoding)" /> throws
    /// <see cref="FormatException" /> when the input is not valid Base64.
    /// </summary>
    [TestMethod]
    public void FromBase64ToString_WhenInputIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = "not-base64!".FromBase64ToString();
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.FromBase64ToString(string, Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FromBase64ToString_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.FromBase64ToString(null!);
        });
    }
}
