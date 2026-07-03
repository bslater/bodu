// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HotpTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the <see cref="Hotp" /> primitive against the published RFC 4226 test vectors and for its
/// argument-validation and constant-time verification contract.
/// </summary>
[TestClass]
public sealed partial class HotpTests
{
    /// <summary>
    /// The 20-byte ASCII shared secret ("12345678901234567890") used by the RFC 4226 Appendix D test vectors.
    /// </summary>
    internal static readonly byte[] Rfc4226Secret = "12345678901234567890"u8.ToArray();

    /// <summary>
    /// Verifies that <see cref="Hotp.GenerateCode(ReadOnlySpan{byte}, long, int, OtpHashAlgorithm)" /> reproduces the
    /// first RFC 4226 Appendix D code for a simple SHA-1 input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GenerateCode_WhenGivenFirstRfc4226Vector_ShouldReproducePublishedCode()
    {
        string code = Hotp.GenerateCode(Rfc4226Secret, counter: 0);

        Assert.AreEqual("755224", code);
    }
}
