// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256Tests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein256Tests
{
    /// <summary>
    /// Verifies that Skein-256-256 reproduces the canonical empty-message digest published with the Skein 1.3
    /// specification and carried by every mainstream reference implementation.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsEmpty_ForSkein256_ShouldMatchSpecificationVector()
    {
        using var skein = new Skein256();

        byte[] actual = skein.ComputeHash(Array.Empty<byte>());

        Assert.AreEqual(
            "C8877087DA56E072870DAA843F176E9453115929094C3A40C463A196C29BF7BA",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that Skein-256-256 reproduces the BouncyCastle reference vector for a single-byte <c>0xFB</c>
    /// message, exercising the sub-block final-block padding path at the smallest state size.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSingleByte_ForSkein256_ShouldMatchReferenceVector()
    {
        using var skein = new Skein256();

        byte[] actual = skein.ComputeHash(new byte[] { 0xFB });

        Assert.AreEqual(
            "088EB23CC2BCCFB8171AA64E966D4AF937325167DFCD170700FFD21F8A4CBDAC",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Skein256.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein256(224);

        Assert.AreEqual("Skein-256-224", skein.AlgorithmName);
    }
}
