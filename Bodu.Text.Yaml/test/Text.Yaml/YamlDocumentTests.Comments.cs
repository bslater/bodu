// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Comments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies handling of comments, blank lines, explicit complex keys, and empty mapping values.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that comments and blank lines are ignored.</summary>
    [TestMethod]
    public void Parse_WhenCommentsAndBlankLines_ShouldIgnore()
    {
        using var doc = YamlDocument.Parse("# header\n\na: 1   # inline\n\n# trailing\nb: 2\n");
        Assert.AreEqual(1L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that an empty mapping value resolves to null.</summary>
    [TestMethod]
    public void Parse_WhenEmptyMappingValue_ShouldBeNull()
    {
        using var d = Doc("key:\nother: value\n");
        Assert.AreEqual(YamlValueKind.Null, d.RootElement.GetProperty("key").ValueKind);
        Assert.AreEqual("value", d.RootElement.GetProperty("other").GetString());
    }

    /// <summary>Verifies that an explicit complex key with no value resolves to null.</summary>
    [TestMethod]
    public void Parse_WhenExplicitComplexKey_ShouldHaveNullValue()
    {
        using var d = Doc("? key\n:\n");
        Assert.AreEqual(YamlValueKind.Null, d.RootElement.GetProperty("key").ValueKind);
    }
}
