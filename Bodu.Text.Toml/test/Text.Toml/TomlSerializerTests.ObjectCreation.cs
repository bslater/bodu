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
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> on a get-only member of a class type sets the read
    /// members on the instance the member holds, leaving its other members as they were.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndGetOnlyObjectMember_ShouldPopulateTheHeldInstance()
    {
        PopulateGetOnlyObjectModel model = TomlSerializer.Deserialize<PopulateGetOnlyObjectModel>("[Inner]\nX = 5\n");

        Assert.AreEqual(5, model.Inner.X);
        Assert.AreEqual(7, model.Inner.Y);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> on a settable member of a class type populates the
    /// instance the member holds rather than replacing it with a new one.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndSettableObjectMember_ShouldPopulateTheHeldInstance()
    {
        PopulateSettableObjectModel model = TomlSerializer.Deserialize<PopulateSettableObjectModel>("[Inner]\nX = 5\n");

        Assert.IsTrue(model.HoldsSeed());
        Assert.AreEqual(5, model.Inner.X);
        Assert.AreEqual(7, model.Inner.Y);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> falls back to replacing a member of a class type
    /// that holds <see langword="null" />, since there is no instance to populate.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndNullObjectMember_ShouldFallBackToReplace()
    {
        PopulateNullObjectModel model = TomlSerializer.Deserialize<PopulateNullObjectModel>("[Inner]\nX = 5\n");

        Assert.IsNotNull(model.Inner);
        Assert.AreEqual(5, model.Inner.X);
        Assert.AreEqual(0, model.Inner.Y);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> on the options populates object members at every
    /// level, so a nested get-only member keeps the values the input does not set.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPopulateAndNestedObjectMembers_ShouldPopulateEachLevel()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        PopulateOuterModel model = TomlSerializer.Deserialize<PopulateOuterModel>("[Middle.Inner]\nX = 5\n", options);

        Assert.AreEqual(5, model.Middle.Inner.X);
        Assert.AreEqual(7, model.Middle.Inner.Y);
        Assert.AreEqual(9, model.Middle.Z);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> on a settable member of a struct type populates a
    /// copy of the held value and stores the copy back through the setter.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndSettableStructMember_ShouldPopulateAndStoreTheValue()
    {
        PopulateStructMemberModel model = TomlSerializer.Deserialize<PopulateStructMemberModel>("[Point]\nX = 5\n");

        Assert.AreEqual(5, model.Point.X);
        Assert.AreEqual(7, model.Point.Y);
    }

    /// <summary>
    /// Verifies that populating an object member runs the deserialization callbacks once on the instance the member
    /// holds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndObjectMemberHasCallbacks_ShouldRunThemOnTheHeldInstance()
    {
        PopulateCallbackModel model = TomlSerializer.Deserialize<PopulateCallbackModel>("[Inner]\nX = 5\n");

        Assert.AreEqual(5, model.Inner.X);
        Assert.AreEqual(1, model.Inner.ReadDeserializingCount());
        Assert.AreEqual(1, model.Inner.ReadDeserializedCount());
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> replaces a member whose type is built through a
    /// parameterized constructor, since a value set only by its constructor cannot be set on the held instance.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPopulateAndObjectMemberHasParameterizedConstructor_ShouldReplaceIt()
    {
        PopulateRecordMemberModel model = TomlSerializer.Deserialize<PopulateRecordMemberModel>("[Value]\nX = 5\n");

        Assert.AreEqual(new PopulatedRecord(5, 0), model.Value);
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

    /// <summary>
    /// A class whose members a populated member's input sets in part.
    /// </summary>
    private sealed class PopulatedInner
    {
        /// <summary>
        /// Gets or sets the value the input sets.
        /// </summary>
        /// <value>The value; 0 until it is read.</value>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the value the input leaves alone.
        /// </summary>
        /// <value>The value; 0 unless a model seeds it.</value>
        public int Y { get; set; }
    }

    /// <summary>
    /// A model whose get-only member of a class type, seeded with <c>Y = 7</c>, is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateGetOnlyObjectModel
    {
        /// <summary>
        /// Gets the held instance.
        /// </summary>
        /// <value>The instance the model creates.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public PopulatedInner Inner { get; } = new() { Y = 7 };
    }

    /// <summary>
    /// A model whose settable member of a class type, seeded with <c>Y = 7</c>, is marked
    /// <see cref="ObjectCreationHandling.Populate" />, and which remembers the instance it seeded.
    /// </summary>
    private sealed class PopulateSettableObjectModel
    {
        /// <summary>The instance the model seeds the member with.</summary>
        private readonly PopulatedInner _seed = new() { Y = 7 };

        /// <summary>
        /// Initializes a new instance of the <see cref="PopulateSettableObjectModel" /> class.
        /// </summary>
        public PopulateSettableObjectModel()
        {
            Inner = _seed;
        }

        /// <summary>
        /// Gets or sets the held instance.
        /// </summary>
        /// <value>The instance the model seeds, until the member is replaced.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public PopulatedInner Inner { get; set; }

        /// <summary>
        /// Returns whether the member still holds the instance the model seeded it with.
        /// </summary>
        /// <returns><see langword="true" /> when the member holds the seeded instance.</returns>
        public bool HoldsSeed() =>
            ReferenceEquals(Inner, _seed);
    }

    /// <summary>
    /// A model whose settable member of a class type holds <see langword="null" /> and is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateNullObjectModel
    {
        /// <summary>
        /// Gets or sets the held instance.
        /// </summary>
        /// <value>The instance; <see langword="null" /> until it is read.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public PopulatedInner? Inner { get; set; }
    }

    /// <summary>
    /// A model whose get-only member holds a <see cref="PopulateMiddleModel" />.
    /// </summary>
    private sealed class PopulateOuterModel
    {
        /// <summary>
        /// Gets the held middle level.
        /// </summary>
        /// <value>The instance the model creates.</value>
        public PopulateMiddleModel Middle { get; } = new();
    }

    /// <summary>
    /// The middle level of <see cref="PopulateOuterModel" />: a get-only member of a class type, seeded with
    /// <c>Y = 7</c>, and a value the input leaves alone.
    /// </summary>
    private sealed class PopulateMiddleModel
    {
        /// <summary>
        /// Gets the held inner level.
        /// </summary>
        /// <value>The instance the model creates.</value>
        public PopulatedInner Inner { get; } = new() { Y = 7 };

        /// <summary>
        /// Gets or sets a value the input leaves alone.
        /// </summary>
        /// <value>The value; 9 unless it is read.</value>
        public int Z { get; set; } = 9;
    }

    /// <summary>
    /// A struct whose members a populated member's input sets in part.
    /// </summary>
    private struct PopulatedPoint
    {
        /// <summary>
        /// Gets or sets the value the input sets.
        /// </summary>
        /// <value>The value.</value>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the value the input leaves alone.
        /// </summary>
        /// <value>The value.</value>
        public int Y { get; set; }
    }

    /// <summary>
    /// A model whose settable member of a struct type, seeded with <c>Y = 7</c>, is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateStructMemberModel
    {
        /// <summary>
        /// Gets or sets the held value.
        /// </summary>
        /// <value>The value; <c>Y = 7</c> until it is read.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public PopulatedPoint Point { get; set; } = new() { Y = 7 };
    }

    /// <summary>
    /// A class that counts the deserialization callbacks it receives.
    /// </summary>
    private sealed class PopulatedCallbackInner
        : IOnDeserializing, IOnDeserialized
    {
        /// <summary>The number of <see cref="IOnDeserializing.OnDeserializing" /> calls received.</summary>
        private int _deserializing;

        /// <summary>The number of <see cref="IOnDeserialized.OnDeserialized" /> calls received.</summary>
        private int _deserialized;

        /// <summary>
        /// Gets or sets the value the input sets.
        /// </summary>
        /// <value>The value; 0 until it is read.</value>
        public int X { get; set; }

        /// <summary>
        /// Returns the number of <see cref="IOnDeserializing.OnDeserializing" /> calls received.
        /// </summary>
        /// <returns>The call count.</returns>
        public int ReadDeserializingCount() =>
            _deserializing;

        /// <summary>
        /// Returns the number of <see cref="IOnDeserialized.OnDeserialized" /> calls received.
        /// </summary>
        /// <returns>The call count.</returns>
        public int ReadDeserializedCount() =>
            _deserialized;

        /// <inheritdoc />
        void IOnDeserializing.OnDeserializing() =>
            _deserializing++;

        /// <inheritdoc />
        void IOnDeserialized.OnDeserialized() =>
            _deserialized++;
    }

    /// <summary>
    /// A model whose get-only member holds a <see cref="PopulatedCallbackInner" /> and is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateCallbackModel
    {
        /// <summary>
        /// Gets the held instance.
        /// </summary>
        /// <value>The instance the model creates.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public PopulatedCallbackInner Inner { get; } = new();
    }

    /// <summary>
    /// A record built through its constructor, whose second value defaults to 0.
    /// </summary>
    /// <param name="X">The value the input sets.</param>
    /// <param name="Y">The value the input leaves alone.</param>
    private sealed record PopulatedRecord(int X, int Y = 0);

    /// <summary>
    /// A model whose settable member holds a <see cref="PopulatedRecord" />, seeded with <c>Y = 7</c>, and is marked
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    private sealed class PopulateRecordMemberModel
    {
        /// <summary>
        /// Gets or sets the held record.
        /// </summary>
        /// <value>The record; <c>(0, 7)</c> until it is read.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public PopulatedRecord Value { get; set; } = new(0, 7);
    }
}
