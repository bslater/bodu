// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> for every variant.
    /// </summary>
    /// <param name="variant">The Base32 variant.</param>
    [DataTestMethod]
    [DataRow(Base32Variant.Standard)]
    [DataRow(Base32Variant.HexExtended)]
    [DataRow(Base32Variant.Crockford)]
    [DataRow(Base32Variant.ZBase32)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base32Variant variant)
    {
        string actual = Base32.Encode(Array.Empty<byte>(), variant);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array for every variant.
    /// </summary>
    /// <param name="variant">The Base32 variant.</param>
    [DataTestMethod]
    [DataRow(Base32Variant.Standard)]
    [DataRow(Base32Variant.HexExtended)]
    [DataRow(Base32Variant.Crockford)]
    [DataRow(Base32Variant.ZBase32)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base32Variant variant)
    {
        byte[] actual = Base32.Decode(string.Empty, variant);

        Assert.AreEqual(0, actual.Length);
    }
}
