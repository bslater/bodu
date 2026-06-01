// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.FillWithRandomBytesExcluding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.FillWithRandomBytesExcluding" /> fills a span without the forbidden byte.
    /// </summary>
    [TestMethod]
    public void FillWithRandomBytesExcluding_WhenForbiddenByteIsGiven_ShouldNotBeInResult()
    {
        Span<byte> span = stackalloc byte[64];
        CryptographyHelper.FillWithRandomBytesExcluding(0xFF, span);
        foreach (var b in span)
        {
            Assert.AreNotEqual(0xFF, b);
        }
    }
}
