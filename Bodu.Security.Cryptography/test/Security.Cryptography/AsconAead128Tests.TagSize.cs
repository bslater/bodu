// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.TagSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── TagSize ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.TagSize" /> returns 16 (128 bits), as required by
    /// NIST SP 800-232 for Ascon-AEAD128.
    /// </summary>
    [TestMethod]
    public void TagSize_ShouldReturn16()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        Assert.AreEqual(16, sut.TagSize);
    }
}
