// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies parsing of YAML source into the read-only <see cref="YamlDocument" /> object model.
/// </summary>
[TestClass]
public partial class YamlDocumentTests
{
    /// <summary>Parses YAML under the default (1.2 core) schema and returns the document.</summary>
    /// <param name="yaml">The YAML source.</param>
    /// <returns>The parsed document, which the caller must dispose.</returns>
    private static YamlDocument Doc(string yaml) => YamlDocument.Parse(yaml);

    /// <summary>Parses YAML under the YAML 1.1 schema and returns the document.</summary>
    /// <param name="yaml">The YAML source.</param>
    /// <returns>The parsed document, which the caller must dispose.</returns>
    private static YamlDocument Doc11(string yaml) =>
        YamlDocument.Parse(yaml, new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });

    /// <summary>Verifies that a simple block mapping resolves scalar value kinds correctly.</summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Parse_WhenBlockMapping_ShouldResolveScalarKinds()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: two\nc: true\nd: 3.5\ne: null\nf: ~");
        var root = doc.RootElement;

        Assert.AreEqual(YamlValueKind.Mapping, root.ValueKind);
        Assert.AreEqual(1L, root.GetProperty("a").GetInt64());
        Assert.AreEqual("two", root.GetProperty("b").GetString());
        Assert.IsTrue(root.GetProperty("c").GetBoolean());
        Assert.AreEqual(3.5, root.GetProperty("d").GetDouble());
        Assert.AreEqual(YamlValueKind.Null, root.GetProperty("e").ValueKind);
        Assert.AreEqual(YamlValueKind.Null, root.GetProperty("f").ValueKind);
    }
}
