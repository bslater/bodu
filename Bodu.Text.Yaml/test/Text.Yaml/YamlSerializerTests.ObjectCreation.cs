// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.ObjectCreation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies object-construction behavior beyond a public parameterless constructor: a type with only a parameterized
/// constructor (such as a positional record) is built by binding its constructor parameters, and a get-only collection
/// member marked <see cref="ObjectCreationHandling.Populate" /> merges the read items into its existing instance
/// rather than being skipped.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a positional record — which has no parameterless constructor — round-trips by binding its
    /// constructor parameters from the mapping.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPositionalRecord_ShouldRoundTrip()
    {
        var point = new PointRecord(3, 4);

        string yaml = YamlSerializer.Serialize(point);
        Assert.AreEqual("Left: 3\nTop: 4\n", yaml);

        PointRecord back = YamlSerializer.Deserialize<PointRecord>(yaml)!;
        Assert.AreEqual(point, back);
    }

    /// <summary>
    /// Verifies that a constructor parameter absent from the input takes its declared default value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterMissing_ShouldUseDefault()
    {
        LabeledRecord back = YamlSerializer.Deserialize<LabeledRecord>("Id: 7\n")!;

        Assert.AreEqual(7, back.Id);
        Assert.AreEqual("none", back.Label);
    }

    /// <summary>
    /// Verifies that a get-only collection member marked <see cref="ObjectCreationHandling.Populate" /> merges the
    /// read items into the instance the type initializes rather than replacing it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateGetOnlyCollection_ShouldMergeIntoExistingInstance()
    {
        PopulateModel model = YamlSerializer.Deserialize<PopulateModel>("Items:\n  - 2\n  - 3\n")!;

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>A positional record with two constructor-bound members.</summary>
    /// <param name="Left">The left coordinate.</param>
    /// <param name="Top">The top coordinate.</param>
    private sealed record PointRecord(int Left, int Top);

    /// <summary>A positional record with a constructor parameter that carries a default value.</summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="Label">The label, defaulting to <c>none</c>.</param>
    private sealed record LabeledRecord(int Id, string Label = "none");

    /// <summary>A model whose get-only collection is populated rather than replaced.</summary>
    private sealed class PopulateModel
    {
        /// <summary>
        /// Gets the items, seeded with a single element and populated with the read items.
        /// </summary>
        /// <value>The items.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public List<int> Items { get; } = new() { 1 };
    }
}
