// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TotpTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the <see cref="Totp" /> primitive against the published RFC 6238 test vectors and for its
/// argument-validation and clock-drift-window contract.
/// </summary>
[TestClass]
public sealed partial class TotpTests
{
    // RFC 6238 Appendix B uses a distinct seed per algorithm, sized to the HMAC block: the ASCII digit run
    // "1234567890..." truncated to 20, 32, and 64 bytes for SHA-1, SHA-256, and SHA-512 respectively. Using the SHA-1
    // seed for all three is a common implementation bug, so each is pinned explicitly.
    private static readonly byte[] Sha1Seed = "12345678901234567890"u8.ToArray();
    private static readonly byte[] Sha256Seed = "12345678901234567890123456789012"u8.ToArray();
    private static readonly byte[] Sha512Seed = "1234567890123456789012345678901234567890123456789012345678901234"u8.ToArray();

    /// <summary>
    /// Verifies that <see cref="Totp.GenerateCode(ReadOnlySpan{byte}, DateTimeOffset, int, int, OtpHashAlgorithm)" />
    /// reproduces the first RFC 6238 Appendix B code.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GenerateCode_WhenGivenFirstRfc6238Vector_ShouldReproducePublishedCode()
    {
        string code = Totp.GenerateCode(Sha1Seed, DateTimeOffset.FromUnixTimeSeconds(59), digits: 8, periodSeconds: 30, algorithm: OtpHashAlgorithm.Sha1);

        Assert.AreEqual("94287082", code);
    }

    /// <summary>
    /// Returns the RFC 6238 Appendix B seed for the specified algorithm.
    /// </summary>
    /// <param name="algorithm">The algorithm whose seed is required.</param>
    /// <returns>The seed bytes.</returns>
    private static byte[] SeedFor(OtpHashAlgorithm algorithm) => algorithm switch
    {
        OtpHashAlgorithm.Sha1 => Sha1Seed,
        OtpHashAlgorithm.Sha256 => Sha256Seed,
        OtpHashAlgorithm.Sha512 => Sha512Seed,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };
}
