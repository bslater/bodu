// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that the <see cref="Base16.Encode(byte[], BaseFormattingOptions)" /> and
    /// <see cref="Base16.Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> overloads produce identical output
    /// for the same input across a range of sizes.
    /// </summary>
    /// <param name="size">The size of the test input.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(16)]
    [DataRow(256)]
    public void Encode_ByteArrayAndSpanOverloads_ShouldProduceIdenticalOutput(int size)
    {
        byte[] bytes = new byte[size];
        for (int i = 0; i < size; i++)
        {
            bytes[i] = (byte)((i * 31) ^ 0xAA);
        }

        string fromArray = Base16.Encode(bytes);
        string fromSpan = Base16.Encode(bytes.AsSpan());

        Assert.AreEqual(fromArray, fromSpan);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> with the default
    /// (lower case) output matches the lower-case form of every RFC 4648 §10 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="Base16KnownAnswerVectors" />.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base16KnownAnswerVectors.Rfc4648Vectors), typeof(Base16KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Encode_ForRfc4648KnownAnswerVector_DefaultLowerCase_ShouldMatchLowerCase(EncodingKnownAnswerVector vector)
    {
        string actual = Base16.Encode(vector.DecodedBytes);

        Assert.AreEqual(vector.Encoded.ToLowerInvariant(), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> reproduces every
    /// RFC 4648 §10 Known Answer Test vector when invoked with
    /// <see cref="BaseFormattingOptions.UpperCase" />. The reference outputs are in upper case per RFC 4648 §8.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="Base16KnownAnswerVectors" />.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base16KnownAnswerVectors.Rfc4648Vectors), typeof(Base16KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Encode_ForRfc4648KnownAnswerVector_WithUpperCaseFlag_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        string actual = Base16.Encode(vector.DecodedBytes, BaseFormattingOptions.UpperCase);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Span{char}, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentException" /> when the destination is exactly one character short of the required size.
    /// </summary>
    [TestMethod]
    public void Encode_SpanDestination_WhenOneCharShort_ShouldThrowArgumentException()
    {
        byte[] bytes = new byte[4];
        char[] destination = new char[7];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Encode(bytes.AsSpan(), destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], int, int, BaseFormattingOptions)" /> rejects a count that
    /// exceeds the array length with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCountExceedsArrayLength_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base16.Encode(CanonicalBytes, 2, 10);
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], BaseFormattingOptions)" /> with the default options returns
    /// lower case hex characters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDefaultOptionsForByteArray_ShouldReturnLowerCaseHex()
    {
        string actual = Base16.Encode(CanonicalBytes);

        Assert.AreEqual(CanonicalHexLower, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Span{char}, BaseFormattingOptions)" /> throws
    /// <see cref="ArgumentException" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        char[] destination = new char[1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Encode(CanonicalBytes.AsSpan(), destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> prepends the literal <c>0x</c> prefix.
    /// </summary>
    [TestMethod]
    public void Encode_WhenIncludePrefixFlagForSpan_ShouldPrependZeroXLiteral()
    {
        string actual = Base16.Encode(CanonicalBytes.AsSpan(), BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("0xdeadbeef", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.InsertLineBreaks" /> wraps at a fixed column.
    /// </summary>
    [TestMethod]
    public void Encode_WhenInsertLineBreaksFlagForSpan_ShouldWrapAtFixedColumn()
    {
        byte[] bytes = new byte[64];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        string actual = Base16.Encode(bytes.AsSpan(), BaseFormattingOptions.InsertLineBreaks);

        string[] lines = actual.Split("\r\n");
        Assert.IsTrue(lines.Length > 1, "Output should contain at least one line break.");
        Assert.AreEqual(64, lines[0].Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.InsertSpacing" /> separates each byte with a single space, with no trailing
    /// space.
    /// </summary>
    [TestMethod]
    public void Encode_WhenInsertSpacingFlagForSpan_ShouldSeparateBytesWithSingleSpace()
    {
        string actual = Base16.Encode(CanonicalBytes.AsSpan(), BaseFormattingOptions.InsertSpacing | BaseFormattingOptions.UpperCase);

        Assert.AreEqual("DE AD BE EF", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], int, int, BaseFormattingOptions)" /> rejects negative
    /// <c>offset</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNegativeOffsetForByteArray_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base16.Encode(CanonicalBytes, -1, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], int, int, BaseFormattingOptions)" /> rejects a slice whose
    /// offset plus count overflows the array bounds with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenOffsetPlusCountOverflows_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Encode(CanonicalBytes, 2, 4);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], int, int, BaseFormattingOptions)" /> returns
    /// <see cref="string.Empty" /> when <c>count</c> is zero.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceCountIsZero_ShouldReturnEmptyString()
    {
        string actual = Base16.Encode(CanonicalBytes, 0, 0);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], int, int, BaseFormattingOptions)" /> encodes only the bytes
    /// inside the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        string actual = Base16.Encode(CanonicalBytes, 1, 2);

        Assert.AreEqual("adbe", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.UpperCase" /> returns upper case hex characters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUpperCaseFlagForByteArray_ShouldReturnUpperCaseHex()
    {
        string actual = Base16.Encode(CanonicalBytes, BaseFormattingOptions.UpperCase);

        Assert.AreEqual(CanonicalHexUpper, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Span{char}, BaseFormattingOptions)" /> writes the
    /// correct number of characters into a pre-allocated destination.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpan_ShouldReturnExactCharCount()
    {
        char[] destination = new char[CanonicalBytes.Length * 2];

        int charsWritten = Base16.Encode(CanonicalBytes.AsSpan(), destination);

        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual(CanonicalHexLower, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Span{char}, BaseFormattingOptions)" /> rejects
    /// unsupported formatting flags with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpanWithUnsupportedFlags_ShouldThrowArgumentException()
    {
        char[] destination = new char[CanonicalBytes.Length * 2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Encode(CanonicalBytes.AsSpan(), destination, BaseFormattingOptions.InsertSpacing);
        });
    }

}
