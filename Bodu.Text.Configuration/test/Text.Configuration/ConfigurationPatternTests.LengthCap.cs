// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.LengthCap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that a pattern at the configured maximum length compiles successfully, pinning the inclusive
    /// upper bound of <see cref="ConfigurationPattern.MaxPatternLength" /> so the cap cannot be reduced
    /// without a deliberate test update.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternLengthAtCap_ShouldCompile()
    {
        var pattern = new string('a', ConfigurationPattern.MaxPatternLength);

        ConfigurationPattern compiled = ConfigurationPattern.Compile(pattern);

        Assert.IsTrue(compiled.IsMatch(pattern));
        Assert.IsFalse(compiled.IsMatch(pattern + "b"));
    }

    /// <summary>
    /// Verifies that a pattern exceeding the configured maximum length throws
    /// <see cref="ConfigurationParseException" /> with
    /// <see cref="ConfigurationDiagnosticCode.PatternTooLong" /> rather than silently allocating a
    /// multi-megabyte regex and compiling it.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternLengthExceedsCap_ShouldThrowWithPatternTooLongCode()
    {
        var pattern = new string('a', ConfigurationPattern.MaxPatternLength + 1);

        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile(pattern);
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(ConfigurationDiagnosticCode.PatternTooLong, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that the length check fires before the cache is consulted — repeatedly compiling an
    /// over-cap pattern continues to throw rather than serving a cached failure or success entry. This pins
    /// the order of validation relative to <c>CompileCache</c>.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternExceedsCapAndCompiledTwice_ShouldThrowEachTime()
    {
        var pattern = new string('a', ConfigurationPattern.MaxPatternLength + 1);

        Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile(pattern);
        });

        Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile(pattern);
        });
    }
}
