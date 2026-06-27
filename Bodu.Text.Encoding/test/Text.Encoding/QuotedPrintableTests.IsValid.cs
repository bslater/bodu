// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.IsValid(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> accepts
    /// well-formed input and rejects malformed input.
    /// </summary>
    /// <param name="input">The candidate input.</param>
    /// <param name="expected">The expected validity.</param>
    [TestMethod]
    [DataRow("Hello", true)]
    [DataRow("=3D", true)]
    [DataRow("abc=\r\ndef", true)]
    [DataRow("a\r\nb", true)]
    [DataRow("", true)]
    [DataRow("=", false)]
    [DataRow("=A", false)]
    [DataRow("=GG", false)]
    [DataRow("=3d", false)]
    [DataRow("a\nb", false)]
    [DataRow("abc \r\ndef", false)]
    public void IsValid_ShouldMatchExpected(string input, bool expected)
    {
        Assert.AreEqual(expected, QuotedPrintable.IsValid(input.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.IsValid(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> agrees
    /// with <see cref="QuotedPrintable.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, QuotedPrintableDecodingOptions)" />
    /// for a representative set of inputs and option combinations.
    /// </summary>
    /// <param name="input">The candidate input.</param>
    [TestMethod]
    [DataRow("Hello")]
    [DataRow("=3D")]
    [DataRow("abc=\r\ndef")]
    [DataRow("=3d")]
    [DataRow("a\nb")]
    [DataRow("abc \r\ndef")]
    [DataRow("=GG")]
    public void IsValid_ShouldAgreeWithTryDecode(string input)
    {
        foreach (QuotedPrintableDecodingOptions options in new[]
        {
            QuotedPrintableDecodingOptions.None,
            QuotedPrintableDecodingOptions.AllowLowercaseHex,
            QuotedPrintableDecodingOptions.AllowBareLineFeed,
            QuotedPrintableDecodingOptions.IgnoreTrailingWhitespace,
        })
        {
            Span<byte> destination = new byte[QuotedPrintable.GetMaxDecodedLength(input.Length)];
            bool tryDecode = QuotedPrintable.TryDecode(input.AsSpan(), destination, out _, options);

            bool isValid = QuotedPrintable.IsValid(input.AsSpan(), options);

            Assert.AreEqual(isValid, tryDecode, $"Disagreement for '{input}' with options {options}.");
        }
    }
}
