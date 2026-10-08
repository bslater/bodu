// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.ObjectCreation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Bodu.Test.Assertions;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies how <see cref="TomlSerializer" /> handles object-creation on read: the default
/// <see cref="ObjectCreationHandling.Replace" /> that overwrites a member's seeded value, and
/// <see cref="ObjectCreationHandling.Populate" /> that merges the read entries into a member's existing collection
/// or dictionary. It covers the options-level, type-level, and member-level controls and their precedence, the
/// get-only collection round-trip Populate enables, and the fallback to replacement when Populate cannot apply.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that the default <see cref="ObjectCreationHandling.Replace" /> overwrites a settable list that the
    /// type seeds, so only the read elements survive.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDefaultHandlingAndSeededList_ShouldReplace()
    {
        SettableListModel model = TomlSerializer.Deserialize<SettableListModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> on the options merges read elements into a
    /// settable list's seeded contents rather than replacing them.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPopulateAndSeededList_ShouldAppendToExisting()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        SettableListModel model = TomlSerializer.Deserialize<SettableListModel>("Items = [2, 3]\n", options);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> lets a get-only list property, which has no
    /// setter, round-trip by populating the instance the type initialized.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPopulateAndGetOnlyList_ShouldPopulateExisting()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        GetOnlyListModel model = TomlSerializer.Deserialize<GetOnlyListModel>("Items = [2, 3]\n", options);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a member annotated with the <see cref="ObjectCreationHandlingAttribute" /> set to
    /// <see cref="ObjectCreationHandling.Populate" /> merges into its seeded value even when the options leave the
    /// default <see cref="ObjectCreationHandling.Replace" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberPopulateAttributeAndOptionsDefault_ShouldAppendToExisting()
    {
        MemberPopulateModel model = TomlSerializer.Deserialize<MemberPopulateModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a member-level <see cref="ObjectCreationHandling.Replace" /> attribute overrides an
    /// options-level <see cref="ObjectCreationHandling.Populate" />, replacing the member's seeded value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberReplaceAttributeAndOptionsPopulate_ShouldReplace()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        MemberReplaceModel model = TomlSerializer.Deserialize<MemberReplaceModel>("Items = [2, 3]\n", options);

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a type annotated with the <see cref="ObjectCreationHandlingAttribute" /> set to
    /// <see cref="ObjectCreationHandling.Populate" /> merges into every member's seeded value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypePopulateAttribute_ShouldAppendToExisting()
    {
        TypePopulateModel model = TomlSerializer.Deserialize<TypePopulateModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that a member-level <see cref="ObjectCreationHandling.Replace" /> attribute overrides a type-level
    /// <see cref="ObjectCreationHandling.Populate" />, confirming member precedence over type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberReplaceOverridesTypePopulate_ShouldReplace()
    {
        TypePopulateWithMemberReplaceModel model = TomlSerializer.Deserialize<TypePopulateWithMemberReplaceModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> merges read entries into a seeded dictionary
    /// member, overwriting matching keys and adding new ones.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndSeededDictionary_ShouldMergeEntries()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        SeededDictionaryModel model = TomlSerializer.Deserialize<SeededDictionaryModel>("[Counts]\nb = 9\nc = 3\n", options);

        Assert.AreEqual(1, model.Counts["a"]);
        Assert.AreEqual(9, model.Counts["b"]);
        Assert.AreEqual(3, model.Counts["c"]);
        Assert.HasCount(3, model.Counts);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> merges read entries into a get-only dictionary
    /// member, overwriting matching keys and adding new ones.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndGetOnlyDictionary_ShouldMergeEntries()
    {
        GetOnlyDictionaryModel model = TomlSerializer.Deserialize<GetOnlyDictionaryModel>("[Counts]\nb = 9\nc = 3\n");

        Assert.AreEqual(1, model.Counts["a"]);
        Assert.AreEqual(9, model.Counts["b"]);
        Assert.AreEqual(3, model.Counts["c"]);
        Assert.HasCount(3, model.Counts);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> falls back to replacing the value when the
    /// member's existing value is <see langword="null" />, since there is no instance to populate.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndNullExistingValue_ShouldFallBackToReplace()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        NullSeedListModel model = TomlSerializer.Deserialize<NullSeedListModel>("Items = [2, 3]\n", options);

        Assert.IsNotNull(model.Items);
        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> replaces an array member, which is fixed in size
    /// and cannot be populated.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndArrayMember_ShouldReplaceIt()
    {
        PopulateArrayModel model = TomlSerializer.Deserialize<PopulateArrayModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> leaves a get-only array member as it was, since
    /// the array cannot be populated and the member cannot be replaced.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndGetOnlyArrayMember_ShouldKeepItsValue()
    {
        PopulateGetOnlyArrayModel model = TomlSerializer.Deserialize<PopulateGetOnlyArrayModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 1 }, model.Items);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> replaces a member that holds a read-only
    /// collection, which cannot be populated.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndReadOnlyCollectionMember_ShouldReplaceIt()
    {
        PopulateReadOnlyListModel model = TomlSerializer.Deserialize<PopulateReadOnlyListModel>("Items = [2, 3]\n");

        CollectionAssert.AreEqual(new[] { 2, 3 }, model.Items.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> replaces a member that holds a read-only
    /// dictionary, which cannot be populated.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndReadOnlyDictionaryMember_ShouldReplaceIt()
    {
        PopulateReadOnlyDictionaryModel model = TomlSerializer.Deserialize<PopulateReadOnlyDictionaryModel>("[Counts]\nb = 2\n");

        Assert.HasCount(1, model.Counts);
        Assert.AreEqual(2, model.Counts["b"]);
    }

    /// <summary>
    /// Verifies that constructing <see cref="ObjectCreationHandlingAttribute" /> with an undefined
    /// <see cref="ObjectCreationHandling" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>handling</c>.
    /// </summary>
    [TestMethod]
    public void ObjectCreationHandlingAttribute_WhenHandlingUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new ObjectCreationHandlingAttribute((ObjectCreationHandling)99);
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
        /// <value>The list.</value>
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
        /// <value>The list.</value>
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
        /// <value>The list.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
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
        /// <value>The list.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Replace)]
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose type carries a type-level Populate attribute.
    /// </summary>
    [ObjectCreationHandling(ObjectCreationHandling.Populate)]
    private sealed class TypePopulateModel
    {
        /// <summary>
        /// Gets or sets the list, merged into on read by the type-level Populate attribute.
        /// </summary>
        /// <value>The list.</value>
        public List<int> Items { get; set; } = new() { 1 };
    }

    /// <summary>
    /// A model whose type-level Populate attribute is overridden on one member by a Replace attribute.
    /// </summary>
    [ObjectCreationHandling(ObjectCreationHandling.Populate)]
    private sealed class TypePopulateWithMemberReplaceModel
    {
        /// <summary>
        /// Gets or sets the list, replaced on read because its member-level attribute overrides the type-level
        /// Populate.
        /// </summary>
        /// <value>The list.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Replace)]
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
        /// <value>The dictionary.</value>
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
        /// <value>The list, or <see langword="null" />.</value>
        public List<int>? Items { get; set; }
    }

    /// <summary>
    /// A model whose settable array member, seeded with one element, is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateArrayModel
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items; a single 1 until they are read.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public int[] Items { get; set; } = [1];
    }

    /// <summary>
    /// A model whose get-only array member, seeded with one element, is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateGetOnlyArrayModel
    {
        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>The items; a single 1.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public int[] Items { get; } = [1];
    }

    /// <summary>
    /// A model whose settable member holds a read-only collection and is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateReadOnlyListModel
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items; a read-only collection holding a single 1 until they are read.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public IReadOnlyList<int> Items { get; set; } = new ReadOnlyCollection<int>([1]);
    }

    /// <summary>
    /// A model whose settable member holds a read-only dictionary and is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateReadOnlyDictionaryModel
    {
        /// <summary>
        /// Gets or sets the counts.
        /// </summary>
        /// <value>The counts; a read-only dictionary mapping <c>a</c> to 1 until they are read.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public IReadOnlyDictionary<string, int> Counts { get; set; } =
            new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 });
    }

    /// <summary>
    /// A model whose get-only dictionary member, seeded with two entries, is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class GetOnlyDictionaryModel
    {
        /// <summary>
        /// Gets the dictionary, seeded with the entries <c>a = 1</c> and <c>b = 2</c>.
        /// </summary>
        /// <value>The dictionary.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public Dictionary<string, int> Counts { get; } = new() { ["a"] = 1, ["b"] = 2 };
    }
}
