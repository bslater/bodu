// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512Tests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein512Tests
{
    /// <summary>
    /// Verifies that Skein-512-512 reproduces the canonical empty-message digest published with the Skein 1.3
    /// specification.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsEmpty_ForSkein512_ShouldMatchSpecificationVector()
    {
        using var skein = new Skein512();

        byte[] actual = skein.ComputeHash(Array.Empty<byte>());

        Assert.AreEqual(
            "BC5B4C50925519C290CC634277AE3D6257212395CBA733BBAD37A4AF0FA06AF4" +
            "1FCA7903D06564FEA7A2D3730DBDB80C1F85562DFCC070334EA4D1D9E72CBA7A",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that Skein-512-512 reproduces the BouncyCastle reference vector for a single-byte <c>0xFB</c>
    /// message, exercising the sub-block final-block padding path at the primary state size.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSingleByte_ForSkein512_ShouldMatchReferenceVector()
    {
        using var skein = new Skein512();

        byte[] actual = skein.ComputeHash(new byte[] { 0xFB });

        Assert.AreEqual(
            "C49E03D50B4B2CC46BD3B7EF7014C8A45B016399FD1714467B7596C86DE98240" +
            "E35BF7F9772B7D65465CD4CFFAB14E6BC154C54FC67B8BC340ABF08EFF572B9E",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Skein512.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein512(384);

        Assert.AreEqual("Skein-512-384", skein.AlgorithmName);
    }
}
