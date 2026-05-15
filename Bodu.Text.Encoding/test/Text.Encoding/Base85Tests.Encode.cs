// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{
    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Ascii85 variant produces the
    /// expected 5-character output for a complete 4-byte group ("Hell").
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85VariantCompleteGroup_ShouldReturnFiveCharacters()
    {
        byte[] bytes = Ascii("Hell");

        string actual = Base85.Encode(bytes);

        Assert.AreEqual(5, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Ascii85 variant emits the <c>z</c>
    /// shortcut for a 4-zero-byte input.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAscii85VariantFourZeroBytes_ShouldEmitZShortcut()
    {
        byte[] bytes = new byte[4];

        string actual = Base85.Encode(bytes);

        Assert.AreEqual("z", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Ascii85 variant emits
    /// (1 + remaining) characters for a partial trailing group.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedCharCount">The expected encoded character count.</param>
    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 4)]
    [DataRow(5, 7)]
    [DataRow(8, 10)]
    [DataRow(9, 12)]
    public void Encode_WhenAscii85PartialGroup_ShouldEmitExpectedLength(int byteCount, int expectedCharCount)
    {
        byte[] bytes = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
            bytes[i] = (byte)(i + 1);

        string actual = Base85.Encode(bytes);

        Assert.AreEqual(expectedCharCount, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Z85 variant rejects non-aligned
    /// input.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZ85VariantAndNonAlignedInput_ShouldThrowArgumentException()
    {
        byte[] bytes = new byte[5];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.Encode(bytes, Base85Variant.Z85);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> with the Z85 variant emits 5 characters
    /// per 4-byte group.
    /// </summary>
    [TestMethod]
    public void Encode_WhenZ85VariantAlignedInput_ShouldEmitFiveCharsPerFourBytes()
    {
        byte[] bytes = new byte[8];

        string actual = Base85.Encode(bytes, Base85Variant.Z85);

        Assert.AreEqual(10, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], int, int, Base85Variant)" /> only encodes the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        byte[] bytes = Ascii("xxxxHelloyyy");

        string actual = Base85.Encode(bytes, 4, 5);

        string expected = Base85.Encode(Ascii("Hello"));
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> rejects an undefined variant.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base85.Encode(new byte[4], (Base85Variant)99);
        });
    }
}
