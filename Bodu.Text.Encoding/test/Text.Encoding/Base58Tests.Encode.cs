// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that the byte-array and span overloads produce identical output across various input sizes.
    /// </summary>
    /// <param name="size">The input size.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(32)]
    [DataRow(64)]
    public void Encode_ByteArrayAndSpanOverloads_ShouldProduceIdenticalOutput(int size)
    {
        byte[] bytes = new byte[size];
        for (int i = 0; i < size; i++)
        {
            bytes[i] = (byte)((i * 19) ^ 0x3C);
        }

        string fromArray = Base58.Encode(bytes);
        string fromSpan = Base58.Encode(bytes.AsSpan());

        Assert.AreEqual(fromArray, fromSpan);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(ReadOnlySpan{byte}, Base58Variant)" /> reproduces every Bitcoin Core
    /// Known Answer Test vector for the Bitcoin/Flickr alphabet.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [DataTestMethod]
    [DynamicData(nameof(Base58KnownAnswerVectors.BitcoinFlickrVectors), typeof(Base58KnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Encode_ForBitcoinFlickrKnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        string actual = Base58.Encode(vector.DecodedBytes, Base58Variant.BitcoinFlickr);

        Assert.AreEqual(vector.Encoded, actual);
    }
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
    /// Verifies that <see cref="Base58.Encode(byte[], int, int, Base58Variant)" /> rejects an out-of-range count.
    /// </summary>
    [TestMethod]
    public void Encode_WhenCountExceedsArrayLength_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base58.Encode(new byte[4], 0, 100);
        });
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base58.Encode(ReadOnlySpan{byte}, Span{char}, Base58Variant)" /> with
    /// a tightly-sized destination matching the actual output succeeds rather than throwing.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationExactlyActualSize_ShouldSucceed()
    {
        byte[] bytes = Ascii("Hello");
        char[] destination = new char[7]; // actual encoded length, smaller than the worst-case max

        int charsWritten = Base58.Encode(bytes.AsSpan(), destination);

        Assert.AreEqual(7, charsWritten);
        Assert.AreEqual("9Ajdvzr", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], int, int, Base58Variant)" /> rejects a negative offset.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNegativeOffset_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base58.Encode(new byte[4], -1, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], int, int, Base58Variant)" /> rejects a slice that overflows
    /// the array bounds with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Encode_WhenOffsetPlusCountOverflows_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base58.Encode(new byte[4], 2, 4);
        });
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

    /// <summary>
    /// Regression: verifies that <see cref="Base58.Encode(ReadOnlySpan{byte}, Span{char}, Base58Variant)" /> and
    /// <see cref="Base58.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base58Variant)" /> succeed when the
    /// destination is exactly the actual output size (rather than the worst-case upper bound). Before the fix, a
    /// tightly-sized destination would be rejected even though the encoded result fits.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationExactlyActualSize_ShouldReturnTrue()
    {
        byte[] bytes = Ascii("Hello"); // encodes to "9Ajdvzr" — 7 chars

        // Destination is exactly the actual encoded size (7), well below the worst-case upper bound for 5 bytes.
        char[] destination = new char[7];

        bool ok = Base58.TryEncode(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok, "TryEncode should succeed when destination is at least the actual encoded length.");
        Assert.AreEqual(7, charsWritten);
        Assert.AreEqual("9Ajdvzr", new string(destination));
    }

}
