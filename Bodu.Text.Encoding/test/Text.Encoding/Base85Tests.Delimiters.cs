// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Delimiters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Tests for the Adobe Ascii85 <c>&lt;~ ... ~&gt;</c> delimiter handling on the
/// <see cref="BaseFormattingOptions.IncludePrefix" /> encode flag and the corresponding
/// <see cref="BaseFormatStyles.AllowPrefix" /> decode style.
/// </summary>
public sealed partial class Base85Tests
{

    /// <summary>
    /// The canonical Adobe Ascii85 vector "Man " (the first four bytes of Adobe Tech Note 5045's example string).
    /// </summary>
    private const string ManAscii85 = "9jqo^";

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> tolerates the Ascii85 delimiter pair.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85AndAllowPrefix_ShouldAcceptDelimitedInput()
    {
        var decoded = Base85.Decode(
            ("<~" + ManAscii85 + "~>").AsSpan(),
            Base85Variant.Ascii85,
            BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> with both
    /// <see cref="BaseFormatStyles.AllowPrefix" /> and <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips
    /// whitespace surrounding the delimiters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85AndAllowPrefixAndIgnoreWhitespace_ShouldStripSurroundingWhitespace()
    {
        var decoded = Base85.Decode(
            ("  <~" + ManAscii85 + "~>  ").AsSpan(),
            Base85Variant.Ascii85,
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> without
    /// <see cref="BaseFormatStyles.AllowPrefix" /> rejects an input with the delimiter pair — the leading <c>&lt;</c>
    /// is outside the alphabet's value range.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85AndDelimitersWithoutAllowPrefix_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode(("<~" + ManAscii85 + "~>").AsSpan(), Base85Variant.Ascii85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> wraps Ascii85 output in the
    /// <c>&lt;~</c> / <c>~&gt;</c> delimiter pair.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85AndIncludePrefix_ShouldEmitAdobeDelimiters()
    {
        var payload = System.Text.Encoding.ASCII.GetBytes("Man ");
        var encoded = Base85.Encode(payload, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("<~" + ManAscii85 + "~>", encoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" /> without
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> emits no delimiters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85AndNoIncludePrefix_ShouldOmitDelimiters()
    {
        var payload = System.Text.Encoding.ASCII.GetBytes("Man ");
        var encoded = Base85.Encode(payload, Base85Variant.Ascii85);

        Assert.AreEqual(ManAscii85, encoded);
    }

    /// <summary>
    /// Verifies that the empty-payload delimited encoding produces just the four-character delimiter pair.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85EmptyPayloadAndIncludePrefix_ShouldReturnDelimiterOnly()
    {
        var encoded = Base85.Encode(ReadOnlySpan<byte>.Empty, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("<~~>", encoded);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormattingOptions.IncludePrefix" /> is ignored for the
    /// <see cref="Base85Variant.Z85" /> variant — Z85 has no delimiter convention.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZ85AndIncludePrefix_ShouldIgnoreFlag()
    {
        var payload = new byte[] { 0x86, 0x4F, 0xD2, 0x6F };
        var withPrefix = Base85.Encode(payload, Base85Variant.Z85, BaseFormattingOptions.IncludePrefix);
        var withoutPrefix = Base85.Encode(payload, Base85Variant.Z85);

        Assert.AreEqual(withoutPrefix, withPrefix);
        Assert.IsFalse(withPrefix.StartsWith("<~", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.GetEncodedLength(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />
    /// reports the four extra characters when delimiters are requested.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenAscii85AndIncludePrefix_ShouldAddFourDelimiterCharacters()
    {
        var payload = System.Text.Encoding.ASCII.GetBytes("Man ");
        var undecorated = Base85.GetEncodedLength(payload, Base85Variant.Ascii85);
        var decorated = Base85.GetEncodedLength(payload, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual(undecorated + 4, decorated);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> returns <see langword="true" /> for delimited input when
    /// <see cref="BaseFormatStyles.AllowPrefix" /> is set.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85DelimitedAndAllowPrefix_ShouldReturnTrue() => Assert.IsTrue(Base85.IsValid(("<~" + ManAscii85 + "~>").AsSpan(), Base85Variant.Ascii85, BaseFormatStyles.AllowPrefix));

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> returns <see langword="false" /> for delimited input when
    /// <see cref="BaseFormatStyles.AllowPrefix" /> is not set.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85DelimitedWithoutAllowPrefix_ShouldReturnFalse() => Assert.IsFalse(Base85.IsValid(("<~" + ManAscii85 + "~>").AsSpan(), Base85Variant.Ascii85));

    /// <summary>
    /// Verifies that the delimited encoded form round-trips through encode and decode.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAscii85WithDelimiters_ShouldRecoverPayload()
    {
        var payload = new byte[256];
        new Random(0xFEED).NextBytes(payload);

        var encoded = Base85.Encode(payload, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        Assert.IsTrue(encoded.StartsWith("<~", StringComparison.Ordinal));
        Assert.IsTrue(encoded.EndsWith("~>", StringComparison.Ordinal));

        var decoded = Base85.Decode(encoded.AsSpan(), Base85Variant.Ascii85, BaseFormatStyles.AllowPrefix);
        CollectionAssert.AreEqual(payload, decoded);
    }

}
