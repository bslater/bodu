// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies that each valid duration row parses to its expected interval.
    /// </summary>
    /// <param name="kat">The valid duration row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ParseValidKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenValid_ShouldReturnExpectedInterval(ValidKat<string, TimeSpan> kat)
    {
        var interval = AnchoredInterval.Parse(kat.Input);

        Assert.AreEqual(kat.Expected, interval.Interval);
    }

    /// <summary>
    /// Verifies that each malformed duration row throws <see cref="FormatException" /> with a message naming the
    /// specific defect.
    /// </summary>
    /// <param name="kat">The malformed duration row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ParseInvalidKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalid_ShouldThrowFormatExceptionNamingDefect(InvalidKat<string> kat)
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = AnchoredInterval.Parse(kat.Input);
        });

        Assert.IsNotNull(kat.MessageContains);
        Assert.IsTrue(
            ex.Message.Contains(kat.MessageContains, StringComparison.Ordinal),
            $"Expected message to contain '{kat.MessageContains}' but was '{ex.Message}'.");
    }

    /// <summary>
    /// Verifies that parsing <see langword="null" /> throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = AnchoredInterval.Parse(null!);
        });

        Assert.AreEqual("s", ex.ParamName);
    }

    /// <summary>
    /// Verifies that parsing from a character span produces the same interval as parsing the string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSpan_ShouldMatchStringOverload()
    {
        var fromSpan = AnchoredInterval.Parse("P1DT2H30M".AsSpan(), null);

        Assert.AreEqual(AnchoredInterval.Parse("P1DT2H30M"), fromSpan);
    }

    /// <summary>
    /// Verifies that each valid duration row parses through <c>TryParse</c>, returning <see langword="true" /> and the
    /// expected interval.
    /// </summary>
    /// <param name="kat">The valid duration row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ParseValidKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenValid_ShouldReturnTrueAndExpectedInterval(ValidKat<string, TimeSpan> kat)
    {
        bool parsed = AnchoredInterval.TryParse(kat.Input, out AnchoredInterval? result);

        Assert.IsTrue(parsed);
        Assert.AreEqual(kat.Expected, result.Interval);
    }

    /// <summary>
    /// Verifies that each malformed duration row fails <c>TryParse</c> with a <see langword="null" /> result.
    /// </summary>
    /// <param name="kat">The malformed duration row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ParseInvalidKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInvalid_ShouldReturnFalse(InvalidKat<string> kat)
    {
        bool parsed = AnchoredInterval.TryParse(kat.Input, out AnchoredInterval? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload names the specific defect on failure.
    /// </summary>
    /// <param name="kat">The malformed duration row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ParseInvalidKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInvalid_ShouldReportDefectMessage(InvalidKat<string> kat)
    {
        bool parsed = AnchoredInterval.TryParse(kat.Input, out AnchoredInterval? result, out string? failureMessage);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
        Assert.IsNotNull(kat.MessageContains);
        Assert.IsNotNull(failureMessage);
        Assert.IsTrue(
            failureMessage.Contains(kat.MessageContains, StringComparison.Ordinal),
            $"Expected failure message to contain '{kat.MessageContains}' but was '{failureMessage}'.");
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload reports no message on success.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenValid_ShouldReportNullFailureMessage()
    {
        bool parsed = AnchoredInterval.TryParse("PT4H", out AnchoredInterval? result, out string? failureMessage);

        Assert.IsTrue(parsed);
        Assert.AreEqual(TimeSpan.FromHours(4), result!.Interval);
        Assert.IsNull(failureMessage);
    }

    /// <summary>
    /// Verifies that the defect-reporting <c>TryParse</c> overload treats <see langword="null" /> input as an empty
    /// duration defect rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenNull_ShouldReturnFalseAndReportEmptyDefect()
    {
        bool parsed = AnchoredInterval.TryParse(null, out AnchoredInterval? result, out string? failureMessage);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
        Assert.IsNotNull(failureMessage);
        Assert.IsTrue(failureMessage.Contains("empty", StringComparison.Ordinal));
    }
}
