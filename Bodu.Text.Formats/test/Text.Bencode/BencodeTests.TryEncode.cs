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
        var destination = new byte[CanonicalIntegerBytes.Length + 8];

        var result = Bencode.TryEncode(new BencodedInteger(42), destination, out var bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(CanonicalIntegerBytes.Length, bytesWritten);

        for (var i = 0; i < CanonicalIntegerBytes.Length; i++)
            Assert.AreEqual(CanonicalIntegerBytes[i], destination[i]);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.TryEncode(BencodedValue, Span{byte}, out int)" /> succeeds when the
    /// destination is sized exactly to the encoded length and reports the same length via <c>bytesWritten</c>.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationSizedExactly_ShouldReturnTrueAndWriteExactBytes()
    {
        var destination = new byte[CanonicalIntegerBytes.Length];

        var result = Bencode.TryEncode(new BencodedInteger(42), destination, out var bytesWritten);

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
        var destination = new byte[2];

        var result = Bencode.TryEncode(new BencodedInteger(42), destination, out var bytesWritten);

        Assert.IsFalse(result);
        Assert.AreEqual(0, bytesWritten);
    }

}
