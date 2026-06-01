// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.NegativeVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvTests
{

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// when a key starts with a digit.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyStartsWithDigit_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("1INVALID=value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// when a key contains a hyphen.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyContainsHyphen_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("MY-KEY=value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// and reports the correct line number for an invalid key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyIsInvalidOnSecondLine_ShouldReportLineNumberTwo()
    {
        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("VALID=ok\n1INVALID=value");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// when a double-quoted string is not closed before end of input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedStringIsUnterminated_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("KEY=\"unclosed");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// and reports the correct line number for an unterminated double-quoted string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDoubleQuotedStringIsUnterminatedOnThirdLine_ShouldReportLineNumberThree()
    {
        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("A=1\nB=2\nKEY=\"unclosed");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// when a single-quoted string is not closed before the end of the line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSingleQuotedStringIsUnterminated_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("KEY='unclosed\nNEXT=value");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> throws <see cref="DotEnvFormatException" />
    /// when duplicate keys are disallowed and the same key appears twice.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyDisallowedAndKeyRepeated_ShouldThrowExactly()
    {
        DotEnvParseOptions options = new() { DuplicateKeyBehavior = DotEnvDuplicateKeyBehavior.Disallowed };

        DotEnvFormatException ex = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            _ = DotEnv.Parse("KEY=first\nKEY=second", options);
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

}
