// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.FormatStyles.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that strict mode (<see cref="BaseFormatStyles.None" />) accepts only clean digit input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndCleanDigits_ShouldDecodeSuccessfully()
    {
        byte[] actual = Base16.Decode("deadbeef");

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that strict mode rejects whitespace anywhere in the input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndWhitespacePresent_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("de ad");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates space, tab, carriage return, and line
    /// feed characters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespaceAndAllAsciiWhitespace_ShouldStripAndDecode()
    {
        byte[] actual = Base16.Decode("de\tad\rbe\nef ", BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> does not strip non-ASCII whitespace such as a
    /// non-breaking space; such input is rejected as invalid.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespaceAndNonAsciiWhitespace_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("de ad", BaseFormatStyles.IgnoreWhitespace);
        });
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
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> tolerates the absence of a prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndNoPrefix_ShouldDecodeSuccessfully()
    {
        byte[] actual = Base16.Decode("deadbeef", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.AllowPrefix" /> does not strip a literal <c>0x</c> if the input
    /// continues with non-hex characters after the prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndInvalidCharsAfterPrefix_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0xZZ", BaseFormatStyles.AllowPrefix);
        });
    }
}
