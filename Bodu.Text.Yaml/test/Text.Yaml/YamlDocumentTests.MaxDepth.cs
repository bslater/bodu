// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that nesting beyond the configured maximum depth is rejected.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that nesting beyond the configured maximum depth is rejected.</summary>
    [TestMethod]
    public void Parse_WhenNestingExceedsMaxDepth_ShouldThrow()
    {
        var deep = string.Concat(Enumerable.Repeat("[", 200)) + "x" + string.Concat(Enumerable.Repeat("]", 200));
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("root: " + deep + "\n");
        });
    }
}
