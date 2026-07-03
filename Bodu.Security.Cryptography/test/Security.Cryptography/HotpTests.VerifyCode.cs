// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HotpTests.VerifyCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HotpTests
{
    /// <summary>
    /// Verifies that <see cref="Hotp.VerifyCode(ReadOnlySpan{byte}, ReadOnlySpan{char}, long, int, OtpHashAlgorithm)" />
    /// accepts the correct RFC 4226 code for its counter.
    /// </summary>
    /// <param name="counter">The moving counter value.</param>
    /// <param name="code">The expected code for that counter.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0L, "755224")]
    [DataRow(5L, "254676")]
    [DataRow(9L, "520489")]
    public void VerifyCode_WhenCodeMatchesCounter_ShouldReturnTrue(long counter, string code)
    {
        Assert.IsTrue(Hotp.VerifyCode(Rfc4226Secret, code, counter));
    }

    /// <summary>
    /// Verifies that a code that does not correspond to the supplied counter is rejected.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCodeDoesNotMatchCounter_ShouldReturnFalse()
    {
        // "755224" is the code for counter 0, not counter 1.
        Assert.IsFalse(Hotp.VerifyCode(Rfc4226Secret, "755224", counter: 1));
    }

    /// <summary>
    /// Verifies that a candidate whose length differs from the expected digit count is rejected rather than throwing.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCandidateLengthDiffers_ShouldReturnFalse()
    {
        Assert.IsFalse(Hotp.VerifyCode(Rfc4226Secret, "7552", counter: 0));
        Assert.IsFalse(Hotp.VerifyCode(Rfc4226Secret, "75522400", counter: 0));
    }

    /// <summary>
    /// Verifies that the look-ahead overload accepts a code from a later counter within the window and reports the
    /// matched counter.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCodeWithinLookAheadWindow_ShouldReturnTrueAndReportMatchedCounter()
    {
        // Code for counter 3, verified starting from counter 0 with a look-ahead of 5.
        bool result = Hotp.VerifyCode(Rfc4226Secret, "969429", counter: 0, lookAhead: 5, out long matchedCounter);

        Assert.IsTrue(result);
        Assert.AreEqual(3L, matchedCounter);
    }

    /// <summary>
    /// Verifies that a code beyond the look-ahead window is rejected and the matched counter is reported as <c>-1</c>.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenCodeBeyondLookAheadWindow_ShouldReturnFalseAndReportNegativeOne()
    {
        // Code for counter 9, but the window only reaches counter 0..2.
        bool result = Hotp.VerifyCode(Rfc4226Secret, "520489", counter: 0, lookAhead: 2, out long matchedCounter);

        Assert.IsFalse(result);
        Assert.AreEqual(-1L, matchedCounter);
    }

    /// <summary>
    /// Verifies that a negative look-ahead throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// <c>lookAhead</c> parameter.
    /// </summary>
    [TestMethod]
    public void VerifyCode_WhenLookAheadNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Hotp.VerifyCode(Rfc4226Secret, "755224", counter: 0, lookAhead: -1, out _);
        });

        Assert.AreEqual("lookAhead", ex.ParamName);
    }
}
