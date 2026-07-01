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
    /// Verifies that whenever <see cref="QuotedPrintable.IsValid(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" />
    /// returns <see langword="true" /> (canonical), the lenient
    /// <see cref="QuotedPrintable.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, QuotedPrintableDecodingOptions)" />
    /// also succeeds. The converse does not hold — canonical validity is strictly stronger than decodability.
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
    public void IsValid_WhenTrue_ShouldImplyTryDecodeSucceeds(string input)
    {
        foreach (QuotedPrintableDecodingOptions options in new[]
        {
            QuotedPrintableDecodingOptions.None,
            QuotedPrintableDecodingOptions.AllowLowercaseHex,
            QuotedPrintableDecodingOptions.AllowBareLineFeed,
            QuotedPrintableDecodingOptions.IgnoreTrailingWhitespace,
        })
        {
            if (!QuotedPrintable.IsValid(input.AsSpan(), options))
                continue;

            Span<byte> destination = new byte[QuotedPrintable.GetMaxDecodedLength(input.Length)];
            bool tryDecode = QuotedPrintable.TryDecode(input.AsSpan(), destination, out _, options);

            Assert.IsTrue(tryDecode, $"IsValid implied decodability but TryDecode failed for '{input}' with options {options}.");
        }
    }

    /// <summary>
    /// Verifies that canonical validation rejects an encoded line longer than the RFC 2045 76-character limit.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldRejectEncodedLineLongerThan76()
    {
        Assert.IsFalse(QuotedPrintable.IsValid(new string('A', 77).AsSpan()));
    }

    /// <summary>
    /// Verifies that canonical validation accepts an encoded line of exactly 76 characters.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptEncodedLineOfExactly76Characters()
    {
        Assert.IsTrue(QuotedPrintable.IsValid(new string('A', 76).AsSpan()));
    }

    /// <summary>
    /// Verifies that the soft-break <c>=</c> counts within the 76-character limit: 75 content characters plus the
    /// soft-break marker fills the line exactly.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldCountSoftBreakMarkerWithinLineLimit()
    {
        Assert.IsTrue(QuotedPrintable.IsValid((new string('A', 75) + "=\r\nB").AsSpan()));
    }

    /// <summary>
    /// Verifies that the soft-break <c>=</c> pushing a line to 77 characters is rejected.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldRejectSoftBreakMarker_WhenItMakesLine77Characters()
    {
        Assert.IsFalse(QuotedPrintable.IsValid((new string('A', 76) + "=\r\n").AsSpan()));
    }

    /// <summary>
    /// Verifies that the lenient decoder still recovers an overlong line that canonical validation rejects.
    /// </summary>
    [TestMethod]
    public void Decode_ShouldRecoverOverlongLine_ThatIsValidRejects()
    {
        string overlong = new('A', 200);

        byte[] decoded = QuotedPrintable.Decode(overlong.AsSpan());

        CollectionAssert.AreEqual(Ascii(overlong), decoded);
        Assert.IsFalse(QuotedPrintable.IsValid(overlong.AsSpan()));
    }

    /// <summary>
    /// Verifies that default-wrapped encoder output is always canonical, so its lines satisfy the 76-character limit.
    /// </summary>
    [TestMethod]
    public void IsValid_ShouldAcceptDefaultEncoderOutput()
    {
        string encoded = QuotedPrintable.Encode(Ascii(new string('A', 500) + " trailing"));

        Assert.IsTrue(QuotedPrintable.IsValid(encoded.AsSpan()));
    }
}
