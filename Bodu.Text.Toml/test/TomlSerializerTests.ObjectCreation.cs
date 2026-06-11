// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.ObjectCreation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies how <see cref="TomlSerializer" /> handles object-creation on read: the default
/// <see cref="TomlObjectCreationHandling.Replace" /> that overwrites a member's seeded value, and
/// <see cref="TomlObjectCreationHandling.Populate" /> that merges the read entries into a member's existing collection
/// or dictionary. It covers the options-level, type-level, and member-level controls and their precedence, the
/// get-only collection round-trip Populate enables, and the fallback to replacement when Populate cannot apply.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that the default <see cref="TomlObjectCreationHandling.Replace" /> overwrites a settable list that the
    /// type seeds, so only the read elements survive.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDefaultHandlingAndSeededList_ShouldReplace()
    {
        var model = TomlSerializer.Deserialize<SettableListModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObjectCreationHandling.Populate" /> on the options merges read elements into a
    /// settable list's seeded contents rather than replacing them.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPopulateAndSeededList_ShouldAppendToExisting()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = TomlObjectCreationHandling.Populate };

        var model = TomlSerializer.Deserialize<SettableListModel>("Items = [2, 3]\n", options);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObjectCreationHandling.Populate" /> lets a get-only list property, which has no
    /// setter, round-trip by populating the instance the type initialized.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPopulateAndGetOnlyList_ShouldPopulateExisting()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = TomlObjectCreationHandling.Populate };

        var model = TomlSerializer.Deserialize<GetOnlyListModel>("Items = [2, 3]\n", options);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a member annotated with the <see cref="TomlObjectCreationHandlingAttribute" /> set to
    /// <see cref="TomlObjectCreationHandling.Populate" /> merges into its seeded value even when the options leave the
    /// default <see cref="TomlObjectCreationHandling.Replace" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberPopulateAttributeAndOptionsDefault_ShouldAppendToExisting()
    {
        var model = TomlSerializer.Deserialize<MemberPopulateModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a member-level <see cref="TomlObjectCreationHandling.Replace" /> attribute overrides an
    /// options-level <see cref="TomlObjectCreationHandling.Populate" />, replacing the member's seeded value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberReplaceAttributeAndOptionsPopulate_ShouldReplace()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = TomlObjectCreationHandling.Populate };

        var model = TomlSerializer.Deserialize<MemberReplaceModel>("Items = [2, 3]\n", options);

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a type annotated with the <see cref="TomlObjectCreationHandlingAttribute" /> set to
    /// <see cref="TomlObjectCreationHandling.Populate" /> merges into every member's seeded value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypePopulateAttribute_ShouldAppendToExisting()
    {
        var model = TomlSerializer.Deserialize<TypePopulateModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a member-level <see cref="TomlObjectCreationHandling.Replace" /> attribute overrides a type-level
    /// <see cref="TomlObjectCreationHandling.Populate" />, confirming member precedence over type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberReplaceOverridesTypePopulate_ShouldReplace()
    {
        var model = TomlSerializer.Deserialize<TypePopulateWithMemberReplaceModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObjectCreationHandling.Populate" /> merges read entries into a seeded dictionary
    /// member, overwriting matching keys and adding new ones.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndSeededDictionary_ShouldMergeEntries()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = TomlObjectCreationHandling.Populate };

        var model = TomlSerializer.Deserialize<SeededDictionaryModel>("[Counts]\nb = 9\nc = 3\n", options);

        Assert.AreEqual(1, model.Counts["a"]);
        Assert.AreEqual(9, model.Counts["b"]);
        Assert.AreEqual(3, model.Counts["c"]);
        Assert.AreEqual(3, model.Counts.Count);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObjectCreationHandling.Populate" /> falls back to replacing the value when the
    /// member's existing value is <see langword="null" />, since there is no instance to populate.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndNullExistingValue_ShouldFallBackToReplace()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = TomlObjectCreationHandling.Populate };

        var model = TomlSerializer.Deserialize<NullSeedListModel>("Items = [2, 3]\n", options);

        Assert.IsNotNull(model.Items);
        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TomlObjectCreationHandlingAttribute" /> with an undefined
    /// <see cref="TomlObjectCreationHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>handling</c>.
    /// </summary>
    [TestMethod]
    public void TomlObjectCreationHandlingAttribute_WhenHandlingUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new TomlObjectCreationHandlingAttribute((TomlObjectCreationHandling)99);
        }, "handling");
    }

    /// <summary>
    /// A model whose list member is settable and seeded with a single element.
    /// </summary>
    private sealed class SettableListModel
    {
        /// <summary>
        /// Gets or sets the list, seeded with the element <c>1</c>.
        /// </summary>
        /// <returns>The list.</returns>
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose list member is get-only and seeded with a single element.
    /// </summary>
    private sealed class GetOnlyListModel
    {
        /// <summary>
        /// Gets the get-only list, seeded with the element <c>1</c>.
        /// </summary>
        /// <returns>The list.</returns>
        public List<int> Items { get; } = new() { 1 };
    }

    /// <summary>
    /// A model whose list member carries a member-level Populate attribute.
    /// </summary>
    private sealed class MemberPopulateModel
    {
        /// <summary>
        /// Gets or sets the list, merged into on read by its Populate attribute.
        /// </summary>
        /// <returns>The list.</returns>
        [TomlObjectCreationHandling(TomlObjectCreationHandling.Populate)]
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose list member carries a member-level Replace attribute.
    /// </summary>
    private sealed class MemberReplaceModel
    {
        /// <summary>
        /// Gets or sets the list, replaced on read by its Replace attribute.
        /// </summary>
        /// <returns>The list.</returns>
        [TomlObjectCreationHandling(TomlObjectCreationHandling.Replace)]
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose type carries a type-level Populate attribute.
    /// </summary>
    [TomlObjectCreationHandling(TomlObjectCreationHandling.Populate)]
    private sealed class TypePopulateModel
    {
        /// <summary>
        /// Gets or sets the list, merged into on read by the type-level Populate attribute.
        /// </summary>
        /// <returns>The list.</returns>
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose type-level Populate attribute is overridden on one member by a Replace attribute.
    /// </summary>
    [TomlObjectCreationHandling(TomlObjectCreationHandling.Populate)]
    private sealed class TypePopulateWithMemberReplaceModel
    {
        /// <summary>
        /// Gets or sets the list, replaced on read because its member-level attribute overrides the type-level
        /// Populate.
        /// </summary>
        /// <returns>The list.</returns>
        [TomlObjectCreationHandling(TomlObjectCreationHandling.Replace)]
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose dictionary member is seeded with a single entry.
    /// </summary>
    private sealed class SeededDictionaryModel
    {
        /// <summary>
        /// Gets or sets the dictionary, seeded with the entry <c>a = 1</c>.
        /// </summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, int> Counts { get; set; } = new() { ["a"] = 1 };
    }

    /// <summary>
    /// A model whose list member is settable but initialized to <see langword="null" />.
    /// </summary>
    private sealed class NullSeedListModel
    {
        /// <summary>
        /// Gets or sets the list, which begins <see langword="null" /> so Populate cannot apply.
        /// </summary>
        /// <returns>The list, or <see langword="null" />.</returns>
        public List<int>? Items { get; set; }
    }
}
