// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.Parse(IEnumerable{string}, TextFilterOptions?)" /> — the gitignore line
/// conventions.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that bare lines become includes and <c>!</c>-prefixed lines become excludes.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLinesMixBareAndNegated_ShouldProduceIncludesAndExcludes()
    {
        var filter = TextFilter.Parse(["error*", "!*debug*"]);

        Assert.AreEqual(2, filter.Patterns.Count);
        Assert.AreEqual(TextFilterAction.Include, filter.Patterns[0].Action);
        Assert.AreEqual("error*", filter.Patterns[0].Pattern);
        Assert.AreEqual(TextFilterAction.Exclude, filter.Patterns[1].Action);
        Assert.AreEqual("*debug*", filter.Patterns[1].Pattern);
        Assert.IsTrue(filter.IsMatch("error1"));
        Assert.IsFalse(filter.IsMatch("error-debug"));
    }

    /// <summary>
    /// Verifies that blank lines and <c>#</c> comment lines are skipped, and surrounding white space is trimmed.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLinesHoldCommentsBlanksAndPadding_ShouldSkipAndTrim()
    {
        var filter = TextFilter.Parse(["# log selection", string.Empty, "   ", "  error*  "]);

        Assert.AreEqual(1, filter.Patterns.Count);
        Assert.AreEqual("error*", filter.Patterns[0].Pattern);
    }

    /// <summary>
    /// Verifies that <c>\!</c> and <c>\#</c> escape a literal leading character instead of negating or commenting.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLeadingCharacterIsEscaped_ShouldTreatItAsLiteralPattern()
    {
        var filter = TextFilter.Parse([@"\!important*", @"\#tag*"]);

        Assert.AreEqual(2, filter.Patterns.Count);
        Assert.AreEqual(TextFilterAction.Include, filter.Patterns[0].Action);
        Assert.IsTrue(filter.IsMatch("!important-note"));
        Assert.IsTrue(filter.IsMatch("#tag42"));
        Assert.IsFalse(filter.IsMatch("important-note"));
    }

    /// <summary>
    /// Verifies that trailing whitespace on a rule line does not become part of the pattern — the gitignore behavior
    /// (git strips unescaped trailing spaces), whose omission is a classic ignore-file bug.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLineHasTrailingSpaces_ShouldMatchAsIfTrimmed()
    {
        var filter = TextFilter.Parse(["error*   ", "!*debug*  "]);

        Assert.IsTrue(filter.IsMatch("error-1"));
        Assert.IsFalse(filter.IsMatch("error-debug"));
        Assert.AreEqual("error*", filter.Patterns[0].Pattern);
    }

    /// <summary>
    /// Verifies that every parsed pattern is a wildcard — regular expressions cannot be expressed in line form.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLinesParsed_ShouldProduceWildcardPatternsOnly()
    {
        var filter = TextFilter.Parse(["^looks-like-regex$"]);

        Assert.AreEqual(TextFilterPatternKind.Wildcard, filter.Patterns[0].Kind);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> line collection throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLinesIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = TextFilter.Parse(null!);
        });

        Assert.AreEqual("lines", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> line entry throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLinesContainsNull_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = TextFilter.Parse(["a", null!]);
        });

        Assert.AreEqual("lines", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a lone <c>!</c> line throws <see cref="ArgumentException" /> because its pattern text is empty.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLineIsLoneNegation_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = TextFilter.Parse(["!"]);
        });
    }
}
