// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.TryEncode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.TryEncode(BencodedValue, Span{byte}, out int)" /> writes only the required
    /// number of bytes into an oversized destination buffer.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationOversized_ShouldReturnTrueAndWriteOnlyRequiredBytes()
    {
        byte[] destination = new byte[CanonicalIntegerBytes.Length + 8];

        bool result = Bencode.TryEncode(new BencodedInteger(42), destination, out int bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(CanonicalIntegerBytes.Length, bytesWritten);

        for (int i = 0; i < CanonicalIntegerBytes.Length; i++)
            Assert.AreEqual(CanonicalIntegerBytes[i], destination[i]);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.TryEncode(BencodedValue, Span{byte}, out int)" /> succeeds when the
    /// destination is sized exactly to the encoded length and reports the same length via <c>bytesWritten</c>.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationSizedExactly_ShouldReturnTrueAndWriteExactBytes()
    {
        byte[] destination = new byte[CanonicalIntegerBytes.Length];

        bool result = Bencode.TryEncode(new BencodedInteger(42), destination, out int bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(CanonicalIntegerBytes.Length, bytesWritten);
        CollectionAssert.AreEqual(CanonicalIntegerBytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryEncode(BencodedValue, Span{byte}, out int)" /> returns
    /// <see langword="false" /> and sets <c>bytesWritten</c> to zero when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseWithZeroBytesWritten()
    {
        byte[] destination = new byte[2];

        bool result = Bencode.TryEncode(new BencodedInteger(42), destination, out int bytesWritten);

        Assert.IsFalse(result);
        Assert.AreEqual(0, bytesWritten);
    }

}
