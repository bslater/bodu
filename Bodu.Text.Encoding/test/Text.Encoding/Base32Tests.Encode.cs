// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> produces the
    /// published RFC 4648 §10 Base32 reference vectors for the Standard variant.
    /// </summary>
    /// <param name="input">The input ASCII string.</param>
    /// <param name="expected">The expected Base32 output.</param>
    [DataTestMethod]
    [DataRow("", "")]
    [DataRow("f", "MY======")]
    [DataRow("fo", "MZXQ====")]
    [DataRow("foo", "MZXW6===")]
    [DataRow("foob", "MZXW6YQ=")]
    [DataRow("fooba", "MZXW6YTB")]
    [DataRow("foobar", "MZXW6YTBOI======")]
    public void Encode_WhenStandardVariant_ShouldMatchRfc4648ReferenceVectors(string input, string expected)
    {
        string actual = Base32.Encode(Ascii(input));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.OmitPadding" /> drops the trailing <c>=</c> characters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenOmitPaddingFlag_ShouldNotEmitPaddingCharacters()
    {
        string actual = Base32.Encode(Ascii("foo"), Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual("MZXW6", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.InsertLineBreaks" /> wraps the output at a fixed column.
    /// </summary>
    [TestMethod]
    public void Encode_WhenInsertLineBreaksFlag_ShouldWrapAtFixedColumn()
    {
        byte[] bytes = new byte[100];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        string actual = Base32.Encode(bytes, Base32Variant.Standard, BaseFormattingOptions.InsertLineBreaks);
        string[] lines = actual.Split("\r\n");

        Assert.IsTrue(lines.Length > 1, "Output should contain at least one line break.");
        Assert.AreEqual(64, lines[0].Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], int, int, Base32Variant, BaseFormattingOptions)" /> only
    /// encodes the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        byte[] bytes = Ascii("xxxxfoobaryyyy");

        string actual = Base32.Encode(bytes, 4, 6);

        Assert.AreEqual("MZXW6YTBOI======", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], int, int, Base32Variant, BaseFormattingOptions)" /> rejects an
    /// out-of-range count with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCountExceedsArrayLength_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] bytes = new byte[4];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.Encode(bytes, 0, 100);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Span{char}, Base32Variant, BaseFormattingOptions)" />
    /// writes the encoded characters into a destination span.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpan_ShouldReturnExactCharCount()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[Base32.GetEncodedLength(bytes.Length)];

        int charsWritten = Base32.Encode(bytes.AsSpan(), destination);

        Assert.AreEqual("MZXW6YTBOI======", new string(destination, 0, charsWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(ReadOnlySpan{byte}, Span{char}, Base32Variant, BaseFormattingOptions)" />
    /// throws <see cref="ArgumentException" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base32.Encode(bytes.AsSpan(), destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> rejects an undefined
    /// variant.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base32.Encode(Ascii("foo"), (Base32Variant)99);
        });
    }
}
