// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein1024Tests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Skein1024Tests
{
    /// <summary>
    /// Verifies that Skein-1024-1024 reproduces the canonical empty-message digest published with the Skein 1.3
    /// specification.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsEmpty_ForSkein1024_ShouldMatchSpecificationVector()
    {
        using var skein = new Skein1024();

        byte[] actual = skein.ComputeHash(Array.Empty<byte>());

        Assert.AreEqual(
            "0FFF9563BB3279289227AC77D319B6FFF8D7E9F09DA1247B72A0A265CD6D2A62" +
            "645AD547ED8193DB48CFF847C06494A03F55666D3B47EB4C20456C9373C86297" +
            "D630D5578EBD34CB40991578F9F52B18003EFA35D3DA6553FF35DB91B81AB890" +
            "BEC1B189B7F52CB2A783EBB7D823D725B0B4A71F6824E88F68F982EEFC6D19C6",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that Skein-1024-1024 reproduces the BouncyCastle reference vector for a single-byte <c>0xFB</c>
    /// message, exercising the sub-block final-block padding path at the widest state size.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSingleByte_ForSkein1024_ShouldMatchReferenceVector()
    {
        using var skein = new Skein1024();

        byte[] actual = skein.ComputeHash(new byte[] { 0xFB });

        Assert.AreEqual(
            "6426BDC57B2771A6EF1B0DD39F8096A9A07554565743AC3DE851D28258FCFF22" +
            "9993E11C4E6BEBC8B6ECB0AD1B140276081AA390EC3875960336119427827473" +
            "4770671B79F076771E2CFDAAF5ADC9B10CBAE43D8E6CD2B1C1F5D6C82DC96618" +
            "00DDC476F25865B8748253173187D81DA971C027D91D32FB390301C2110D2DB2",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Skein1024.AlgorithmName" /> formats the state size and the configured output size.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConfiguredWithOutputSize_ShouldReturnFormattedName()
    {
        using var skein = new Skein1024(512);

        Assert.AreEqual("Skein-1024-512", skein.AlgorithmName);
    }
}
