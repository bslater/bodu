// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the collection value model of <see cref="TomlSerializer" />: a sequence member maps to a TOML array
/// preserving element order, empty and nested arrays are handled, a <see langword="null" /> element is rejected, an
/// interface-typed member materializes a concrete list on read, and a top-level collection is rejected because a TOML
/// document's root must be a table.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a <see cref="System.Collections.Generic.List{T}" /> member serializes to a TOML array preserving
    /// the element order and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenListMember_ShouldEmitOrderedArrayAndRoundTrip()
    {
        var model = new ListModel { Numbers = [5, 3, 9, 1] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Numbers = [5, 3, 9, 1]\n", text);

        var roundTripped = TomlSerializer.Deserialize<ListModel>(text);
        CollectionAssert.AreEqual(new[] { 5, 3, 9, 1 }, roundTripped.Numbers);
    }

    /// <summary>
    /// Verifies that an array member serializes to a TOML array and round-trips back into an array of the same element
    /// type.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenArrayMember_ShouldRoundTripToArray()
    {
        var model = new ArrayModel { Numbers = [3, 1, 2] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Numbers = [3, 1, 2]\n", text);

        var roundTripped = TomlSerializer.Deserialize<ArrayModel>(text);
        Assert.IsInstanceOfType<int[]>(roundTripped.Numbers);
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, roundTripped.Numbers);
    }

    /// <summary>
    /// Verifies that an empty collection member serializes to an empty TOML array and round-trips to an empty
    /// collection.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEmptyCollection_ShouldEmitEmptyArrayAndRoundTrip()
    {
        var model = new ListModel { Numbers = [] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Numbers = []\n", text);

        var roundTripped = TomlSerializer.Deserialize<ListModel>(text);
        Assert.AreEqual(0, roundTripped.Numbers.Count);
    }

    /// <summary>
    /// Verifies that a nested collection member serializes to a TOML array of arrays preserving inner order and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNestedCollection_ShouldEmitArrayOfArraysAndRoundTrip()
    {
        var model = new NestedListModel { Matrix = [[1, 2], [3]] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Matrix = [[1, 2], [3]]\n", text);

        var roundTripped = TomlSerializer.Deserialize<NestedListModel>(text);
        Assert.AreEqual(2, roundTripped.Matrix.Count);
        CollectionAssert.AreEqual(new[] { 1, 2 }, roundTripped.Matrix[0]);
        CollectionAssert.AreEqual(new[] { 3 }, roundTripped.Matrix[1]);
    }

    /// <summary>
    /// Verifies that a string collection whose elements need quoting serializes each element as a basic-quoted string
    /// and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringCollection_ShouldQuoteElementsAndRoundTrip()
    {
        var model = new StringListModel { Words = ["a b", "c\td"] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Words = [\"a b\", \"c\\td\"]\n", text);

        var roundTripped = TomlSerializer.Deserialize<StringListModel>(text);
        CollectionAssert.AreEqual(new[] { "a b", "c\td" }, roundTripped.Words);
    }

    /// <summary>
    /// Verifies that a collection of a native scalar kind other than integer — here Boolean — serializes to a TOML array
    /// of that kind and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBooleanCollection_ShouldEmitBooleanArrayAndRoundTrip()
    {
        var model = new BoolListModel { Flags = [true, false, true] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Flags = [true, false, true]\n", text);

        var roundTripped = TomlSerializer.Deserialize<BoolListModel>(text);
        CollectionAssert.AreEqual(new[] { true, false, true }, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that serializing a collection containing a <see langword="null" /> element throws
    /// <see cref="TomlSerializationException" />, because TOML has no null and an array cannot hold one.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCollectionContainsNullElement_ShouldThrowTomlSerializationException()
    {
        var model = new NullableElementModel { Words = ["a", null] };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Generic.HashSet{T}" /> member serializes to a TOML array and
    /// round-trips into a materialized set.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenHashSetMember_ShouldRoundTripToSet()
    {
        var model = new HashSetModel { Numbers = [5] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Numbers = [5]\n", text);

        var roundTripped = TomlSerializer.Deserialize<HashSetModel>(text);
        Assert.IsInstanceOfType<HashSet<int>>(roundTripped.Numbers);
        Assert.IsTrue(roundTripped.Numbers.Contains(5));
    }

    /// <summary>
    /// Verifies that an <see cref="System.Collections.Generic.IList{T}" />-typed member materializes a concrete
    /// <see cref="System.Collections.Generic.List{T}" /> on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIListMember_ShouldMaterializeList()
    {
        var model = TomlSerializer.Deserialize<IListModel>("Numbers = [1, 2]\n");

        Assert.IsInstanceOfType<List<int>>(model.Numbers);
        CollectionAssert.AreEqual(new[] { 1, 2 }, (System.Collections.ICollection)model.Numbers);
    }

    /// <summary>
    /// Verifies that an <see cref="System.Collections.Generic.IEnumerable{T}" />-typed member materializes a concrete
    /// <see cref="System.Collections.Generic.List{T}" /> on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIEnumerableMember_ShouldMaterializeList()
    {
        var model = TomlSerializer.Deserialize<EnumerableModel>("Numbers = [1, 2]\n");

        Assert.IsInstanceOfType<List<int>>(model.Numbers);
        CollectionAssert.AreEqual(new[] { 1, 2 }, model.Numbers.ToArray());
    }

    /// <summary>
    /// Verifies that an <see cref="System.Collections.Generic.ICollection{T}" />-typed member materializes a concrete
    /// <see cref="System.Collections.Generic.List{T}" /> on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenICollectionMember_ShouldMaterializeList()
    {
        var model = TomlSerializer.Deserialize<CollectionModel>("Numbers = [9]\n");

        Assert.IsInstanceOfType<List<int>>(model.Numbers);
        CollectionAssert.AreEqual(new[] { 9 }, (System.Collections.ICollection)model.Numbers);
    }

    /// <summary>
    /// Verifies that an interface-typed collection member serializes from its concrete backing instance to a TOML array
    /// and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInterfaceCollectionMember_ShouldRoundTrip()
    {
        var model = new IListModel { Numbers = new List<int> { 4, 5, 6 } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Numbers = [4, 5, 6]\n", text);

        var roundTripped = TomlSerializer.Deserialize<IListModel>(text);
        CollectionAssert.AreEqual(new[] { 4, 5, 6 }, (System.Collections.ICollection)roundTripped.Numbers);
    }

    /// <summary>
    /// Verifies that serializing a top-level <see cref="System.Collections.Generic.List{T}" /> throws
    /// <see cref="TomlSerializationException" />, because a TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsList_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new List<int> { 1, 2, 3 });
        });
    }

    /// <summary>
    /// Verifies that serializing a top-level <see cref="System.Collections.Generic.HashSet{T}" /> throws
    /// <see cref="TomlSerializationException" />, because a TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsSet_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new HashSet<int> { 1, 2 });
        });
    }

    /// <summary>
    /// Verifies that the message of the root-collection failure names the offending root type, confirming the
    /// root-must-be-a-table diagnostic.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsList_ShouldReportRootTypeInMessage()
    {
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new List<int> { 1 });
        });

        Assert.IsTrue(ex.Message.Contains("List", StringComparison.Ordinal));
    }

    /// <summary>
    /// A model with a list member.
    /// </summary>
    private sealed class ListModel
    {
        /// <summary>Gets or sets the integer list.</summary>
        /// <returns>The list.</returns>
        public List<int> Numbers { get; set; } = [];
    }

    /// <summary>
    /// A model with an array member.
    /// </summary>
    private sealed class ArrayModel
    {
        /// <summary>Gets or sets the integer array.</summary>
        /// <returns>The array.</returns>
        public int[] Numbers { get; set; } = [];
    }

    /// <summary>
    /// A model with a nested-list member.
    /// </summary>
    private sealed class NestedListModel
    {
        /// <summary>Gets or sets the matrix of integers.</summary>
        /// <returns>The matrix.</returns>
        public List<List<int>> Matrix { get; set; } = [];
    }

    /// <summary>
    /// A model with a string-list member.
    /// </summary>
    private sealed class StringListModel
    {
        /// <summary>Gets or sets the word list.</summary>
        /// <returns>The list.</returns>
        public List<string> Words { get; set; } = [];
    }

    /// <summary>
    /// A model with a Boolean-list member.
    /// </summary>
    private sealed class BoolListModel
    {
        /// <summary>Gets or sets the flag list.</summary>
        /// <returns>The list.</returns>
        public List<bool> Flags { get; set; } = [];
    }

    /// <summary>
    /// A model with a list member whose elements may be <see langword="null" />.
    /// </summary>
    private sealed class NullableElementModel
    {
        /// <summary>Gets or sets the word list, possibly containing a <see langword="null" /> element.</summary>
        /// <returns>The list.</returns>
        public List<string?> Words { get; set; } = [];
    }

    /// <summary>
    /// A model with a hash-set member.
    /// </summary>
    private sealed class HashSetModel
    {
        /// <summary>Gets or sets the integer set.</summary>
        /// <returns>The set.</returns>
        public HashSet<int> Numbers { get; set; } = [];
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.IList{T}" />-typed member.
    /// </summary>
    private sealed class IListModel
    {
        /// <summary>Gets or sets the integer list, typed as an interface.</summary>
        /// <returns>The list.</returns>
        public IList<int> Numbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.IEnumerable{T}" />-typed member.
    /// </summary>
    private sealed class EnumerableModel
    {
        /// <summary>Gets or sets the integer sequence, typed as an interface.</summary>
        /// <returns>The sequence.</returns>
        public IEnumerable<int> Numbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.ICollection{T}" />-typed member.
    /// </summary>
    private sealed class CollectionModel
    {
        /// <summary>Gets or sets the integer collection, typed as an interface.</summary>
        /// <returns>The collection.</returns>
        public ICollection<int> Numbers { get; set; } = new List<int>();
    }
}
