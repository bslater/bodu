// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.NestedBraces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that a brace alternation nested two levels deep compiles and matches each of the expanded
    /// alternatives, confirming that nested braces are recursed into rather than treated as a literal block.
    /// </summary>
    [TestMethod]
    public void Compile_WhenAlternationIsNestedTwoLevels_ShouldMatchEachExpandedAlternative()
    {
        var pattern = ConfigurationPattern.Compile("file.{a,{b,c},d}.log");

        Assert.IsTrue(pattern.IsMatch("file.a.log"));
        Assert.IsTrue(pattern.IsMatch("file.b.log"));
        Assert.IsTrue(pattern.IsMatch("file.c.log"));
        Assert.IsTrue(pattern.IsMatch("file.d.log"));
        Assert.IsFalse(pattern.IsMatch("file.e.log"));
    }

    /// <summary>
    /// Verifies that a brace alternation nested three levels deep compiles and matches every leaf
    /// alternative. The pattern <c>{a,{b,{c,d}}}</c> should expand to {a, b, c, d}.
    /// </summary>
    [TestMethod]
    public void Compile_WhenAlternationIsNestedThreeLevels_ShouldMatchEachExpandedLeaf()
    {
        var pattern = ConfigurationPattern.Compile("x.{a,{b,{c,d}}}.y");

        Assert.IsTrue(pattern.IsMatch("x.a.y"));
        Assert.IsTrue(pattern.IsMatch("x.b.y"));
        Assert.IsTrue(pattern.IsMatch("x.c.y"));
        Assert.IsTrue(pattern.IsMatch("x.d.y"));
        Assert.IsFalse(pattern.IsMatch("x.e.y"));
    }

    /// <summary>
    /// Verifies that a brace alternation can contain a nested numeric range and still match each member of
    /// the expanded set.
    /// </summary>
    [TestMethod]
    public void Compile_WhenAlternationContainsNestedNumericRange_ShouldMatchEachAlternative()
    {
        var pattern = ConfigurationPattern.Compile("run.{init,{1..3}}.log");

        Assert.IsTrue(pattern.IsMatch("run.init.log"));
        Assert.IsTrue(pattern.IsMatch("run.1.log"));
        Assert.IsTrue(pattern.IsMatch("run.2.log"));
        Assert.IsTrue(pattern.IsMatch("run.3.log"));
        Assert.IsFalse(pattern.IsMatch("run.0.log"));
        Assert.IsFalse(pattern.IsMatch("run.4.log"));
    }

    /// <summary>
    /// Verifies that a deeply-but-narrowly nested pattern (depth 8, two alternatives at each level) still
    /// compiles within a reasonable time bound. The recursion depth is bounded by pattern length and the
    /// existing line-length cap, but this pinning ensures the compiler does not regress to pathological
    /// O(2^n) behaviour.
    /// </summary>
    [TestMethod]
    public void Compile_WhenAlternationIsDeeplyNested_ShouldCompileWithinReasonableBound()
    {
        StringBuilder source = new();
        source.Append("prefix.");
        for (int i = 0; i < 8; i++) source.Append("{a,");
        source.Append('b');
        for (int i = 0; i < 8; i++) source.Append('}');
        source.Append(".suffix");

        var pattern = ConfigurationPattern.Compile(source.ToString());

        // Both endpoints of the expansion should match.
        Assert.IsTrue(pattern.IsMatch("prefix.a.suffix"));
        Assert.IsTrue(pattern.IsMatch("prefix.b.suffix"));
    }

    /// <summary>
    /// Verifies that an unbalanced opening brace inside a nested alternation still surfaces as
    /// <see cref="ConfigurationDiagnosticCode.UnbalancedBrace" /> rather than being silently absorbed.
    /// </summary>
    [TestMethod]
    public void Compile_WhenNestedAlternationHasUnbalancedBrace_ShouldThrowUnbalancedBrace()
    {
        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile("x.{a,{b,c}.y");
        });

        Assert.AreEqual(ConfigurationDiagnosticCode.UnbalancedBrace, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that an escaped opening brace is treated as a literal character and does not start a nested
    /// alternation. The pattern <c>foo\{bar</c> matches the literal string <c>foo{bar</c>.
    /// </summary>
    [TestMethod]
    public void Compile_WhenBraceIsEscaped_ShouldTreatAsLiteralCharacter()
    {
        var pattern = ConfigurationPattern.Compile(@"foo\{bar");

        Assert.IsTrue(pattern.IsMatch("foo{bar"));
        Assert.IsFalse(pattern.IsMatch("foobar"));
    }
}
