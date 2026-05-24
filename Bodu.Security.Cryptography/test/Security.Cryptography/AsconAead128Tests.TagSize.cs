// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.TagSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── TagSize ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.TagSize" /> returns 128 (16 bytes), as required by
    /// NIST SP 800-232 for Ascon-AEAD128.
    /// </summary>
    [TestMethod]
    public void TagSize_ShouldReturn128()
    {
        using var sut = new AsconAead128(ValidKey, ValidNonce);
        Assert.AreEqual(128, sut.TagSize);
    }
}
