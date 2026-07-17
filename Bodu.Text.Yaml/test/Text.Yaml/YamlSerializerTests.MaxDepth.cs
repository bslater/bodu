// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializerOptions.MaxDepth" /> bounds the nesting the serializer accepts on both
/// sides of the round trip — the depth backbone the sibling serializers pin in
/// <c>TomlSerializerTests.MaxDepth</c>: a graph deeper than the limit fails when writing, value nesting deeper than
/// the limit fails when reading, and a graph within the limit succeeds.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that serializing a POCO graph nested deeper than <see cref="YamlSerializerOptions.MaxDepth" /> throws
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsMaxDepth_ShouldThrowYamlSerializationException()
    {
        var options = new YamlSerializerOptions { MaxDepth = 2 };
        var deep = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel() } } };

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Serialize(deep, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a POCO graph nested no deeper than <see cref="YamlSerializerOptions.MaxDepth" />
    /// succeeds.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphWithinMaxDepth_ShouldSucceed()
    {
        var options = new YamlSerializerOptions { MaxDepth = 3 };
        var shallow = new RecursiveModel { Child = new RecursiveModel() };

        string yaml = YamlSerializer.Serialize(shallow, options);

        Assert.IsTrue(yaml.Contains("Child", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that deserializing YAML whose flow nesting exceeds <see cref="YamlSerializerOptions.MaxDepth" />
    /// throws <see cref="YamlFormatException" />, surfaced from the reader.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFlowNestingExceedsMaxDepth_ShouldThrowYamlFormatException()
    {
        var options = new YamlSerializerOptions { MaxDepth = 2 };

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            _ = YamlSerializer.Deserialize<NestedListModel>("Items: [[[1]]]\n", options);
        });
    }

    /// <summary>
    /// Verifies that deserializing YAML whose block-mapping nesting exceeds
    /// <see cref="YamlSerializerOptions.MaxDepth" /> throws <see cref="YamlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenBlockNestingExceedsMaxDepth_ShouldThrowYamlFormatException()
    {
        var options = new YamlSerializerOptions { MaxDepth = 2 };

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            _ = YamlSerializer.Deserialize<RecursiveModel>("Child:\n  Child:\n    Child:\n      Child: {}\n", options);
        });
    }

    /// <summary>
    /// A self-referential POCO used to build graphs of controlled depth.
    /// </summary>
    private sealed class RecursiveModel
    {
        /// <summary>
        /// Gets or sets the nested child.
        /// </summary>
        /// <value>The nested child, or <see langword="null" />.</value>
        public RecursiveModel? Child { get; set; }
    }

    /// <summary>
    /// A POCO whose member nests lists three levels deep.
    /// </summary>
    private sealed class NestedListModel
    {
        /// <summary>
        /// Gets or sets the nested items.
        /// </summary>
        /// <value>The nested items.</value>
        public List<List<List<int>>>? Items { get; set; }
    }
}
