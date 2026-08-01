// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPatternTests.Classify.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for the cost-tier classification performed by <see cref="WildcardPattern.Compile" />.
/// </content>
public partial class WildcardPatternTests
{
    /// <summary>
    /// Gets classification rows: pattern text mapped to the expected <see cref="WildcardMatchKind" /> member name
    /// (carried by name because the enumeration is internal and the data-driven method signature is public).
    /// </summary>
    public static IEnumerable<object[]> ClassifyKats
    {
        get
        {
            yield return new object[] { new ValidKat<string, string>("LoneStar", "*", nameof(WildcardMatchKind.MatchAll)) };
            yield return new object[] { new ValidKat<string, string>("CollapsedStars", "***", nameof(WildcardMatchKind.MatchAll)) };
            yield return new object[] { new ValidKat<string, string>("PlainLiteral", "abc", nameof(WildcardMatchKind.Literal)) };
            yield return new object[] { new ValidKat<string, string>("EscapedMetaLiteral", @"a\*b", nameof(WildcardMatchKind.Literal)) };
            yield return new object[] { new ValidKat<string, string>("TrailingStar", "abc*", nameof(WildcardMatchKind.Prefix)) };
            yield return new object[] { new ValidKat<string, string>("LeadingStar", "*abc", nameof(WildcardMatchKind.Suffix)) };
            yield return new object[] { new ValidKat<string, string>("InteriorStar", "abc*def", nameof(WildcardMatchKind.PrefixAndSuffix)) };
            yield return new object[] { new ValidKat<string, string>("SurroundingStars", "*abc*", nameof(WildcardMatchKind.Contains)) };
            yield return new object[] { new ValidKat<string, string>("CollapsedSurroundingStars", "**abc**", nameof(WildcardMatchKind.Contains)) };
            yield return new object[] { new ValidKat<string, string>("QuestionMark", "a?c", nameof(WildcardMatchKind.General)) };
            yield return new object[] { new ValidKat<string, string>("CharacterClass", "[ab]c", nameof(WildcardMatchKind.General)) };
            yield return new object[] { new ValidKat<string, string>("ThreeSegments", "a*b*c", nameof(WildcardMatchKind.General)) };
            yield return new object[] { new ValidKat<string, string>("StarThenQuestion", "*a?", nameof(WildcardMatchKind.General)) };
        }
    }

    /// <summary>
    /// Verifies that compilation classifies each pattern shape into the expected cost-tier strategy.
    /// </summary>
    /// <param name="kat">The classification row.</param>
    [TestMethod]
    [DynamicData(nameof(ClassifyKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compile_WhenPatternHasKnownShape_ShouldClassifyExpectedKind(ValidKat<string, string> kat)
    {
        var program = WildcardPattern.Compile(kat.Input);

        Assert.AreEqual(Enum.Parse<WildcardMatchKind>(kat.Expected), program.Kind);
    }

    /// <summary>
    /// Verifies that a prefix pattern's literal fragment is the unescaped leading text.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternIsPrefix_ShouldExtractPrefixFragment()
    {
        var program = WildcardPattern.Compile(@"ab\*c*");

        Assert.AreEqual("ab*c", program.Prefix);
    }

    /// <summary>
    /// Verifies that an interior-star pattern extracts both the prefix and suffix fragments.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternIsPrefixAndSuffix_ShouldExtractBothFragments()
    {
        var program = WildcardPattern.Compile("abc*def");

        Assert.AreEqual("abc", program.Prefix);
        Assert.AreEqual("def", program.Suffix);
    }

    /// <summary>
    /// Verifies that a contains pattern extracts its inner literal fragment.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternIsContains_ShouldExtractCoreFragment()
    {
        var program = WildcardPattern.Compile("*abc*");

        Assert.AreEqual("abc", program.Core);
    }

    /// <summary>
    /// Verifies that an empty pattern (arising from an empty brace alternative) compiles to an empty literal.
    /// </summary>
    [TestMethod]
    public void Compile_WhenPatternIsEmpty_ShouldClassifyEmptyLiteral()
    {
        var program = WildcardPattern.Compile(string.Empty);

        Assert.AreEqual(WildcardMatchKind.Literal, program.Kind);
        Assert.AreEqual(string.Empty, program.Literal);
    }

    /// <summary>
    /// Gets rows whose pattern text violates the wildcard grammar.
    /// </summary>
    public static IEnumerable<object[]> CompileInvalidKats
    {
        get
        {
            yield return new object[] { new InvalidKat<string>("UnterminatedClass", "[abc", typeof(ArgumentException), "pattern") };
            yield return new object[] { new InvalidKat<string>("EmptyClass", "[]", typeof(ArgumentException), "pattern") };
            yield return new object[] { new InvalidKat<string>("EmptyNegatedClass", "[!]", typeof(ArgumentException), "pattern") };
            yield return new object[] { new InvalidKat<string>("DanglingEscape", @"abc\", typeof(ArgumentException), "pattern") };
            yield return new object[] { new InvalidKat<string>("EscapeAtClassEnd", @"[a\", typeof(ArgumentException), "pattern") };
        }
    }

    /// <summary>
    /// Verifies that a malformed pattern throws <see cref="ArgumentException" /> with the expected parameter name.
    /// </summary>
    /// <param name="kat">The invalid-pattern row.</param>
    [TestMethod]
    [DynamicData(nameof(CompileInvalidKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compile_WhenPatternIsMalformed_ShouldThrowArgumentException(InvalidKat<string> kat)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = WildcardPattern.Compile(kat.Input);
        });

        Assert.AreEqual(kat.ParamName, ex.ParamName);
    }
}
