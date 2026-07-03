// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TotpTests.GenerateCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class TotpTests
{
    /// <summary>
    /// Verifies that <see cref="Totp.GenerateCode(ReadOnlySpan{byte}, DateTimeOffset, int, int, OtpHashAlgorithm)" />
    /// reproduces every eight-digit code from the RFC 6238 Appendix B test table across all three hash algorithms.
    /// </summary>
    /// <param name="unixSeconds">The Unix-time instant to generate the code for.</param>
    /// <param name="algorithm">The HMAC hash algorithm.</param>
    /// <param name="expected">The expected eight-digit code.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(59L, OtpHashAlgorithm.Sha1, "94287082")]
    [DataRow(59L, OtpHashAlgorithm.Sha256, "46119246")]
    [DataRow(59L, OtpHashAlgorithm.Sha512, "90693936")]
    [DataRow(1111111109L, OtpHashAlgorithm.Sha1, "07081804")]
    [DataRow(1111111109L, OtpHashAlgorithm.Sha256, "68084774")]
    [DataRow(1111111109L, OtpHashAlgorithm.Sha512, "25091201")]
    [DataRow(1111111111L, OtpHashAlgorithm.Sha1, "14050471")]
    [DataRow(1111111111L, OtpHashAlgorithm.Sha256, "67062674")]
    [DataRow(1111111111L, OtpHashAlgorithm.Sha512, "99943326")]
    [DataRow(1234567890L, OtpHashAlgorithm.Sha1, "89005924")]
    [DataRow(1234567890L, OtpHashAlgorithm.Sha256, "91819424")]
    [DataRow(1234567890L, OtpHashAlgorithm.Sha512, "93441116")]
    [DataRow(2000000000L, OtpHashAlgorithm.Sha1, "69279037")]
    [DataRow(2000000000L, OtpHashAlgorithm.Sha256, "90698825")]
    [DataRow(2000000000L, OtpHashAlgorithm.Sha512, "38618901")]
    [DataRow(20000000000L, OtpHashAlgorithm.Sha1, "65353130")]
    [DataRow(20000000000L, OtpHashAlgorithm.Sha256, "77737706")]
    [DataRow(20000000000L, OtpHashAlgorithm.Sha512, "47863826")]
    public void GenerateCode_WhenGivenRfc6238Vector_ShouldReproducePublishedCode(long unixSeconds, OtpHashAlgorithm algorithm, string expected)
    {
        byte[] seed = SeedFor(algorithm);

        string code = Totp.GenerateCode(seed, DateTimeOffset.FromUnixTimeSeconds(unixSeconds), digits: 8, periodSeconds: 30, algorithm: algorithm);

        Assert.AreEqual(expected, code);
    }

    /// <summary>
    /// Verifies that a period of less than one second throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>periodSeconds</c> parameter.
    /// </summary>
    [TestMethod]
    public void GenerateCode_WhenPeriodLessThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Totp.GenerateCode(Sha1Seed, DateTimeOffset.FromUnixTimeSeconds(59), digits: 8, periodSeconds: 0, algorithm: OtpHashAlgorithm.Sha1);
        });

        Assert.AreEqual("periodSeconds", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a timestamp earlier than the epoch throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>timestamp</c> parameter.
    /// </summary>
    [TestMethod]
    public void GenerateCode_WhenTimestampBeforeEpoch_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Totp.GenerateCode(Sha1Seed, DateTimeOffset.FromUnixTimeSeconds(-1));
        });

        Assert.AreEqual("timestamp", ex.ParamName);
    }
}
