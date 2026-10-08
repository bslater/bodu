// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.MaxDepth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializerOptions.MaxDepth" /> bounds the nesting the serializer accepts on both
/// sides of the round trip - the depth backbone the sibling serializers pin in
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
    /// Verifies that serializing an <see cref="object" /> array nested ten deep under a
    /// <see cref="YamlSerializerOptions.MaxDepth" /> of five throws <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphNestsBeyondMaxDepth_ShouldThrowYamlSerializationException()
    {
        object[] nested = Enumerable.Range(1, 10).Aggregate<int, object[]>([], (root, _) => [root]);
        var options = new YamlSerializerOptions { MaxDepth = 5 };

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Serialize(nested, options);
        });
    }

    /// <summary>
    /// Verifies that serializing a POCO chain one hundred levels deep with the default options throws
    /// <see cref="YamlSerializationException" />, as a depth limit reached through the serializer is documented to,
    /// rather than overflowing the stack.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectGraphNestsBeyondMaxDepth_ShouldThrowYamlSerializationException()
    {
        var root = new RecursiveModel();
        RecursiveModel current = root;
        for (int depth = 1; depth < 100; depth++)
        {
            current.Child = new RecursiveModel();
            current = current.Child;
        }

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Serialize(root);
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
