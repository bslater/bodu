// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> reverses the RFC 4648 §10
    /// reference vectors for the Standard variant.
    /// </summary>
    /// <param name="encoded">The Base64 encoded input.</param>
    /// <param name="expected">The expected decoded ASCII string.</param>
    [DataTestMethod]
    [DataRow("", "")]
    [DataRow("Zg==", "f")]
    [DataRow("Zm8=", "fo")]
    [DataRow("Zm9v", "foo")]
    [DataRow("Zm9vYg==", "foob")]
    [DataRow("Zm9vYmE=", "fooba")]
    [DataRow("Zm9vYmFy", "foobar")]
    public void Decode_WhenStandardVariantReferenceVectors_ShouldRecoverInputBytes(string encoded, string expected)
    {
        byte[] actual = Base64.Decode(encoded);

        CollectionAssert.AreEqual(Ascii(expected), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> rejects characters outside
    /// the Standard alphabet with <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInvalidCharacters_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm9vYm!y");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> accepts input without trailing <c>=</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowMissingPaddingAndPaddingOmitted_ShouldDecodeSuccessfully()
    {
        byte[] actual = Base64.Decode("Zm9vYmE", Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding);

        CollectionAssert.AreEqual(Ascii("fooba"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> in strict mode rejects
    /// input that omits required padding.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndPaddingOmitted_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm9vYmE");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates whitespace in the input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripWhitespaceAndDecode()
    {
        byte[] actual = Base64.Decode(
            "Zm 9v\tYm\nFy",
            Base64Variant.Standard,
            BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> for the MIME variant
    /// implicitly ignores whitespace (since MIME inputs always contain line breaks).
    /// </summary>
    [TestMethod]
    public void Decode_WhenMimeVariantWithLineBreaks_ShouldStripImplicitWhitespace()
    {
        byte[] bytes = new byte[120];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        string encoded = Base64.Encode(bytes, Base64Variant.Mime);
        byte[] decoded = Base64.Decode(encoded, Base64Variant.Mime);

        CollectionAssert.AreEqual(bytes, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(char[], int, int, Base64Variant, BaseFormatStyles)" /> decodes only the
    /// bytes inside the requested slice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSliceForCharArray_ShouldReturnSliceOnly()
    {
        char[] chars = "????Zm9vYmFy####".ToCharArray();

        byte[] actual = Base64.Decode(chars, 4, 8);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that the Standard Base64 decoder rejects URL-safe alphabet characters that are absent from the
    /// Standard alphabet.
    /// </summary>
    /// <param name="excluded">A URL-safe-only character.</param>
    [TestMethod]
    [DataRow('-')]
    [DataRow('_')]
    public void Decode_WhenStandardVariantWithUrlSafeChar_ShouldThrowFormatException(char excluded)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm" + excluded + "v");
        });
    }

    /// <summary>
    /// Verifies that the URL-safe Base64 decoder rejects Standard alphabet characters that are absent from the URL-
    /// safe alphabet.
    /// </summary>
    /// <param name="excluded">A Standard-only character.</param>
    [TestMethod]
    [DataRow('+')]
    [DataRow('/')]
    public void Decode_WhenUrlSafeVariantWithStandardChar_ShouldThrowFormatException(char excluded)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm" + excluded + "v", Base64Variant.UrlSafe);
        });
    }

    /// <summary>
    /// Verifies that the Standard Base64 decoder handles every individual ASCII whitespace pattern via
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" />.
    /// </summary>
    /// <param name="decoratedInput">The decorated input.</param>
    [TestMethod]
    [DataRow("Zm 9v Ym Fy")]
    [DataRow("Zm\t9v\tYm\tFy")]
    [DataRow("Zm\n9v\nYm\nFy")]
    [DataRow("Zm\r\n9v\r\nYm\r\nFy")]
    [DataRow("  Zm9vYmFy  ")]
    [DataRow("Zm9vYmFy\n")]
    [DataRow("\nZm9vYmFy")]
    public void Decode_WhenIgnoreWhitespaceAndVariousWhitespacePatterns_ShouldDecodeFoobar(string decoratedInput)
    {
        byte[] actual = Base64.Decode(decoratedInput, Base64Variant.Standard, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that the decoder rejects an input containing only padding characters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenOnlyPaddingCharacters_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("====");
        });
    }
}
