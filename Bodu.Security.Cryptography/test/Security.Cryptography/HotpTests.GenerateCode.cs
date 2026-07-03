// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HotpTests.GenerateCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HotpTests
{
    /// <summary>
    /// Verifies that <see cref="Hotp.GenerateCode(ReadOnlySpan{byte}, long, int, OtpHashAlgorithm)" /> reproduces every
    /// six-digit code from the RFC 4226 Appendix D test table.
    /// </summary>
    /// <param name="testName">The RFC 4226 test-case label.</param>
    /// <param name="counter">The moving counter value.</param>
    /// <param name="expected">The expected six-digit code.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("RFC 4226 Appendix D, count=0", 0L, "755224")]
    [DataRow("RFC 4226 Appendix D, count=1", 1L, "287082")]
    [DataRow("RFC 4226 Appendix D, count=2", 2L, "359152")]
    [DataRow("RFC 4226 Appendix D, count=3", 3L, "969429")]
    [DataRow("RFC 4226 Appendix D, count=4", 4L, "338314")]
    [DataRow("RFC 4226 Appendix D, count=5", 5L, "254676")]
    [DataRow("RFC 4226 Appendix D, count=6", 6L, "287922")]
    [DataRow("RFC 4226 Appendix D, count=7", 7L, "162583")]
    [DataRow("RFC 4226 Appendix D, count=8", 8L, "399871")]
    [DataRow("RFC 4226 Appendix D, count=9", 9L, "520489")]
    public void GenerateCode_WhenGivenRfc4226Vector_ShouldReproducePublishedCode(string testName, long counter, string expected)
    {
        _ = testName;

        string code = Hotp.GenerateCode(Rfc4226Secret, counter);

        Assert.AreEqual(expected, code);
    }

    /// <summary>
    /// Verifies that a generated code always has exactly the requested number of digits, including significant leading
    /// zeros.
    /// </summary>
    /// <param name="digits">The requested digit count.</param>
    [TestMethod]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    public void GenerateCode_WhenDigitsSpecified_ShouldReturnThatManyCharacters(int digits)
    {
        string code = Hotp.GenerateCode(Rfc4226Secret, counter: 1, digits: digits);

        Assert.AreEqual(digits, code.Length);
        Assert.IsTrue(code.All(char.IsDigit));
    }

    /// <summary>
    /// Verifies that a digit count outside the permitted 6 to 8 range throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>digits</c> parameter.
    /// </summary>
    /// <param name="digits">The invalid digit count.</param>
    [TestMethod]
    [DataRow(5)]
    [DataRow(9)]
    public void GenerateCode_WhenDigitsOutOfRange_ShouldThrowArgumentOutOfRangeException(int digits)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Hotp.GenerateCode(Rfc4226Secret, counter: 0, digits: digits);
        });

        Assert.AreEqual("digits", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="OtpHashAlgorithm" /> value throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>algorithm</c> parameter.
    /// </summary>
    [TestMethod]
    public void GenerateCode_WhenAlgorithmUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Hotp.GenerateCode(Rfc4226Secret, counter: 0, algorithm: (OtpHashAlgorithm)99);
        });

        Assert.AreEqual("algorithm", ex.ParamName);
    }
}
