// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> for every variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [DataTestMethod]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base64Variant variant)
    {
        string actual = Base64.Encode(Array.Empty<byte>(), variant);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array for every variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [DataTestMethod]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base64Variant variant)
    {
        byte[] actual = Base64.Decode(string.Empty, variant);

        Assert.AreEqual(0, actual.Length);
    }
}
