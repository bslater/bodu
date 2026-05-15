// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" />.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [DataRow(Base85Variant.Ascii85)]
    [DataRow(Base85Variant.Z85)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base85Variant variant)
    {
        Assert.AreEqual(string.Empty, Base85.Encode(Array.Empty<byte>(), variant));
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [DataRow(Base85Variant.Ascii85)]
    [DataRow(Base85Variant.Z85)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base85Variant variant)
    {
        Assert.AreEqual(0, Base85.Decode(string.Empty, variant).Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode" /> and <see cref="Base85.TryDecode" /> with empty source return
    /// <see langword="true" /> with zero counts written.
    /// </summary>
    [TestMethod]
    public void TryEncodeAndTryDecode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroWritten()
    {
        Assert.IsTrue(Base85.TryEncode(ReadOnlySpan<byte>.Empty, new char[8], out int charsWritten));
        Assert.AreEqual(0, charsWritten);

        Assert.IsTrue(Base85.TryDecode(ReadOnlySpan<char>.Empty, new byte[8], out int bytesWritten));
        Assert.AreEqual(0, bytesWritten);
    }
}
