// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{
    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> with Ascii85 expands the
    /// <c>z</c> shortcut into four zero bytes.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85ZShortcut_ShouldExpandToFourZeroBytes()
    {
        byte[] actual = Base85.Decode("z");

        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects characters outside
    /// the Ascii85 alphabet (a control character below ASCII <c>'!'</c>).
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85InvalidCharacter_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("\x01\x02\x03\x04\x05");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects characters outside
    /// the variant alphabet (using a definitely-invalid character).
    /// </summary>
    [TestMethod]
    public void Decode_WhenCharBelowAlphabetRange_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("\x01\x02\x03\x04\x05");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects misplaced
    /// <c>z</c> shortcut characters.
    /// </summary>
    [TestMethod]
    public void Decode_WhenZShortcutInMiddleOfGroup_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("9jz"); // 'z' appearing after non-zero data in a group
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> with Z85 rejects
    /// non-aligned input length.
    /// </summary>
    [TestMethod]
    public void Decode_WhenZ85NonAlignedInput_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("HelloW", Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates whitespace.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripAndDecode()
    {
        // First encode some bytes
        byte[] original = Ascii("Hello world!");
        string encoded = Base85.Encode(original);

        // Inject whitespace
        string decorated = string.Concat(encoded.Select((c, i) => i % 2 == 0 ? c.ToString() + " " : c.ToString()));

        byte[] decoded = Base85.Decode(decorated, Base85Variant.Ascii85, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that the Ascii85 decoder rejects a single trailing character because two characters are the smallest
    /// valid Base85 final group.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85TrailingSingleChar_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("9");
        });
    }

    /// <summary>
    /// Verifies that the Ascii85 decoder rejects an input that contains a six-character sequence (a complete 5-char
    /// group plus a single trailing character — invalid).
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85SixCharSequenceWithTrailingSingleChar_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("uuuuuu");
        });
    }

    /// <summary>
    /// Verifies that the Ascii85 decoder rejects characters above ASCII <c>'u'</c> (i.e. outside the alphabet upper
    /// bound).
    /// </summary>
    [TestMethod]
    public void Decode_WhenAscii85CharAboveAlphabetRange_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("9jqov"); // 'v' is ASCII 118, just above the 33-117 Ascii85 range
        });
    }

    /// <summary>
    /// Verifies that the Z85 decoder rejects each non-Z85 character.
    /// </summary>
    /// <param name="excluded">A character outside the Z85 alphabet.</param>
    [TestMethod]
    [DataRow('"')]
    [DataRow('\'')]
    [DataRow('\\')]
    [DataRow(';')]
    public void Decode_WhenZ85AlphabetExcludedCharacter_ShouldThrowFormatException(char excluded)
    {
        string input = "abcd" + excluded;

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode(input, Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> rejects an undefined
    /// variant.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base85.Decode("9jqo>", (Base85Variant)99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> recovers the bytes for
    /// every Adobe Ascii85 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base85KnownAnswerVectors.Ascii85Vectors), typeof(Base85KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForAscii85KnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = Base85.Decode(vector.Encoded, Base85Variant.Ascii85);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> recovers the bytes for
    /// every Z85 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base85KnownAnswerVectors.Z85Vectors), typeof(Base85KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForZ85KnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = Base85.Decode(vector.Encoded, Base85Variant.Z85);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that strict Ascii85 decoding rejects every malformed input in
    /// <see cref="Base85KnownAnswerVectors.Ascii85NegativeVectors" /> with the expected exception type.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base85KnownAnswerVectors.Ascii85NegativeVectors), typeof(Base85KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForAscii85KnownMalformedInput_ShouldThrowExpectedException(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base85.Decode(vector.MalformedInput, Base85Variant.Ascii85);
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
    /// Verifies that strict Z85 decoding rejects every malformed input in
    /// <see cref="Base85KnownAnswerVectors.Z85NegativeVectors" /> with the expected exception type.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base85KnownAnswerVectors.Z85NegativeVectors), typeof(Base85KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForZ85KnownMalformedInput_ShouldThrowExpectedException(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base85.Decode(vector.MalformedInput, Base85Variant.Z85);
        }
        catch (Exception ex)
        {
            actual = ex;
        }

        Assert.IsNotNull(actual, $"No exception thrown for: {vector}");
        Assert.AreEqual(vector.ExpectedExceptionType, actual.GetType(),
            $"Wrong exception type for: {vector}; actual was {actual.GetType().Name}.");
    }
}
