// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> reverses the RFC 4648 §10
    /// Standard reference vectors.
    /// </summary>
    /// <param name="encoded">The Base32 encoded input.</param>
    /// <param name="expected">The expected decoded ASCII string.</param>
    [DataTestMethod]
    [DataRow("", "")]
    [DataRow("MY======", "f")]
    [DataRow("MZXQ====", "fo")]
    [DataRow("MZXW6===", "foo")]
    [DataRow("MZXW6YQ=", "foob")]
    [DataRow("MZXW6YTB", "fooba")]
    [DataRow("MZXW6YTBOI======", "foobar")]
    public void Decode_WhenStandardVariantReferenceVectors_ShouldRecoverInputBytes(string encoded, string expected)
    {
        byte[] actual = Base32.Decode(encoded);

        CollectionAssert.AreEqual(Ascii(expected), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> in the Standard variant is
    /// case-insensitive.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLowerCaseInput_ShouldDecodeCaseInsensitively()
    {
        byte[] actual = Base32.Decode("mzxw6ytboi======");

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> rejects characters outside
    /// the variant alphabet with <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInvalidCharacters_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZXW1YTB"); // '1' is not in the Standard alphabet
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> rejects misplaced padding
    /// (padding that appears before any data character past the last data character).
    /// </summary>
    [TestMethod]
    public void Decode_WhenPaddingBeforeDataEnds_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZ=XW6YT");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> accepts input without trailing <c>=</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowMissingPaddingAndPaddingOmitted_ShouldDecodeSuccessfully()
    {
        byte[] actual = Base32.Decode("MZXW6YTBOI", Base32Variant.Standard, BaseFormatStyles.AllowMissingPadding);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> in strict mode rejects
    /// input that omits required padding.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndPaddingOmitted_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZXW6YTBOI");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates whitespace anywhere in the input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripAsciiWhitespaceAndDecode()
    {
        byte[] actual = Base32.Decode(
            "MZ XW\t6Y\nTBOI======\r",
            Base32Variant.Standard,
            BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(char[], int, int, Base32Variant, BaseFormatStyles)" /> decodes only the
    /// bytes inside the requested slice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSliceForCharArray_ShouldReturnSliceOnly()
    {
        char[] chars = "????MZXW6YTBOI======####".ToCharArray();

        byte[] actual = Base32.Decode(chars, 4, 16);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> rejects an undefined
    /// variant.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.Decode("MZXW6YTBOI======", (Base32Variant)99);
        });
    }

    /// <summary>
    /// Verifies that the Standard Base32 decoder rejects each character that is excluded from the RFC 4648 §6
    /// alphabet (digits 0, 1, 8, 9 are intentionally absent).
    /// </summary>
    /// <param name="excluded">An excluded character.</param>
    [TestMethod]
    [DataRow('0')]
    [DataRow('1')]
    [DataRow('8')]
    [DataRow('9')]
    [DataRow('!')]
    [DataRow('@')]
    [DataRow('#')]
    [DataRow('-')]
    [DataRow('_')]
    public void Decode_WhenStandardAlphabetExcludedCharacter_ShouldThrowFormatException(char excluded)
    {
        string input = "MZ" + excluded + "W6YTB";

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode(input);
        });
    }

    /// <summary>
    /// Verifies that the Standard Base32 decoder accepts the same alphabet characters in both upper and lower case
    /// across the full alphabet range.
    /// </summary>
    /// <param name="input">Mixed-case input.</param>
    /// <param name="expectedAscii">The expected decoded ASCII string.</param>
    [TestMethod]
    [DataRow("MZXW6YTBOI======", "foobar")]
    [DataRow("mzxw6ytboi======", "foobar")]
    [DataRow("MzXw6YtBoI======", "foobar")]
    [DataRow("mZxW6yTbOi======", "foobar")]
    public void Decode_WhenMixedCaseInput_ShouldDecodeCaseInsensitively(string input, string expectedAscii)
    {
        byte[] actual = Base32.Decode(input);

        CollectionAssert.AreEqual(Ascii(expectedAscii), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> handles every individual ASCII whitespace character.
    /// </summary>
    /// <param name="decoratedInput">The decorated input.</param>
    [TestMethod]
    [DataRow("MZ XW 6Y TB OI ======")]
    [DataRow("MZ\tXW\t6Y\tTB\tOI\t======")]
    [DataRow("MZ\nXW\n6Y\nTB\nOI\n======")]
    [DataRow("MZ\rXW\r6Y\rTB\rOI\r======")]
    [DataRow("  MZXW6YTBOI======  ")]
    [DataRow("\r\nMZXW6YTBOI======\r\n")]
    public void Decode_WhenIgnoreWhitespaceAndVariousWhitespacePatterns_ShouldDecodeFoobar(string decoratedInput)
    {
        byte[] actual = Base32.Decode(decoratedInput, Base32Variant.Standard, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }
}
