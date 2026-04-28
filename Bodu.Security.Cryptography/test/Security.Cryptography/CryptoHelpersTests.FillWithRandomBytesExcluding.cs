// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.FillWithRandomBytesExcluding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomBytesExcluding" /> fills a span without the forbidden byte.
    /// </summary>
    [TestMethod]
    public void FillWithRandomBytesExcluding_WhenForbiddenByteIsGiven_ShouldNotBeInResult()
    {
        Span<byte> span = stackalloc byte[64];
        CryptoHelpers.FillWithRandomBytesExcluding(0xFF, span);
        foreach (byte b in span)
        {
            Assert.AreNotEqual(0xFF, b);
        }
    }
}
