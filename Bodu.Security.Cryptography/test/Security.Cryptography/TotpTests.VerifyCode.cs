// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TotpTests.VerifyCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class TotpTests
{
    /// <summary>
    /// Verifies that a code generated for the current time step is accepted at that same instant.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCodeMatchesCurrentStep_ShouldReturnTrue()
    {
        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        string code = Totp.GenerateCode(Sha1Seed, now);

        Assert.IsTrue(Totp.VerifyCode(Sha1Seed, code, now));
    }

    /// <summary>
    /// Verifies that a code from the immediately preceding time step is accepted one step later within the drift window,
    /// and that the matched step offset is reported as <c>-1</c>.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCodeFromPreviousStepWithinWindow_ShouldReturnTrueAndReportOffset()
    {
        var issued = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var oneStepLater = issued.AddSeconds(30);
        string code = Totp.GenerateCode(Sha1Seed, issued);

        bool result = Totp.VerifyCode(Sha1Seed, code, oneStepLater, window: 1, out int matchedStepOffset);

        Assert.IsTrue(result);
        Assert.AreEqual(-1, matchedStepOffset);
    }

    /// <summary>
    /// Verifies that a code older than the drift window is rejected and the matched step offset is reported as <c>0</c>.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCodeOutsideWindow_ShouldReturnFalse()
    {
        var issued = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var muchLater = issued.AddSeconds(90);
        string code = Totp.GenerateCode(Sha1Seed, issued);

        bool result = Totp.VerifyCode(Sha1Seed, code, muchLater, window: 1, out int matchedStepOffset);

        Assert.IsFalse(result);
        Assert.AreEqual(0, matchedStepOffset);
    }

    /// <summary>
    /// Verifies that a negative window throws <see cref="ArgumentOutOfRangeException" /> naming the <c>window</c>
    /// parameter.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenWindowNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Totp.VerifyCode(Sha1Seed, "94287082", DateTimeOffset.FromUnixTimeSeconds(59), window: -1);
        });

        Assert.AreEqual("window", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a window exceeding the verification ceiling throws <see cref="ArgumentOutOfRangeException" />
    /// rather than admitting an unbounded HMAC scan.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenWindowExceedsCeiling_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Totp.VerifyCode(Sha1Seed, "94287082", DateTimeOffset.FromUnixTimeSeconds(59), window: int.MaxValue);
        });

        Assert.AreEqual("window", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="OtpHashAlgorithm" /> value throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>algorithm</c> parameter.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenAlgorithmUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Totp.VerifyCode(Sha1Seed, "94287082", DateTimeOffset.FromUnixTimeSeconds(59), window: 1, digits: 8, periodSeconds: 30, algorithm: (OtpHashAlgorithm)42);
        });

        Assert.AreEqual("algorithm", ex.ParamName);
    }
}
