// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.GetDecodedLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that <see cref="Base32.GetDecodedLength" /> returns the length
    /// <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> produces when it succeeds, and throws
    /// <see cref="FormatException" /> when it fails, in every variant and every combination of styles.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenSweptAcrossVariantsAndStyles_ShouldMatchDecode()
    {
        var disagreements = new List<string>();

        foreach (Base32Variant variant in Enum.GetValues<Base32Variant>())
        {
            foreach (var input in GetValidationSweepInputs(variant))
            {
                foreach (BaseFormatStyles styles in DecodeAgreement.AllStyles)
                {
                    DecodeAgreement.TryDecode(() => Base32.Decode(input, variant, styles), out int decodedLength);
                    DecodeAgreement.TryDecode(() => new byte[Base32.GetDecodedLength(input, variant, styles)], out int reportedLength);
                    if (reportedLength != decodedLength)
                        disagreements.Add($"{variant} [{styles}] {DecodeAgreement.Quote(input)}: GetDecodedLength {reportedLength}, Decode {decodedLength} (-1: FormatException)");
                }
            }
        }

        DecodeAgreement.AssertNone(disagreements);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryGetDecodedLength" /> returns <see langword="true" /> exactly when
    /// <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> succeeds, and then reports the length it
    /// produces, in every variant and every combination of styles.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenSweptAcrossVariantsAndStyles_ShouldMatchDecode()
    {
        var disagreements = new List<string>();

        foreach (Base32Variant variant in Enum.GetValues<Base32Variant>())
        {
            foreach (var input in GetValidationSweepInputs(variant))
            {
                foreach (BaseFormatStyles styles in DecodeAgreement.AllStyles)
                {
                    DecodeAgreement.TryDecode(() => Base32.Decode(input, variant, styles), out int decodedLength);
                    var reportedLength = Base32.TryGetDecodedLength(input, out int byteCount, variant, styles) ? byteCount : -1;
                    if (reportedLength != decodedLength)
                        disagreements.Add($"{variant} [{styles}] {DecodeAgreement.Quote(input)}: TryGetDecodedLength {reportedLength}, Decode {decodedLength} (-1: rejected)");
                }
            }
        }

        DecodeAgreement.AssertNone(disagreements);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.GetDecodedLength" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips whitespace before counting.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenIgnoreWhitespace_ShouldExcludeWhitespace()
    {
        int actual = Base32.GetDecodedLength(
            "MZ XW\t6Y\nTBOI======".AsSpan(),
            Base32Variant.Standard,
            BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(6, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.GetDecodedLength" /> rejects characters outside the alphabet with
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.GetDecodedLength("MZXW@".AsSpan());
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base32.GetDecodedLength" /> returns the exact byte count for a valid padded input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenPaddedInput_ShouldReturnExactByteCount()
    {
        int actual = Base32.GetDecodedLength("MZXW6YTBOI======".AsSpan());

        Assert.AreEqual(6, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.GetDecodedLength" /> returns the exact byte count for an unpadded input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenUnpaddedInput_ShouldExcludePaddingFromCount()
    {
        int actual = Base32.GetDecodedLength("MZXW6YTB".AsSpan());

        Assert.AreEqual(5, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.GetEncodedLength(int)" /> (the no-options overload) returns the Standard
    /// variant length.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WithoutOptionsOverload_ShouldReturnStandardLength() => Assert.AreEqual(16, Base32.GetEncodedLength(6));

    /// <summary>
    /// Verifies that <see cref="Base32.GetEncodedLength(int, Base32Variant)" /> respects the variant default padding
    /// convention.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WithVariantOverload_ShouldRespectVariantDefaults()
    {
        int standard = Base32.GetEncodedLength(6, Base32Variant.Standard);
        int crockford = Base32.GetEncodedLength(6, Base32Variant.Crockford);

        Assert.AreEqual(16, standard);
        Assert.IsLessThan(standard, crockford, "Crockford output (no padding) should be shorter than Standard (padded).");
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryGetDecodedLength" /> returns <see langword="false" /> for invalid input.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenInvalid_ShouldReturnFalseAndZero()
    {
        bool ok = Base32.TryGetDecodedLength("MZXW@".AsSpan(), out int byteCount);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryGetDecodedLength" /> returns <see langword="true" /> for valid input and
    /// reports the byte count.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenValid_ShouldReturnTrueAndCount()
    {
        bool ok = Base32.TryGetDecodedLength("MZXW6YTBOI======".AsSpan(), out int byteCount);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, byteCount);
    }

}
