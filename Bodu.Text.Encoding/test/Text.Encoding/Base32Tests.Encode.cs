// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Encode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that the byte-array and span overloads of <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" />
    /// and <see cref="Base32.Encode(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" /> produce identical
    /// output across a range of sizes.
    /// </summary>
    /// <param name="size">The size of the test input.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(16)]
    [DataRow(100)]
    public void Encode_ByteArrayAndSpanOverloads_ShouldProduceIdenticalOutput(int size)
    {
        var bytes = new byte[size];
        for (var i = 0; i < size; i++)
        {
            bytes[i] = (byte)((i * 17) ^ 0x55);
        }

        var fromArray = Base32.Encode(bytes);
        var fromSpan = Base32.Encode(bytes.AsSpan());

        Assert.AreEqual(fromArray, fromSpan);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" /> with the
    /// HexExtended variant reproduces every RFC 4648 §10 base32hex vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base32KnownAnswerVectors.HexExtendedRfc4648Vectors), typeof(Base32KnownAnswerVectors))]
    public void Encode_ForHexExtendedRfc4648KnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        var actual = Base32.Encode(vector.DecodedBytes, Base32Variant.HexExtended);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Base32Variant, BaseFormattingOptions)" /> with the
    /// Standard variant reproduces every RFC 4648 §10 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(Base32KnownAnswerVectors.StandardRfc4648Vectors), typeof(Base32KnownAnswerVectors))]
    public void Encode_ForStandardRfc4648KnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        var actual = Base32.Encode(vector.DecodedBytes, Base32Variant.Standard);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], int, int, Base32Variant, BaseFormattingOptions)" /> rejects an
    /// out-of-range count with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCountExceedsArrayLength_ShouldThrowExactly()
    {
        var bytes = new byte[4];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.Encode(bytes, 0, 100);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Span{char}, Base32Variant, BaseFormattingOptions)" />
    /// throws <see cref="ArgumentException" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationTooSmall_ShouldThrowExactly()
    {
        var bytes = Ascii("foobar");
        var destination = new char[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base32.Encode(bytes.AsSpan(), destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.InsertLineBreaks" /> wraps the output at a fixed column.
    /// </summary>
    [TestMethod]
    public void Encode_WhenInsertLineBreaksFlag_ShouldWrapAtFixedColumn()
    {
        var bytes = new byte[100];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        var actual = Base32.Encode(bytes, Base32Variant.Standard, BaseFormattingOptions.InsertLineBreaks);
        var lines = actual.Split("\r\n");

        Assert.IsTrue(lines.Length > 1, "Output should contain at least one line break.");
        Assert.AreEqual(64, lines[0].Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], int, int, Base32Variant, BaseFormattingOptions)" /> rejects a
    /// negative offset.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNegativeOffset_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.Encode(new byte[4], -1, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.OmitPadding" /> drops the trailing <c>=</c> characters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenOmitPaddingFlag_ShouldNotEmitPaddingCharacters()
    {
        var actual = Base32.Encode(Ascii("foo"), Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual("MZXW6", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], int, int, Base32Variant, BaseFormattingOptions)" /> only
    /// encodes the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        var bytes = Ascii("xxxxfoobaryyyy");

        var actual = Base32.Encode(bytes, 4, 6);

        Assert.AreEqual("MZXW6YTBOI======", actual);
    }
    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> produces the
    /// published RFC 4648 §10 Base32 reference vectors for the Standard variant.
    /// </summary>
    /// <param name="input">The input ASCII string.</param>
    /// <param name="expected">The expected Base32 output.</param>
    [TestMethod]
    [DataRow("", "")]
    [DataRow("f", "MY======")]
    [DataRow("fo", "MZXQ====")]
    [DataRow("foo", "MZXW6===")]
    [DataRow("foob", "MZXW6YQ=")]
    [DataRow("fooba", "MZXW6YTB")]
    [DataRow("foobar", "MZXW6YTBOI======")]
    public void Encode_WhenStandardVariant_ShouldMatchRfc4648ReferenceVectors(string input, string expected)
    {
        var actual = Base32.Encode(Ascii(input));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> rejects an undefined
    /// variant.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUndefinedVariant_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.Encode(Ascii("foo"), (Base32Variant)99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Span{char}, Base32Variant, BaseFormattingOptions)" />
    /// writes the encoded characters into a destination span.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpan_ShouldReturnExactCharCount()
    {
        var bytes = Ascii("foobar");
        var destination = new char[Base32.GetEncodedLength(bytes.Length)];

        var charsWritten = Base32.Encode(bytes.AsSpan(), destination);

        Assert.AreEqual("MZXW6YTBOI======", new string(destination, 0, charsWritten));
    }

}
