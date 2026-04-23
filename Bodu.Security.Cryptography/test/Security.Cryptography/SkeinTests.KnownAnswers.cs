// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class SkeinTests
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
            "1FCA7903D06564FEA7A2D3730DBDB80C1F85562DFCC070334EA4D1D9E72CBE7B",
            Convert.ToHexString(actual));
    }

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
    /// Verifies that Skein-256-256 reproduces the Skein 1.3 specification vector for a single-byte <c>0xFF</c>
    /// message, exercising the final-block padding path for a message that is strictly shorter than one Threefish-256
    /// block.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSingleByte_ForSkein256_ShouldMatchSpecificationVector()
    {
        using var skein = new Skein256();

        byte[] actual = skein.ComputeHash(new byte[] { 0xFF });

        Assert.AreEqual(
            "0B98DCD198EA0E50A7A244C444E25C23DA30C10FC9A1F270A6637F1F34E67ED2",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that Skein-512-512 reproduces the Skein 1.3 specification vector for a single-byte <c>0xFF</c>
    /// message, exercising the single-block final UBI path for a sub-block message.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSingleByte_ForSkein512_ShouldMatchSpecificationVector()
    {
        using var skein = new Skein512();

        byte[] actual = skein.ComputeHash(new byte[] { 0xFF });

        Assert.AreEqual(
            "71B7BCE6FE6452227B9CED6014249E5BF9A9754C3AD618CCC4E0AAE16B316CC8" +
            "CA698D8644307ED3E80B6EF1570812AC5272DC409B5A012DF2A579102F340617",
            Convert.ToHexString(actual));
    }

    /// <summary>
    /// Verifies that Skein-1024-1024 reproduces the Skein 1.3 specification vector for a single-byte <c>0xFF</c>
    /// message, exercising the single-block final UBI path at the widest state size.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSingleByte_ForSkein1024_ShouldMatchSpecificationVector()
    {
        using var skein = new Skein1024();

        byte[] actual = skein.ComputeHash(new byte[] { 0xFF });

        Assert.AreEqual(
            "E62C05802EA0152407CDD8787FDA9E35703DE862A4FBC119CFF8590AFE79250B" +
            "CCC8B3FAF1BD2422AB5C0D03611BFF1F79D12432F31DCA0F78D8FE26B3C31B9B" +
            "CA0EFF7D61B3BB76D3A906D2D8C8F3CDC4E3B12C5A5C2F1FF35CD54FAE3D5C1D" +
            "9DE0A2A0A96E11B4C3E1E6D82CC7D7D5E6A4E2C14E8EFBDD52B42DB3D65A8F1F",
            Convert.ToHexString(actual));
    }
}
