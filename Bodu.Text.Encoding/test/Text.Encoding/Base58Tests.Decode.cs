// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Decode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> recovers the bytes for
    /// every Bitcoin Core Known Answer Test vector for the Bitcoin/Flickr alphabet.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base58KnownAnswerVectors.BitcoinFlickrVectors), typeof(Base58KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForBitcoinFlickrKnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        var actual = Base58.Decode(vector.Encoded, Base58Variant.BitcoinFlickr);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that strict Bitcoin/Flickr Base58 decoding rejects every malformed input in
    /// <see cref="Base58KnownAnswerVectors.BitcoinFlickrNegativeVectors" /> with the expected exception type.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base58KnownAnswerVectors.BitcoinFlickrNegativeVectors), typeof(Base58KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForBitcoinFlickrKnownMalformedInput_ShouldThrowExactly(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base58.Decode(vector.MalformedInput, Base58Variant.BitcoinFlickr);
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
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> reverses the known
    /// Bitcoin/Flickr Base58 vectors.
    /// </summary>
    /// <param name="encoded">The Base58 input.</param>
    /// <param name="expectedHex">The expected decoded bytes expressed as hex.</param>
    [DataTestMethod]
    [DataRow("1", "00")]
    [DataRow("11", "0000")]
    [DataRow("12", "0001")]
    [DataRow("z", "39")]
    [DataRow("9Ajdvzr", "48656c6c6f")]
    public void Decode_WhenBitcoinFlickrVariantKnownVectors_ShouldReturnExpectedBytes(string encoded, string expectedHex)
    {
        var expected = Convert.FromHexString(expectedHex);

        var actual = Base58.Decode(encoded);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates whitespace.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripAndDecode()
    {
        var actual = Base58.Decode(
            "9 Aj\tdv\nzr",
            Base58Variant.BitcoinFlickr,
            BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("Hello"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> handles every individual ASCII whitespace pattern.
    /// </summary>
    /// <param name="decoratedInput">The decorated input.</param>
    [TestMethod]
    [DataRow("9 Aj dv zr")]
    [DataRow("9\tAj\tdv\tzr")]
    [DataRow("9\nAj\ndv\nzr")]
    [DataRow("9\rAj\rdv\rzr")]
    [DataRow("  9Ajdvzr  ")]
    public void Decode_WhenIgnoreWhitespaceAndVariousWhitespacePatterns_ShouldStripAndDecodeHello(string decoratedInput)
    {
        var actual = Base58.Decode(decoratedInput, Base58Variant.BitcoinFlickr, BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(Ascii("Hello"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> rejects each character
    /// that the Bitcoin/Flickr alphabet intentionally omits.
    /// </summary>
    /// <param name="excluded">The excluded character.</param>
    [TestMethod]
    [DataRow('0')]
    [DataRow('O')]
    [DataRow('I')]
    [DataRow('l')]
    [DataRow('!')]
    [DataRow('@')]
    [DataRow(' ')]  // strict mode rejects whitespace
    public void Decode_WhenInputContainsExcludedCharacter_ShouldThrowExactly(char excluded)
    {
        var input = "2" + excluded + "2";

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58.Decode(input);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> rejects characters outside
    /// the variant alphabet.
    /// </summary>
    [TestMethod]
    public void Decode_WhenInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58.Decode("0OIl"); // every character is intentionally excluded from Bitcoin/Flickr Base58
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(char[], int, int, Base58Variant, BaseFormatStyles)" /> decodes only
    /// the requested slice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSliceForCharArray_ShouldReturnSliceOnly()
    {
        var chars = "????9Ajdvzr####".ToCharArray();

        var actual = Base58.Decode(chars, 4, 7);

        CollectionAssert.AreEqual(Ascii("Hello"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> rejects an undefined
    /// variant.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUndefinedVariant_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base58.Decode("9Ajdvzr", (Base58Variant)99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.IsValid(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" /> returns
    /// <see langword="false" /> for every malformed Bitcoin/Flickr input.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base58KnownAnswerVectors.BitcoinFlickrNegativeVectors), typeof(Base58KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void IsValid_ForBitcoinFlickrKnownMalformedInput_ShouldReturnFalse(EncodingNegativeDecodeVector vector)
    {
        Assert.IsFalse(Base58.IsValid(vector.MalformedInput.AsSpan(), Base58Variant.BitcoinFlickr),
            $"IsValid should return false for: {vector}");
    }

}
