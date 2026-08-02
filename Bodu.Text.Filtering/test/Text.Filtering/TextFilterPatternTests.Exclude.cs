// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPatternTests.Exclude.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for the <see cref="TextFilterPattern.Exclude" /> factory.
/// </content>
public partial class TextFilterPatternTests
{
    /// <summary>
    /// Verifies that the factory produces an exclude pattern carrying the supplied kind and case override.
    /// </summary>
    [TestMethod]
    public void Exclude_WhenCalled_ShouldProduceExcludePattern()
    {
        var pattern = TextFilterPattern.Exclude("*.log", ignoreCase: false);

        Assert.AreEqual(TextFilterAction.Exclude, pattern.Action);
        Assert.AreEqual("*.log", pattern.Pattern);
        Assert.AreEqual(TextFilterPatternKind.Wildcard, pattern.Kind);
        Assert.AreEqual(false, pattern.IgnoreCase);
    }

    /// <summary>
    /// Verifies that the factory applies the same argument validation as the constructor.
    /// </summary>
    [TestMethod]
    public void Exclude_WhenPatternIsEmpty_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = TextFilterPattern.Exclude(string.Empty);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }
}
