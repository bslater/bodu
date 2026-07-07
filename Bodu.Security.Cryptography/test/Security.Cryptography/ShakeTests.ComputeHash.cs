// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ShakeTests
{
    /// <summary>
    /// Verifies that SHAKE128 and SHAKE256 produce different digests for the same input and output length.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenSecurityLevelsDiffer_ShouldProduceDifferentHashes()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("test");

        using Shake shake128 = new(256, 128);
        using Shake shake256 = new(256, 256);

        byte[] hash128 = shake128.ComputeHash(input);
        byte[] hash256 = shake256.ComputeHash(input);

        CollectionAssert.AreNotEqual(hash128, hash256,
            "SHAKE128 and SHAKE256 must produce different digests for the same input.");
    }

    // ── FIPS 202 SHAKE known-answer tests over non-canonical message inputs ────────────────────
    //
    // The variant harness (ShakeTests.cs) pins the standard's empty-string vectors plus the shared
    // canonical inputs at 256-bit output. These rows add distinct message inputs (0x00, 0x00010203,
    // and the lowercase "abc" = 0x616263) and, for SHAKE256, the 512-bit output length — exercising a
    // longer squeeze than the harness covers. Expected outputs are computed from the FIPS 202 SHAKE
    // definitions (Keccak); each was independently recomputed here before pinning.

    /// <summary>
    /// Verifies that SHAKE128 produces exactly the 256-bit FIPS 202 output for the given message.
    /// </summary>
    /// <param name="messageHex">The input message bytes.</param>
    /// <param name="expectedHex">The expected 32-byte output as uppercase hex.</param>
    [TestMethod]
    [DataRow("00", "0B784469A0628E03861CD8A196DFAFA0E9E8056D04CDDCC49F0746B9AD43CCB2")]
    [DataRow("00010203", "0B0CC28E60E37698B411234B1158A5D42636440432A28E8B8DF5BE04208878F9")]
    [DataRow("616263", "5881092DD818BF5CF8A3DDB793FBCBA74097D5C526A6D35F97B83351940F2CC8")]
    public void ComputeHash_WithFips202Shake128Vector_ShouldMatch(string messageHex, string expectedHex)
    {
        using Shake shake = new(256, 128);
        byte[] actual = shake.ComputeHash(Convert.FromHexString(messageHex));

        Assert.AreEqual(expectedHex, Convert.ToHexString(actual),
            "SHAKE128 output must match the FIPS 202 reference for the given message.");
    }

    /// <summary>
    /// Verifies that SHAKE256 produces exactly the 512-bit FIPS 202 output for the given message.
    /// </summary>
    /// <param name="messageHex">The input message bytes.</param>
    /// <param name="expectedHex">The expected 64-byte output as uppercase hex.</param>
    [TestMethod]
    [DataRow("00", "B8D01DF855F7075882C636F6DDEACF41E5DE0BBF30042EF0A86E36F4B8600D546C516501A6A3C821678D3D9943FA9E74B9B99FCCD47AECC91DD1F4946B8355B3")]
    [DataRow("00010203", "48B8D57A5F8C29D0326049216380AA85D2D7A58B784F5A49E980CA93409E3D4BAC25509371F937EF3224820EDA0AF0915C10D07E2DF78BAFE7208D23F36388A9")]
    [DataRow("616263", "483366601360A8771C6863080CC4114D8DB44530F8F1E1EE4F94EA37E78B5739D5A15BEF186A5386C75744C0527E1FAA9F8726E462A12A4FEB06BD8801E751E4")]
    public void ComputeHash_WithFips202Shake256Vector_ShouldMatch(string messageHex, string expectedHex)
    {
        using Shake shake = new(512, 256);
        byte[] actual = shake.ComputeHash(Convert.FromHexString(messageHex));

        Assert.AreEqual(expectedHex, Convert.ToHexString(actual),
            "SHAKE256 output must match the FIPS 202 reference for the given message.");
    }
}
