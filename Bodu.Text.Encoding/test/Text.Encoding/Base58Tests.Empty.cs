// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" />.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    [DataTestMethod]
    [DataRow(Base58Variant.BitcoinFlickr)]
    [DataRow(Base58Variant.Ripple)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base58Variant variant)
    {
        string actual = Base58.Encode(Array.Empty<byte>(), variant);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    [DataTestMethod]
    [DataRow(Base58Variant.BitcoinFlickr)]
    [DataRow(Base58Variant.Ripple)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base58Variant variant)
    {
        byte[] actual = Base58.Decode(string.Empty, variant);

        Assert.AreEqual(0, actual.Length);
    }
}
