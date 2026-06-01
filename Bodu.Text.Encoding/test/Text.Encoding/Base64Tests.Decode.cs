// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Decode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that strict Standard Base64 decoding rejects every malformed input in
    /// <see cref="Base64KnownAnswerVectors.StandardNegativeVectors" /> with the expected exception type.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base64KnownAnswerVectors.StandardNegativeVectors), typeof(Base64KnownAnswerVectors))]
    public void Decode_ForStandardKnownMalformedInput_ShouldThrowExactly(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base64.Decode(vector.MalformedInput, Base64Variant.Standard);
        }
        catch (Exception ex)
        {
            actual = ex;
        }

        Assert.IsNotNull(actual, $"No exception thrown for: {vector}");
        Assert.AreEqual(vector.ExpectedExceptionType, actual.GetType(),
            $"Wrong exception type for: {vector}; actual was {actual.GetType().Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with the Standard variant
    /// recovers the bytes for every RFC 4648 §10 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base64KnownAnswerVectors.StandardRfc4648Vectors), typeof(Base64KnownAnswerVectors))]
    public void Decode_ForStandardRfc4648KnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        var actual = Base64.Decode(vector.Encoded, Base64Variant.Standard);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with the URL-safe variant
    /// recovers the bytes for every URL-safe Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A URL-safe KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base64KnownAnswerVectors.UrlSafeVectors), typeof(Base64KnownAnswerVectors))]
    public void Decode_ForUrlSafeKnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        var actual = Base64.Decode(vector.Encoded, Base64Variant.UrlSafe);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that strict URL-safe Base64 decoding rejects every malformed input in
    /// <see cref="Base64KnownAnswerVectors.UrlSafeNegativeVectors" /> with the expected exception type.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base64KnownAnswerVectors.UrlSafeNegativeVectors), typeof(Base64KnownAnswerVectors))]
    public void Decode_ForUrlSafeKnownMalformedInput_ShouldThrowExactly(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base64.Decode(vector.MalformedInput, Base64Variant.UrlSafe);
        }
        catch (Exception ex)
        {
            actual = ex;
        }

        Assert.IsNotNull(actual, $"No exception thrown for: {vector}");
        Assert.AreEqual(vector.ExpectedExceptionType, actual.GetType(),
            $"Wrong exception type for: {vector}; actual was {actual.GetType().Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowMissingPadding" /> accepts input without trailing <c>=</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowMissingPaddingAndPaddingOmitted_ShouldDecodeSuccessfully()
    {
        var actual = Base64.Decode("Zm9vYmE", Base64Variant.Standard, BaseFormatStyles.AllowMissingPadding);

        CollectionAssert.AreEqual(Ascii("fooba"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates whitespace in the input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripWhitespaceAndDecode()
    {
        var actual = Base64.Decode(
            "Zm 9v\tYm\nFy",
            Base64Variant.Standard,
            BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
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
        var actual = Base64.Decode(decoratedInput, Base64Variant.Standard, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> rejects characters outside
    /// the Standard alphabet with <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInvalidCharacters_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm9vYm!y");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> for the MIME variant
    /// implicitly ignores whitespace (since MIME inputs always contain line breaks).
    /// </summary>
    [TestMethod]
    public void Decode_WhenMimeVariantWithLineBreaks_ShouldStripImplicitWhitespace()
    {
        var bytes = new byte[120];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        var encoded = Base64.Encode(bytes, Base64Variant.Mime);
        var decoded = Base64.Decode(encoded, Base64Variant.Mime);

        CollectionAssert.AreEqual(bytes, decoded);
    }

    /// <summary>
    /// Verifies that the decoder rejects an input containing only padding characters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenOnlyPaddingCharacters_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("====");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(char[], int, int, Base64Variant, BaseFormatStyles)" /> decodes only the
    /// bytes inside the requested slice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSliceForCharArray_ShouldReturnSliceOnly()
    {
        var chars = "????Zm9vYmFy####".ToCharArray();

        var actual = Base64.Decode(chars, 4, 8);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }
    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> reverses the RFC 4648 §10
    /// reference vectors for the Standard variant.
    /// </summary>
    /// <param name="encoded">The Base64 encoded input.</param>
    /// <param name="expected">The expected decoded ASCII string.</param>
    [TestMethod]
    [DataRow("", "")]
    [DataRow("Zg==", "f")]
    [DataRow("Zm8=", "fo")]
    [DataRow("Zm9v", "foo")]
    [DataRow("Zm9vYg==", "foob")]
    [DataRow("Zm9vYmE=", "fooba")]
    [DataRow("Zm9vYmFy", "foobar")]
    public void Decode_WhenStandardVariantReferenceVectors_ShouldRecoverInputBytes(string encoded, string expected)
    {
        var actual = Base64.Decode(encoded);

        CollectionAssert.AreEqual(Ascii(expected), actual);
    }

    /// <summary>
    /// Verifies that the Standard Base64 decoder rejects URL-safe alphabet characters that are absent from the
    /// Standard alphabet.
    /// </summary>
    /// <param name="excluded">A URL-safe-only character.</param>
    [TestMethod]
    [DataRow('-')]
    [DataRow('_')]
    public void Decode_WhenStandardVariantWithUrlSafeChar_ShouldThrowExactly(char excluded)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm" + excluded + "v");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> in strict mode rejects
    /// input that omits required padding.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndPaddingOmitted_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm9vYmE");
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
    public void Decode_WhenUrlSafeVariantWithStandardChar_ShouldThrowExactly(char excluded)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zm" + excluded + "v", Base64Variant.UrlSafe);
        });
    }

}
