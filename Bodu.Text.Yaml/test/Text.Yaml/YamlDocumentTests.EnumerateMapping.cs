// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.EnumerateMapping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlElement.EnumerateMapping" />.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that mapping enumeration yields all pairs in order.</summary>
    [TestMethod]
    public void EnumerateMapping_ShouldYieldPairsInOrder()
    {
        using var doc = YamlDocument.Parse("a: 1\nb: 2\nc: 3\n");
        var keys = new List<string>();
        foreach (var pair in doc.RootElement.EnumerateMapping())
            keys.Add(pair.Name);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, keys);
    }
}
