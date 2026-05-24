// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.WrapAndQuote.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.Quote(string)" /> wraps the input in double-quote characters.
    /// </summary>
    [TestMethod]
    public void Quote_WhenInvoked_ShouldWrapInDoubleQuotes()
    {
        Assert.AreEqual("\"hello\"", "hello".Quote());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Quote(string)" /> throws <see cref="ArgumentNullException" />
    /// when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Quote_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.Quote(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SingleQuote(string)" /> wraps the input in single-quote
    /// characters.
    /// </summary>
    [TestMethod]
    public void SingleQuote_WhenInvoked_ShouldWrapInSingleQuotes()
    {
        Assert.AreEqual("'hello'", "hello".SingleQuote());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SingleQuote(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SingleQuote_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.SingleQuote(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Wrap(string, string, string)" /> concatenates prefix, value,
    /// and suffix.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenInvoked_ShouldConcatenatePrefixValueSuffix()
    {
        Assert.AreEqual("<b>hello</b>", "hello".Wrap("<b>", "</b>"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Wrap(string, string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when any argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Wrap_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Wrap(null!, "<", ">"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Wrap(null!, ">"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Wrap("<", null!));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Unwrap(string, string, string, StringComparison)" /> removes
    /// the prefix and suffix when both are present.
    /// </summary>
    [TestMethod]
    public void Unwrap_WhenBothEndsMatch_ShouldRemoveBoth()
    {
        Assert.AreEqual("hello", "<b>hello</b>".Unwrap("<b>", "</b>"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Unwrap(string, string, string, StringComparison)" /> returns
    /// the input unchanged when only the prefix matches.
    /// </summary>
    [TestMethod]
    public void Unwrap_WhenOnlyPrefixMatches_ShouldReturnInputUnchanged()
    {
        Assert.AreEqual("<b>hello", "<b>hello".Unwrap("<b>", "</b>"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Unwrap(string, string, string, StringComparison)" /> returns
    /// the input unchanged when only the suffix matches.
    /// </summary>
    [TestMethod]
    public void Unwrap_WhenOnlySuffixMatches_ShouldReturnInputUnchanged()
    {
        Assert.AreEqual("hello</b>", "hello</b>".Unwrap("<b>", "</b>"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Unwrap(string, string, string, StringComparison)" /> honours
    /// the supplied <see cref="StringComparison" />.
    /// </summary>
    [TestMethod]
    public void Unwrap_WhenIgnoreCaseSupplied_ShouldMatchCaseInsensitively()
    {
        Assert.AreEqual(
            "hello",
            "<B>hello</B>".Unwrap("<b>", "</b>", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Unwrap(string, string, string, StringComparison)" /> is the
    /// inverse of <see cref="StringExtensions.Wrap(string, string, string)" /> for matching affixes.
    /// </summary>
    [TestMethod]
    public void Unwrap_AfterWrap_ShouldRoundTrip()
    {
        const string original = "hello";

        var actual = original.Wrap("<b>", "</b>").Unwrap("<b>", "</b>");

        Assert.AreEqual(original, actual);
    }

    /// <summary>
    /// Verifies that the bracket-style shortcuts wrap with the expected character pair.
    /// </summary>
    [TestMethod]
    public void BracketStyleShortcuts_WhenInvoked_ShouldWrapInExpectedCharacterPair()
    {
        Assert.AreEqual("(hello)", "hello".Parenthesize());
        Assert.AreEqual("[hello]", "hello".Bracket());
        Assert.AreEqual("{hello}", "hello".Brace());
    }

    /// <summary>
    /// Verifies that the bracket-style shortcuts throw <see cref="ArgumentNullException" /> on a null
    /// receiver.
    /// </summary>
    [TestMethod]
    public void BracketStyleShortcuts_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Parenthesize(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Bracket(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Brace(null!));
    }
}
