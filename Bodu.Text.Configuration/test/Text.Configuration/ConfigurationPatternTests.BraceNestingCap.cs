// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.BraceNestingCap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that a brace alternation nested up to <see cref="ConfigurationPattern.MaxBraceNestingDepth" />
    /// compiles and matches each expanded leaf. This pins the inclusive upper bound and guards against the cap
    /// being inadvertently reduced.
    /// </summary>
    [TestMethod]
    public void Compile_WhenBraceNestingAtCap_ShouldCompile()
    {
        ConfigurationPattern pattern = ConfigurationPattern.Compile(BuildNestedAlternation(ConfigurationPattern.MaxBraceNestingDepth));

        // The nested pattern produced by BuildNestedAlternation matches both 'a' and 'b' at every level,
        // so its outer-most match set is {a, b}.
        Assert.IsTrue(pattern.IsMatch("prefix.a.suffix"));
        Assert.IsTrue(pattern.IsMatch("prefix.b.suffix"));
    }

    /// <summary>
    /// Verifies that a brace alternation nested one level deeper than the cap throws a
    /// <see cref="ConfigurationParseException" /> with
    /// <see cref="ConfigurationDiagnosticCode.BraceNestingTooDeep" />, rather than silently recursing into a
    /// pathological pattern that risks stack exhaustion or unbounded regex expansion.
    /// </summary>
    [TestMethod]
    public void Compile_WhenBraceNestingExceedsCap_ShouldThrowWithBraceNestingTooDeepCode()
    {
        var source = BuildNestedAlternation(ConfigurationPattern.MaxBraceNestingDepth + 1);

        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationPattern.Compile(source);
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(ConfigurationDiagnosticCode.BraceNestingTooDeep, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that the cap counts only unescaped opening braces — a pattern containing many escaped
    /// <c>\{</c> literals does not exhaust the nesting budget.
    /// </summary>
    [TestMethod]
    public void Compile_WhenManyEscapedBracesExist_ShouldNotCountTowardsNestingCap()
    {
        StringBuilder source = new();
        source.Append("prefix");
        for (var i = 0; i < ConfigurationPattern.MaxBraceNestingDepth + 16; i++)
            source.Append(@"\{");

        source.Append(".log");

        // Should not throw — every '{' is escaped, so depth never exceeds zero.
        ConfigurationPattern pattern = ConfigurationPattern.Compile(source.ToString());

        StringBuilder expectedMatch = new();
        expectedMatch.Append("prefix");
        for (var i = 0; i < ConfigurationPattern.MaxBraceNestingDepth + 16; i++)
            expectedMatch.Append('{');

        expectedMatch.Append(".log");

        Assert.IsTrue(pattern.IsMatch(expectedMatch.ToString()));
    }

    /// <summary>
    /// Builds <c>prefix.{a,{a,{a,…,b}…}}.suffix</c> — an <paramref name="depth" />-deep nested alternation
    /// where every layer offers <c>a</c> alongside an inner brace, terminating in <c>b</c>. The compiled
    /// pattern matches both <c>prefix.a.suffix</c> and <c>prefix.b.suffix</c>.
    /// </summary>
    /// <param name="depth">The number of nested <c>{…}</c> levels to generate.</param>
    /// <returns>The constructed glob pattern.</returns>
    private static string BuildNestedAlternation(int depth)
    {
        StringBuilder source = new();
        source.Append("prefix.");
        for (var i = 0; i < depth; i++)
            source.Append("{a,");

        source.Append('b');
        for (var i = 0; i < depth; i++)
            source.Append('}');

        source.Append(".suffix");
        return source.ToString();
    }
}
