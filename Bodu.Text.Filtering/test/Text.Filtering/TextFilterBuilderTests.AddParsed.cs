// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterBuilderTests.AddParsed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilterBuilder.AddParsed(string)" /> and
/// <see cref="TextFilterBuilder.AddParsed(IEnumerable{string})" />.
/// </content>
public partial class TextFilterBuilderTests
{
    /// <summary>
    /// Verifies that parsed lines follow the gitignore conventions and interleave with explicitly added patterns.
    /// </summary>
    [TestMethod]
    public void AddParsed_WhenMixedWithExplicitAdds_ShouldPreserveOverallOrder()
    {
        var filter = new TextFilterBuilder()
            .AddInclude("error*")
            .AddParsed("!*debug*")
            .AddParsed("# comment")
            .AddParsed(["warn*", string.Empty])
            .Build();

        Assert.AreEqual(3, filter.Patterns.Count);
        Assert.AreEqual("error*", filter.Patterns[0].Pattern);
        Assert.AreEqual(TextFilterAction.Exclude, filter.Patterns[1].Action);
        Assert.AreEqual("warn*", filter.Patterns[2].Pattern);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> line throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddParsed_WhenLineIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TextFilterBuilder().AddParsed((string)null!);
        });

        Assert.AreEqual("line", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a line collection containing <see langword="null" /> throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void AddParsed_WhenLinesContainsNull_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new TextFilterBuilder().AddParsed(["a", null!]);
        });

        Assert.AreEqual("lines", ex.ParamName);
    }
}
