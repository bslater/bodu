// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPatternTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for the <see cref="TextFilterPattern" /> constructor.
/// </content>
public partial class TextFilterPatternTests
{
    /// <summary>
    /// Verifies that the constructor stores the pattern text, action, kind, and case override unchanged.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArgumentsAreValid_ShouldStoreAllProperties()
    {
        var pattern = new TextFilterPattern("abc*", TextFilterAction.Exclude, TextFilterPatternKind.Regex, ignoreCase: false);

        Assert.AreEqual("abc*", pattern.Pattern);
        Assert.AreEqual(TextFilterAction.Exclude, pattern.Action);
        Assert.AreEqual(TextFilterPatternKind.Regex, pattern.Kind);
        Assert.AreEqual(false, pattern.IgnoreCase);
    }

    /// <summary>
    /// Verifies that the kind defaults to <see cref="TextFilterPatternKind.Wildcard" /> and the case override defaults
    /// to inherit (<see langword="null" />).
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionalArgumentsOmitted_ShouldDefaultToWildcardAndInheritedCase()
    {
        var pattern = new TextFilterPattern("abc", TextFilterAction.Include);

        Assert.AreEqual(TextFilterPatternKind.Wildcard, pattern.Kind);
        Assert.IsNull(pattern.IgnoreCase);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pattern throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TextFilterPattern(null!, TextFilterAction.Include);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty or whitespace-only pattern throws <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="pattern">The rejected pattern text.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\t")]
    public void Ctor_WhenPatternIsEmptyOrWhiteSpace_ShouldThrowArgumentException(string pattern)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new TextFilterPattern(pattern, TextFilterAction.Include);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a pattern longer than the supported maximum throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternExceedsMaxLength_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new TextFilterPattern(new string('a', TextFilterPattern.MaxPatternLength + 1), TextFilterAction.Include);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined action value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenActionIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new TextFilterPattern("abc", (TextFilterAction)99);
        });

        Assert.AreEqual("action", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined kind value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKindIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new TextFilterPattern("abc", TextFilterAction.Include, (TextFilterPatternKind)99);
        });

        Assert.AreEqual("kind", ex.ParamName);
    }
}
