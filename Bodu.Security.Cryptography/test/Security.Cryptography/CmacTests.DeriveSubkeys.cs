// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CmacTests.DeriveSubkeys.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CmacTests
{
    /// <summary>
    /// Verifies that the subkeys derived under the RFC 4493 example key match the published <c>K1</c> and <c>K2</c>.
    /// </summary>
    [TestMethod]
    public void DeriveSubkeys_WhenGivenRfc4493Key_ShouldMatchPublishedSubkeys()
    {
        using var cipher = new AesBlockCipher(s_rfc4493Key);
        byte[] k1 = new byte[16];
        byte[] k2 = new byte[16];

        Cmac.DeriveSubkeys(cipher, k1, k2);

        Assert.AreEqual("fbeed618357133667c85e08f7236a8de", Convert.ToHexString(k1).ToLowerInvariant());
        Assert.AreEqual("f7ddac306ae266ccf90bc11ee46d513b", Convert.ToHexString(k2).ToLowerInvariant());
    }
}
