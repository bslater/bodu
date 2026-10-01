// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CmacTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CmacTests
{
    /// <summary>
    /// Verifies that the MAC of each RFC 4493 Section 4 example - the first 0, 16, 40, and 64 bytes of the example
    /// message under the example key - matches the published value.
    /// </summary>
    /// <param name="length">The example's message length.</param>
    /// <param name="expected">The published MAC, hex.</param>
    [TestMethod]
    [DataRow(0, "bb1d6929e95937287fa37d129b756746")]
    [DataRow(16, "070a16b46b4d4144f79bdd9dd04a287c")]
    [DataRow(40, "dfa66747de9ae63030ca32611497c827")]
    [DataRow(64, "51f0bebf7e3b9d92fc49741779363cfe")]
    public void Finish_WhenGivenRfc4493Example_ShouldMatchPublishedMac(int length, string expected)
    {
        using var cipher = new AesBlockCipher(s_rfc4493Key);

        byte[] mac = ComputeInPieces(cipher, s_rfc4493Message[..length], length);

        Assert.AreEqual(expected, Convert.ToHexString(mac).ToLowerInvariant());
    }
}
