// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> reproduces the
    /// RFC 4648 §10 Base64 reference vectors for the Standard variant.
    /// </summary>
    /// <param name="input">The input ASCII string.</param>
    /// <param name="expected">The expected Base64 output.</param>
    [DataTestMethod]
    [DataRow("", "")]
    [DataRow("f", "Zg==")]
    [DataRow("fo", "Zm8=")]
    [DataRow("foo", "Zm9v")]
    [DataRow("foob", "Zm9vYg==")]
    [DataRow("fooba", "Zm9vYmE=")]
    [DataRow("foobar", "Zm9vYmFy")]
    public void Encode_WhenStandardVariant_ShouldMatchRfc4648ReferenceVectors(string input, string expected)
    {
        string actual = Base64.Encode(Ascii(input));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> with
    /// <see cref="BaseFormattingOptions.OmitPadding" /> drops the trailing <c>=</c> characters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenOmitPadding_ShouldNotEmitPaddingCharacters()
    {
        string actual = Base64.Encode(Ascii("foo"), Base64Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual("Zm9v", actual);

        string padded = Base64.Encode(Ascii("foob"), Base64Variant.Standard, BaseFormattingOptions.OmitPadding);
        Assert.AreEqual("Zm9vYg", padded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> with the
    /// <see cref="Base64Variant.Mime" /> variant inserts a <c>\r\n</c> sequence every 76 characters.
    /// </summary>
    [TestMethod]
    public void Encode_WhenMimeVariant_ShouldInsertLineBreaksEvery76Chars()
    {
        byte[] bytes = new byte[120];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        string actual = Base64.Encode(bytes, Base64Variant.Mime);
        string[] lines = actual.Split("\r\n");

        Assert.IsTrue(lines.Length > 1, "MIME output should contain at least one line break.");
        Assert.AreEqual(76, lines[0].Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(ReadOnlySpan{byte}, Span{char}, Base64Variant, BaseFormattingOptions)" />
    /// writes the encoded characters into a destination span.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpan_ShouldReturnExactCharCount()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[Base64.GetEncodedLength(bytes.Length)];

        int charsWritten = Base64.Encode(bytes.AsSpan(), destination);

        Assert.AreEqual("Zm9vYmFy", new string(destination, 0, charsWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(ReadOnlySpan{byte}, Span{char}, Base64Variant, BaseFormattingOptions)" />
    /// throws <see cref="ArgumentException" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        byte[] bytes = Ascii("foobar");
        char[] destination = new char[1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base64.Encode(bytes.AsSpan(), destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> rejects an undefined
    /// variant.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base64.Encode(Ascii("foo"), (Base64Variant)99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], int, int, Base64Variant, BaseFormattingOptions)" /> only
    /// encodes the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        byte[] bytes = Ascii("xxxxfoobaryyyy");

        string actual = Base64.Encode(bytes, 4, 6);

        Assert.AreEqual("Zm9vYmFy", actual);
    }
}
