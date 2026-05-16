// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationPatternTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the EditorConfig-style glob matcher.
/// </summary>
[TestClass]
public partial class BoduConfigurationPatternTests
{
    /// <summary>
    /// Verifies that <c>*</c> matches characters within a single path segment but stops at a forward slash.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenSingleStar_ShouldNotCrossSlash()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("*.cs");

        Assert.IsTrue(pattern.IsMatch("Foo.cs"));
        Assert.IsTrue(pattern.IsMatch("src/Bar.cs"));
        Assert.IsFalse(pattern.IsMatch("Foo.txt"));
    }

    /// <summary>
    /// Verifies that <c>**</c> crosses forward slashes.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenDoubleStar_ShouldCrossSlash()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("src/**/*.cs");

        Assert.IsTrue(pattern.IsMatch("src/a/Foo.cs"));
        Assert.IsTrue(pattern.IsMatch("src/a/b/c/Foo.cs"));
        Assert.IsFalse(pattern.IsMatch("other/Foo.cs"));
    }

    /// <summary>
    /// Verifies that <c>?</c> matches exactly one character within a path segment.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenQuestion_ShouldMatchSingleCharacter()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("?.cs");

        Assert.IsTrue(pattern.IsMatch("a.cs"));
        Assert.IsFalse(pattern.IsMatch("ab.cs"));
    }

    /// <summary>
    /// Verifies that brace alternation matches any of the listed alternatives.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenBraceAlternation_ShouldMatchAnyAlternative()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("*.{cs,csproj}");

        Assert.IsTrue(pattern.IsMatch("Foo.cs"));
        Assert.IsTrue(pattern.IsMatch("Foo.csproj"));
        Assert.IsFalse(pattern.IsMatch("Foo.vb"));
    }

    /// <summary>
    /// Verifies that a character class matches a single character in the set.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenCharacterClass_ShouldMatchSetMember()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("Foo.[ch]");

        Assert.IsTrue(pattern.IsMatch("Foo.c"));
        Assert.IsTrue(pattern.IsMatch("Foo.h"));
        Assert.IsFalse(pattern.IsMatch("Foo.x"));
    }

    /// <summary>
    /// Verifies that a negated character class rejects the listed characters.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenNegatedCharacterClass_ShouldRejectSetMembers()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("Foo.[!ch]");

        Assert.IsFalse(pattern.IsMatch("Foo.c"));
        Assert.IsFalse(pattern.IsMatch("Foo.h"));
        Assert.IsTrue(pattern.IsMatch("Foo.x"));
    }

    /// <summary>
    /// Verifies that a numeric range expands to alternatives.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenNumericRange_ShouldMatchEachIntegerInRange()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile("file-{1..3}.txt");

        Assert.IsTrue(pattern.IsMatch("file-1.txt"));
        Assert.IsTrue(pattern.IsMatch("file-2.txt"));
        Assert.IsTrue(pattern.IsMatch("file-3.txt"));
        Assert.IsFalse(pattern.IsMatch("file-4.txt"));
    }

    /// <summary>
    /// Verifies that the backslash escape causes the next character to be matched literally.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenBackslashEscape_ShouldMatchLiteralSpecialCharacter()
    {
        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile(@"\*.cs");

        Assert.IsTrue(pattern.IsMatch("*.cs"));
        Assert.IsFalse(pattern.IsMatch("Foo.cs"));
    }

    /// <summary>
    /// Verifies that an unbalanced brace throws a configuration parse exception.
    /// </summary>
    [TestMethod]
    public void Compile_WhenUnbalancedBrace_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<BoduConfigurationParseException>(() =>
        {
            _ = BoduConfigurationPattern.Compile("{a,b");
        });
    }

    /// <summary>
    /// Verifies that an unbalanced bracket throws a configuration parse exception.
    /// </summary>
    [TestMethod]
    public void Compile_WhenUnbalancedBracket_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<BoduConfigurationParseException>(() =>
        {
            _ = BoduConfigurationPattern.Compile("[abc");
        });
    }
}
