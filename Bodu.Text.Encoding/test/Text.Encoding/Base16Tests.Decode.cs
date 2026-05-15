// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that strict-mode <see cref="Base16.Decode(string, BaseFormatStyles)" /> rejects every malformed input
    /// in <see cref="Base16KnownAnswerVectors.NegativeVectors" /> with the expected exception type.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base16KnownAnswerVectors.NegativeVectors), typeof(Base16KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForKnownMalformedInput_ShouldThrowExpectedException(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = Base16.Decode(vector.MalformedInput);
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
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> recovers the original bytes for every
    /// RFC 4648 §10 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base16KnownAnswerVectors.Rfc4648Vectors), typeof(Base16KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForRfc4648KnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = Base16.Decode(vector.Encoded);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that the string and span overloads of <see cref="Base16.Decode(string, BaseFormatStyles)" /> and
    /// <see cref="Base16.Decode(ReadOnlySpan{char}, BaseFormatStyles)" /> produce identical output.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Decode_StringAndSpanOverloads_ShouldProduceIdenticalOutput()
    {
        string encoded = "deadbeefcafebabe";

        byte[] fromString = Base16.Decode(encoded);
        byte[] fromSpan = Base16.Decode(encoded.AsSpan());

        CollectionAssert.AreEqual(fromString, fromSpan);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with both leniency flags accepts a fully
    /// decorated input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndIgnoreWhitespace_ShouldStripBoth()
    {
        byte[] actual = Base16.Decode(
            "0x DE AD BE EF",
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> preserves a leading <c>0</c> digit when no <c>x</c> follows.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndLeadingZeroNoX_ShouldPreserveDigit()
    {
        byte[] actual = Base16.Decode("0FAB", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(new byte[] { 0x0F, 0xAB }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> strips a leading <c>0x</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndPrefixPresent_ShouldStripPrefix()
    {
        byte[] actual = Base16.Decode("0xDEADBEEF", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> tolerates the upper case <c>0X</c> prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndUpperCasePrefix_ShouldStripPrefix()
    {
        byte[] actual = Base16.Decode("0XDEADBEEF", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> rejects a count larger than the
    /// array length with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCountExceedsCharArrayLength_ShouldThrowArgumentOutOfRangeException()
    {
        char[] chars = "abcd".ToCharArray();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base16.Decode(chars, 2, 10);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips spaces, tabs, and newlines.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripAllAsciiWhitespace()
    {
        byte[] actual = Base16.Decode("DE AD\tBE\nEF\r", BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that the decoder rejects characters that are common look-alikes but not valid hex digits.
    /// </summary>
    /// <param name="invalidInput">The malformed input under test.</param>
    [TestMethod]
    [DataRow("g0")]
    [DataRow("G0")]
    [DataRow("h0")]
    [DataRow("ab/cd")]
    [DataRow("ab.cd")]
    [DataRow("ab-cd")]
    [DataRow("ab_cd")]
    [DataRow("ab cd")]
    [DataRow("«cd")] // non-ASCII letter outside the alphabet
    public void Decode_WhenInputContainsInvalidCharacters_ShouldThrowFormatException(string invalidInput)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode(invalidInput);
        });
    }

    /// <summary>
    /// Verifies that lenient decoding still rejects an odd digit count after decorations are stripped.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLenientYieldsOddDigitCount_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0x ABC", BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> rejects a slice whose offset
    /// plus count overflows the array bounds with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenOffsetPlusCountOverflowsCharArray_ShouldThrowArgumentException()
    {
        char[] chars = "abcd".ToCharArray();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Decode(chars, 2, 3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(ReadOnlySpan{char}, BaseFormatStyles)" /> decodes a stack-allocated
    /// span correctly.
    /// </summary>
    [TestMethod]
    public void Decode_WhenReadOnlySpan_ShouldReturnExpectedBytes()
    {
        ReadOnlySpan<char> chars = CanonicalHexLower.AsSpan();

        byte[] actual = Base16.Decode(chars);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> decodes only the bytes inside
    /// the requested slice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSliceForCharArray_ShouldReturnSliceOnly()
    {
        char[] chars = "00deadbeef00".ToCharArray();

        byte[] actual = Base16.Decode(chars, 2, 8);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> in strict mode rejects a prefix-decorated
    /// input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndPrefixPresent_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0xDEADBEEF");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> throws <see cref="FormatException" />
    /// for any non-hex character.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictInvalidCharacter_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("xx");
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with the default strict mode decodes the
    /// canonical lower case input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictLowerCaseString_ShouldReturnExpectedBytes()
    {
        byte[] actual = Base16.Decode(CanonicalHexLower);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> decodes mixed-case input without
    /// requiring a flag.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictMixedCaseString_ShouldReturnExpectedBytes()
    {
        byte[] actual = Base16.Decode("DeAdBeEf");

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> throws <see cref="FormatException" />
    /// when the input has an odd number of hex digits.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictOddLengthString_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("abc");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> decodes upper case input without
    /// requiring a flag.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictUpperCaseString_ShouldReturnExpectedBytes()
    {
        byte[] actual = Base16.Decode(CanonicalHexUpper);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> returns
    /// <see langword="false" /> for every malformed input in <see cref="Base16KnownAnswerVectors.NegativeVectors" />.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base16KnownAnswerVectors.NegativeVectors), typeof(Base16KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void IsValid_ForKnownMalformedInput_ShouldReturnFalse(EncodingNegativeDecodeVector vector)
    {
        Assert.IsFalse(Base16.IsValid(vector.MalformedInput.AsSpan()),
            $"IsValid should return false for: {vector}");
    }

}
