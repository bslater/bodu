// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{
    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], Base58Variant)" /> reproduces known Bitcoin/Flickr Base58
    /// vectors for selected inputs.
    /// </summary>
    /// <param name="hexInput">The input bytes expressed as hex.</param>
    /// <param name="expected">The expected Base58 output.</param>
    [DataTestMethod]
    [DataRow("00", "1")]
    [DataRow("0000", "11")]
    [DataRow("0001", "12")]
    [DataRow("39", "z")]
    [DataRow("48656c6c6f", "9Ajdvzr")]
    public void Encode_WhenBitcoinFlickrVariantKnownVectors_ShouldReturnExpectedOutput(string hexInput, string expected)
    {
        byte[] bytes = Convert.FromHexString(hexInput);

        string actual = Base58.Encode(bytes);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], int, int, Base58Variant)" /> encodes only the requested slice.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSliceForByteArray_ShouldReturnSliceOnly()
    {
        byte[] bytes = Ascii("xxxHelloyyy");

        string actual = Base58.Encode(bytes, 3, 5);

        Assert.AreEqual("9Ajdvzr", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], Base58Variant)" /> rejects an undefined variant.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUndefinedVariant_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base58.Encode(new byte[] { 0x01 }, (Base58Variant)99);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(ReadOnlySpan{byte}, Span{char}, Base58Variant)" /> writes the encoded
    /// characters into a destination span.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpan_ShouldReturnExactCharCount()
    {
        byte[] bytes = Ascii("Hello");
        char[] destination = new char[Base58.GetMaxEncodedLength(bytes.Length)];

        int charsWritten = Base58.Encode(bytes.AsSpan(), destination);

        Assert.AreEqual("9Ajdvzr", new string(destination, 0, charsWritten));
    }
}
