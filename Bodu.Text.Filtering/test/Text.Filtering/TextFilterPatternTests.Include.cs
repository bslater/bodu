// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPatternTests.Include.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for the <see cref="TextFilterPattern.Include" /> factory.
/// </content>
public partial class TextFilterPatternTests
{
    /// <summary>
    /// Verifies that the factory produces an include pattern carrying the supplied kind and case override.
    /// </summary>
    [TestMethod]
    public void Include_WhenCalled_ShouldProduceIncludePattern()
    {
        var pattern = TextFilterPattern.Include("abc", TextFilterPatternKind.Regex, ignoreCase: true);

        Assert.AreEqual(TextFilterAction.Include, pattern.Action);
        Assert.AreEqual("abc", pattern.Pattern);
        Assert.AreEqual(TextFilterPatternKind.Regex, pattern.Kind);
        Assert.AreEqual(true, pattern.IgnoreCase);
    }

    /// <summary>
    /// Verifies that the factory applies the same argument validation as the constructor.
    /// </summary>
    [TestMethod]
    public void Include_WhenPatternIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = TextFilterPattern.Include(null!);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }
}
