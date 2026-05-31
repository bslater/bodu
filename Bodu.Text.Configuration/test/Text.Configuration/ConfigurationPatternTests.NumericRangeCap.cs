// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.NumericRangeCap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that a numeric range whose expansion exceeds the configured maximum throws a
    /// <see cref="ConfigurationParseException" /> with
    /// <see cref="ConfigurationDiagnosticCode.NumericRangeTooLarge" /> instead of silently building a giant
    /// regex alternation.
    /// </summary>
    [TestMethod]
    public void Compile_WhenNumericRangeExceedsCap_ShouldThrowWithNumericRangeTooLargeCode()
    {
        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile("file-{1..100000}.txt");
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(ConfigurationDiagnosticCode.NumericRangeTooLarge, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that a reversed range still throws when its absolute size exceeds the cap.
    /// </summary>
    [TestMethod]
    public void Compile_WhenReversedNumericRangeExceedsCap_ShouldThrow()
    {
        Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile("file-{100000..1}.txt");
        });
    }

    /// <summary>
    /// Verifies that a modest numeric range continues to compile and match every integer in the range.
    /// </summary>
    [TestMethod]
    public void Compile_WhenNumericRangeIsWithinCap_ShouldCompileAndMatch()
    {
        ConfigurationPattern pattern = ConfigurationPattern.Compile("file-{1..50}.txt");

        for (var i = 1; i <= 50; i++)
            Assert.IsTrue(pattern.IsMatch($"file-{i}.txt"), $"expected match for {i}");

        Assert.IsFalse(pattern.IsMatch("file-0.txt"));
        Assert.IsFalse(pattern.IsMatch("file-51.txt"));
    }

    /// <summary>
    /// Verifies that a range starting and ending at the same value still compiles and matches that single
    /// value (degenerate but legal).
    /// </summary>
    [TestMethod]
    public void Compile_WhenNumericRangeIsSingleValue_ShouldMatchOnlyThatValue()
    {
        ConfigurationPattern pattern = ConfigurationPattern.Compile("file-{5..5}.txt");

        Assert.IsTrue(pattern.IsMatch("file-5.txt"));
        Assert.IsFalse(pattern.IsMatch("file-4.txt"));
        Assert.IsFalse(pattern.IsMatch("file-6.txt"));
    }

    /// <summary>
    /// Verifies that a negative range is handled identically to its positive equivalent in terms of cap
    /// enforcement. The cap is based on the count of integers in the range, not the magnitude of either
    /// endpoint.
    /// </summary>
    [TestMethod]
    public void Compile_WhenNegativeRangeIsModest_ShouldCompileAndMatch()
    {
        ConfigurationPattern pattern = ConfigurationPattern.Compile("offset-{-3..3}.log");

        for (var i = -3; i <= 3; i++)
            Assert.IsTrue(pattern.IsMatch($"offset-{i}.log"), $"expected match for {i}");

        Assert.IsFalse(pattern.IsMatch("offset-4.log"));
    }
}
