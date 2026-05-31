// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.FormatStyles.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that combined prefix-and-whitespace stripping works when the prefix is followed by whitespace.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndIgnoreWhitespaceAndPrefixFollowedByWhitespace_ShouldDecode()
    {
        var actual = Base16.Decode(
            "0x  DE AD BE EF",
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> only consumes a leading <c>0x</c> at the start of the
    /// input; later <c>0x</c> sequences are not stripped.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndInternalPrefix_ShouldRejectInternalPrefix()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("ab0xcd", BaseFormatStyles.AllowPrefix);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> does not strip a literal <c>0x</c> if the input
    /// continues with non-hex characters after the prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndInvalidCharsAfterPrefix_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0xZZ", BaseFormatStyles.AllowPrefix);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> tolerates the absence of a prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndNoPrefix_ShouldDecodeSuccessfully()
    {
        var actual = Base16.Decode("deadbeef", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> rejects truncated prefix forms (just <c>0</c> with
    /// no following <c>x</c>, in the presence of no other content).
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndOnlyLeadingZeroPresent_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0", BaseFormatStyles.AllowPrefix);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> matches both lower and upper case prefix forms.
    /// </summary>
    /// <param name="prefix">The prefix variant.</param>
    [TestMethod]
    [DataRow("0x")]
    [DataRow("0X")]
    public void Decode_WhenAllowPrefixAndPrefixCaseVariants_ShouldStripPrefix(string prefix)
    {
        var actual = Base16.Decode(prefix + "DEADBEEF", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that whitespace inside the prefix (e.g. <c>"0 x..."</c>) is treated as a leading <c>0</c> followed by
    /// data: only the literal two-character <c>0x</c> sequence is recognised as a prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndWhitespaceBetweenZeroAndX_ShouldNotTreatAsPrefix()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode(
                "0 xDEADBEEF",
                BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates space, tab, carriage return, and line
    /// feed characters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespaceAndAllAsciiWhitespace_ShouldStripAndDecode()
    {
        var actual = Base16.Decode("de\tad\rbe\nef ", BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> does not strip non-ASCII whitespace such as a
    /// non-breaking space; such input is rejected as invalid.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespaceAndNonAsciiWhitespace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("de ad", BaseFormatStyles.IgnoreWhitespace);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> does NOT strip non-ASCII whitespace
    /// characters such as no-break space (U+00A0) or zero-width space (U+200B).
    /// </summary>
    /// <param name="whitespaceChar">A non-ASCII whitespace character.</param>
    [TestMethod]
    [DataRow(' ')] // no-break space
    [DataRow(' ')] // em space
    [DataRow('​')] // zero-width space
    [DataRow('　')] // ideographic space
    public void Decode_WhenIgnoreWhitespaceAndNonAsciiWhitespaceCharacter_ShouldThrowExactly(char whitespaceChar)
    {
        var input = "DE" + whitespaceChar + "AD";

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode(input, BaseFormatStyles.IgnoreWhitespace);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> handles each individual ASCII whitespace
    /// category (space, tab, carriage return, line feed) and combinations thereof.
    /// </summary>
    /// <param name="input">The decorated input.</param>
    [TestMethod]
    [DataRow("DE AD BE EF")]
    [DataRow("DE\tAD\tBE\tEF")]
    [DataRow("DE\rAD\rBE\rEF")]
    [DataRow("DE\nAD\nBE\nEF")]
    [DataRow("DE\r\nAD\r\nBE\r\nEF")]
    [DataRow("  DEADBEEF  ")]
    [DataRow("DEADBEEF\n")]
    [DataRow("\nDEADBEEF")]
    [DataRow("D E A D B E E F")]
    [DataRow("DE  AD  BE  EF")] // multiple consecutive spaces
    public void Decode_WhenIgnoreWhitespaceAndVariousWhitespacePatterns_ShouldStripAndDecode(string input)
    {
        var actual = Base16.Decode(input, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }
    /// <summary>
    /// Verifies that strict mode (<see cref="BaseFormatStyles.None" />) accepts only clean digit input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndCleanDigits_ShouldDecodeSuccessfully()
    {
        var actual = Base16.Decode("deadbeef");

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that strict mode rejects whitespace anywhere in the input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndWhitespacePresent_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("de ad");
        });
    }

}
