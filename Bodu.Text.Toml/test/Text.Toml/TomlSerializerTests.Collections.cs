// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the collection value model of <see cref="TomlSerializer" />: a sequence member (including the queue-,
/// stack-, and bag-shaped collections that do not implement <see cref="ICollection{T}" />) maps to a TOML array
/// preserving element order - with the stack-reversing round-trip - empty and nested arrays are handled, a
/// <see langword="null" /> element is rejected, an interface-typed member materializes a concrete list on read, and a
/// top-level collection is rejected because a TOML document's root must be a table.
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

        ListModel roundTripped = TomlSerializer.Deserialize<ListModel>(text);
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

        ArrayModel roundTripped = TomlSerializer.Deserialize<ArrayModel>(text);
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

        ListModel roundTripped = TomlSerializer.Deserialize<ListModel>(text);
        Assert.IsEmpty(roundTripped.Numbers);
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

        NestedListModel roundTripped = TomlSerializer.Deserialize<NestedListModel>(text);
        Assert.HasCount(2, roundTripped.Matrix);
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

        StringListModel roundTripped = TomlSerializer.Deserialize<StringListModel>(text);
        CollectionAssert.AreEqual(new[] { "a b", "c\td" }, roundTripped.Words);
    }

    /// <summary>
    /// Verifies that a collection of a native scalar kind other than integer - here Boolean - serializes to a TOML array
    /// of that kind and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBooleanCollection_ShouldEmitBooleanArrayAndRoundTrip()
    {
        var model = new BoolListModel { Flags = [true, false, true] };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Flags = [true, false, true]\n", text);

        BoolListModel roundTripped = TomlSerializer.Deserialize<BoolListModel>(text);
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

        HashSetModel roundTripped = TomlSerializer.Deserialize<HashSetModel>(text);
        Assert.IsInstanceOfType<HashSet<int>>(roundTripped.Numbers);
        Assert.Contains(5, roundTripped.Numbers);
    }

    /// <summary>
    /// Verifies that an <see cref="System.Collections.Generic.IList{T}" />-typed member materializes a concrete
    /// <see cref="System.Collections.Generic.List{T}" /> on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIListMember_ShouldMaterializeList()
    {
        IListModel model = TomlSerializer.Deserialize<IListModel>("Numbers = [1, 2]\n");

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
        EnumerableModel model = TomlSerializer.Deserialize<EnumerableModel>("Numbers = [1, 2]\n");

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
        CollectionModel model = TomlSerializer.Deserialize<CollectionModel>("Numbers = [9]\n");

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

        IListModel roundTripped = TomlSerializer.Deserialize<IListModel>(text);
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
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new List<int> { 1 });
        });

        Assert.IsTrue(ex.Message.Contains("List", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Generic.Queue{T}" /> member serializes to a TOML array in
    /// dequeue (first-in) order and round-trips to a queue that dequeues the same sequence.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenQueueMember_ShouldRoundTripInDequeueOrder()
    {
        var model = new QueueModel();
        model.Items.Enqueue(1);
        model.Items.Enqueue(2);
        model.Items.Enqueue(3);

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Items = [1, 2, 3]\n", text);

        QueueModel roundTripped = TomlSerializer.Deserialize<QueueModel>(text);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, roundTripped.Items.ToArray());
        Assert.AreEqual(1, roundTripped.Items.Dequeue());
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Generic.Stack{T}" /> member serializes to a TOML array in pop
    /// order (most recently pushed first), the stack's natural enumeration order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStackMember_ShouldWriteElementsInPopOrder()
    {
        var model = new StackModel();
        model.Items.Push(1);
        model.Items.Push(2);
        model.Items.Push(3);

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Items = [3, 2, 1]\n", text);
    }

    /// <summary>
    /// Verifies that deserializing a TOML array into a <see cref="System.Collections.Generic.Stack{T}" /> pushes the
    /// elements in document order, so the last document element becomes the top of the stack.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStackMember_ShouldPushElementsInDocumentOrder()
    {
        StackModel model = TomlSerializer.Deserialize<StackModel>("Items = [1, 2, 3]\n");

        Assert.HasCount(3, model.Items);
        Assert.AreEqual(3, model.Items.Pop());
        Assert.AreEqual(2, model.Items.Pop());
        Assert.AreEqual(1, model.Items.Pop());
    }

    /// <summary>
    /// Verifies that a serialize/deserialize round-trip reverses a <see cref="System.Collections.Generic.Stack{T}" />:
    /// the writer enumerates pop order while the reader pushes in document order, mirroring the behavior of
    /// <see cref="System.Text.Json.JsonSerializer" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStackMember_ShouldReverseElementOrder()
    {
        var model = new StackModel { Items = new Stack<int>(new[] { 1, 2 }) };

        string text = TomlSerializer.Serialize(model);
        StackModel roundTripped = TomlSerializer.Deserialize<StackModel>(text);

        CollectionAssert.AreEqual(model.Items.Reverse().ToArray(), roundTripped.Items.ToArray());
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> member serializes to a TOML
    /// array in dequeue order and round-trips to a queue yielding the same sequence.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenConcurrentQueueMember_ShouldRoundTripInDequeueOrder()
    {
        var model = new ConcurrentQueueModel();
        model.Items.Enqueue(1);
        model.Items.Enqueue(2);
        model.Items.Enqueue(3);

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Items = [1, 2, 3]\n", text);

        ConcurrentQueueModel roundTripped = TomlSerializer.Deserialize<ConcurrentQueueModel>(text);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, roundTripped.Items.ToArray());
        Assert.IsTrue(roundTripped.Items.TryPeek(out int head));
        Assert.AreEqual(1, head);
    }

    /// <summary>
    /// Verifies that a serialize/deserialize round-trip reverses a
    /// <see cref="System.Collections.Concurrent.ConcurrentStack{T}" />, matching the
    /// <see cref="System.Collections.Generic.Stack{T}" /> semantics: the writer enumerates pop order while the reader
    /// pushes in document order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenConcurrentStackMember_ShouldReverseElementOrder()
    {
        var model = new ConcurrentStackModel();
        model.Items.Push(1);
        model.Items.Push(2);

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Items = [2, 1]\n", text);

        ConcurrentStackModel roundTripped = TomlSerializer.Deserialize<ConcurrentStackModel>(text);
        Assert.IsTrue(roundTripped.Items.TryPeek(out int top));
        Assert.AreEqual(1, top);
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> member serializes to a TOML
    /// array and round-trips to a bag holding an equivalent set of elements, with no enumeration-order guarantee.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenConcurrentBagMember_ShouldRoundTripToEquivalentElements()
    {
        var model = new ConcurrentBagModel { Items = { 1, 2, 3 } };

        string text = TomlSerializer.Serialize(model);

        ConcurrentBagModel roundTripped = TomlSerializer.Deserialize<ConcurrentBagModel>(text);
        CollectionAssert.AreEquivalent(model.Items.ToList(), roundTripped.Items.ToList());
    }

    /// <summary>
    /// Verifies that serializing a top-level <see cref="System.Collections.Generic.Queue{T}" /> throws
    /// <see cref="TomlSerializationException" />, because queue-shaped collections map to TOML arrays and a TOML
    /// document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsQueue_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new Queue<int>(new[] { 1, 2 }));
        });
    }

    /// <summary>
    /// Verifies that an empty array is read as an empty collection rather than <see langword="null" />: into an
    /// <see cref="object" /> array member, into a <see cref="List{T}" /> member, and into a dictionary of objects,
    /// where it surfaces as an array element with no items.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenArrayIsEmpty_ShouldYieldEmptyCollection()
    {
        ObjectArrayMemberModel objects = TomlSerializer.Deserialize<ObjectArrayMemberModel>("S = []\n");
        Int32ListMemberModel integers = TomlSerializer.Deserialize<Int32ListMemberModel>("S = []\n");
        Dictionary<string, object> table = TomlSerializer.Deserialize<Dictionary<string, object>>("S = []\n");

        Assert.IsNotNull(objects.S, "The object array member.");
        Assert.IsEmpty(objects.S, "The object array member.");
        Assert.IsNotNull(integers.S, "The integer list member.");
        Assert.IsEmpty(integers.S, "The integer list member.");
        Assert.IsInstanceOfType<TomlElement>(table["S"], "The dictionary entry.");
        Assert.AreEqual(TomlValueKind.Array, ((TomlElement)table["S"]).ValueKind, "The dictionary entry.");
        Assert.AreEqual(0, ((TomlElement)table["S"]).GetArrayLength(), "The dictionary entry.");
    }

    /// <summary>
    /// Verifies that an integer given for a collection member throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerIsReadIntoCollectionMember_ShouldThrowTomlSerializationException()
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ListNamedListModel>("List = 123\n");
        });
    }

    /// <summary>
    /// Verifies that a table given for a string collection member throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringCollectionMemberIsGivenTable_ShouldThrowTomlSerializationException()
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ThingsModel>("[things]\nfoo = \"bar\"");
        });
    }

    /// <summary>
    /// Verifies that an implicit table, defined only by the array-of-tables headers beneath it, given for a collection
    /// member throws <see cref="TomlSerializationException" />, because a table binds only to a table-shaped member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCollectionMemberIsGivenImplicitTable_ShouldThrowTomlSerializationException()
    {
        const string toml = "[[rules.allowlists]]\n  description = \"a\"\n\n[[rules.allowlists]]\n  description = \"b\"\n";

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RuleListModel>(toml);
        });
    }

    /// <summary>
    /// Verifies that serializing an array that holds a <see langword="null" /> element throws
    /// <see cref="TomlSerializationException" /> rather than dropping the element or the array: a nullable-integer
    /// array member through the serializer, and a <see cref="TomlArray" /> through <see cref="TomlNode.ToUtf8Bytes" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenArrayElementIsNull_ShouldThrowTomlSerializationException()
    {
        var model = new NullableInt32ArrayModel { Values = [1, null] };
        var root = new TomlObject { ["values"] = new TomlArray { 1, null } };

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that a list and an array of objects are written as arrays of tables and read back to equal items in the
    /// same order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMembersAreListAndArrayOfClass_ShouldRoundTrip()
    {
        var original = new NamedValueCollectionsModel
        {
            ItemList = [new() { Name = "a", Value = 1 }, new() { Name = "b", Value = 2 }],
            ItemArray = [new() { Name = "c", Value = 3 }],
        };

        string text = TomlSerializer.Serialize(original);
        NamedValueCollectionsModel roundTripped = TomlSerializer.Deserialize<NamedValueCollectionsModel>(text);

        Assert.AreEqual(
            "[[ItemList]]\nName = \"a\"\nValue = 1\n\n[[ItemList]]\nName = \"b\"\nValue = 2\n\n[[ItemArray]]\nName = \"c\"\nValue = 3\n",
            text);
        CollectionAssert.AreEqual(new[] { ("a", 1), ("b", 2) }, roundTripped.ItemList.Select(item => (item.Name, item.Value)).ToArray());
        CollectionAssert.AreEqual(new[] { ("c", 3) }, roundTripped.ItemArray.Select(item => (item.Name, item.Value)).ToArray());
    }

    /// <summary>
    /// Verifies that an <see cref="ObservableCollection{T}" /> member is written as an array and read back into an
    /// <see cref="ObservableCollection{T}" /> holding the same items.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemberIsObservableCollection_ShouldRoundTrip()
    {
        var original = new ObservableCollectionModel { Global = ["Hello, World!"] };

        string text = TomlSerializer.Serialize(original);
        ObservableCollectionModel roundTripped = TomlSerializer.Deserialize<ObservableCollectionModel>(text);

        Assert.AreEqual("Global = [\"Hello, World!\"]\n", text);
        CollectionAssert.AreEqual(new[] { "Hello, World!" }, roundTripped.Global);
    }

    /// <summary>
    /// Verifies that an array with more items than a collection's initial capacity is read in full and in order, into
    /// an integer array member under the snake-case policy and into a <see cref="TomlNode" /> tree.
    /// </summary>
    /// <param name="count">The number of items in the array.</param>
    [TestMethod]
    [DataRow(17)]
    [DataRow(33)]
    public void Deserialize_WhenArrayExceedsInitialCapacity_ShouldReadEveryItem(int count)
    {
        int[] expected = Enumerable.Range(1, count).ToArray();
        string toml = $"items = [{string.Join(", ", expected.Select(item => item.ToString(CultureInfo.InvariantCulture)))}]\n";
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

        ItemsArrayModel model = TomlSerializer.Deserialize<ItemsArrayModel>(toml, options);
        TomlArray node = TomlNode.Parse(Encoding.UTF8.GetBytes(toml))!["items"]!.AsArray();

        CollectionAssert.AreEqual(expected, model.Items, "The integer array member.");
        CollectionAssert.AreEqual(expected, node.Select(item => (int)item!).ToArray(), "The node tree.");
    }

    /// <summary>
    /// Verifies that an array of inline tables binds one item per table to a list of objects, a key a table omits
    /// leaving that item's default, and that serializing the model again keeps all three tables.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenArrayOfInlineTablesTargetsListOfClass_ShouldBindEachTable()
    {
        const string toml = "sub = [ { id = \"id1\", publish = true }, { id = \"id2\", publish = false }, { id = \"id3\" } ]\n";
        (string, bool)[] expected = [("id1", true), ("id2", false), ("id3", false)];

        PublishListModel model = TomlSerializer.Deserialize<PublishListModel>(toml);
        PublishListModel reread = TomlSerializer.Deserialize<PublishListModel>(TomlSerializer.Serialize(model));

        CollectionAssert.AreEqual(expected, model.Sub.Select(item => (item.Id, item.Publish)).ToArray(), "The document read.");
        CollectionAssert.AreEqual(expected, reread.Sub.Select(item => (item.Id, item.Publish)).ToArray(), "The model written and read again.");
    }

    /// <summary>
    /// A model with a list member.
    /// </summary>
    private sealed class ListModel
    {
        /// <summary>Gets or sets the integer list.</summary>
        /// <value>The list.</value>
        public List<int> Numbers { get; set; } = [];
    }

    /// <summary>
    /// A model with an array member.
    /// </summary>
    private sealed class ArrayModel
    {
        /// <summary>Gets or sets the integer array.</summary>
        /// <value>The array.</value>
        public int[] Numbers { get; set; } = [];
    }

    /// <summary>
    /// A model with a nested-list member.
    /// </summary>
    private sealed class NestedListModel
    {
        /// <summary>Gets or sets the matrix of integers.</summary>
        /// <value>The matrix.</value>
        public List<List<int>> Matrix { get; set; } = [];
    }

    /// <summary>
    /// A model with a string-list member.
    /// </summary>
    private sealed class StringListModel
    {
        /// <summary>Gets or sets the word list.</summary>
        /// <value>The list.</value>
        public List<string> Words { get; set; } = [];
    }

    /// <summary>
    /// A model with a Boolean-list member.
    /// </summary>
    private sealed class BoolListModel
    {
        /// <summary>Gets or sets the flag list.</summary>
        /// <value>The list.</value>
        public List<bool> Flags { get; set; } = [];
    }

    /// <summary>
    /// A model with a list member whose elements may be <see langword="null" />.
    /// </summary>
    private sealed class NullableElementModel
    {
        /// <summary>Gets or sets the word list, possibly containing a <see langword="null" /> element.</summary>
        /// <value>The list.</value>
        public List<string?> Words { get; set; } = [];
    }

    /// <summary>
    /// A model with a hash-set member.
    /// </summary>
    private sealed class HashSetModel
    {
        /// <summary>Gets or sets the integer set.</summary>
        /// <value>The set.</value>
        public HashSet<int> Numbers { get; set; } = [];
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.IList{T}" />-typed member.
    /// </summary>
    private sealed class IListModel
    {
        /// <summary>Gets or sets the integer list, typed as an interface.</summary>
        /// <value>The list.</value>
        public IList<int> Numbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.IEnumerable{T}" />-typed member.
    /// </summary>
    private sealed class EnumerableModel
    {
        /// <summary>Gets or sets the integer sequence, typed as an interface.</summary>
        /// <value>The sequence.</value>
        public IEnumerable<int> Numbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.ICollection{T}" />-typed member.
    /// </summary>
    private sealed class CollectionModel
    {
        /// <summary>Gets or sets the integer collection, typed as an interface.</summary>
        /// <value>The collection.</value>
        public ICollection<int> Numbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// A model with a <see cref="System.Collections.Generic.Queue{T}" /> member.
    /// </summary>
    private sealed class QueueModel
    {
        /// <summary>Gets or sets the integer queue.</summary>
        /// <value>The queue.</value>
        public Queue<int> Items { get; set; } = new();
    }

    /// <summary>
    /// A model with a <see cref="System.Collections.Generic.Stack{T}" /> member.
    /// </summary>
    private sealed class StackModel
    {
        /// <summary>Gets or sets the integer stack.</summary>
        /// <value>The stack.</value>
        public Stack<int> Items { get; set; } = new();
    }

    /// <summary>
    /// A model with a <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> member.
    /// </summary>
    private sealed class ConcurrentQueueModel
    {
        /// <summary>Gets or sets the integer queue.</summary>
        /// <value>The queue.</value>
        public System.Collections.Concurrent.ConcurrentQueue<int> Items { get; set; } = new();
    }

    /// <summary>
    /// A model with a <see cref="System.Collections.Concurrent.ConcurrentStack{T}" /> member.
    /// </summary>
    private sealed class ConcurrentStackModel
    {
        /// <summary>Gets or sets the integer stack.</summary>
        /// <value>The stack.</value>
        public System.Collections.Concurrent.ConcurrentStack<int> Items { get; set; } = new();
    }

    /// <summary>
    /// A model with a <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> member.
    /// </summary>
    private sealed class ConcurrentBagModel
    {
        /// <summary>Gets or sets the integer bag.</summary>
        /// <value>The bag.</value>
        public System.Collections.Concurrent.ConcurrentBag<int> Items { get; set; } = new();
    }

    /// <summary>
    /// A model with an <see cref="object" /> array member that holds <see langword="null" /> until it is read.
    /// </summary>
    private sealed class ObjectArrayMemberModel
    {
        /// <summary>
        /// Gets or sets the object array.
        /// </summary>
        /// <value>The array, or <see langword="null" />.</value>
        public object[]? S { get; set; }
    }

    /// <summary>
    /// A model with an integer list member that holds <see langword="null" /> until it is read.
    /// </summary>
    private sealed class Int32ListMemberModel
    {
        /// <summary>
        /// Gets or sets the integer list.
        /// </summary>
        /// <value>The list, or <see langword="null" />.</value>
        public List<int>? S { get; set; }
    }

    /// <summary>
    /// A model whose string list member is named <c>List</c>.
    /// </summary>
    private sealed class ListNamedListModel
    {
        /// <summary>
        /// Gets or sets the string list.
        /// </summary>
        /// <value>The list.</value>
        public List<string> List { get; set; } = [];
    }

    /// <summary>
    /// A model with a string list member written under the key <c>things</c>.
    /// </summary>
    private sealed class ThingsModel
    {
        /// <summary>
        /// Gets or sets the strings.
        /// </summary>
        /// <value>The strings.</value>
        [PropertyName("things")]
        public List<string> Things { get; set; } = [];
    }

    /// <summary>
    /// A model with a list of rules written under the key <c>rules</c>.
    /// </summary>
    private sealed class RuleListModel
    {
        /// <summary>
        /// Gets or sets the rules.
        /// </summary>
        /// <value>The rules.</value>
        [PropertyName("rules")]
        public List<AllowlistRuleModel> Rules { get; set; } = [];
    }

    /// <summary>
    /// A rule holding a list of allowlists.
    /// </summary>
    private sealed class AllowlistRuleModel
    {
        /// <summary>
        /// Gets or sets the allowlists.
        /// </summary>
        /// <value>The allowlists.</value>
        [PropertyName("allowlists")]
        public List<AllowlistModel> Allowlists { get; set; } = [];
    }

    /// <summary>
    /// An allowlist with a description.
    /// </summary>
    private sealed class AllowlistModel
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [PropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a nullable-integer array member.
    /// </summary>
    private sealed class NullableInt32ArrayModel
    {
        /// <summary>
        /// Gets or sets the values, which may hold <see langword="null" /> elements.
        /// </summary>
        /// <value>The values.</value>
        public int?[] Values { get; set; } = [];
    }

    /// <summary>
    /// An item with a name and a value, written as a table.
    /// </summary>
    private sealed class NamedValueItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model with a list and an array of <see cref="NamedValueItem" />, each written as an array of tables.
    /// </summary>
    private sealed class NamedValueCollectionsModel
    {
        /// <summary>
        /// Gets or sets the items held in a list.
        /// </summary>
        /// <value>The list.</value>
        public List<NamedValueItem> ItemList { get; set; } = [];

        /// <summary>
        /// Gets or sets the items held in an array.
        /// </summary>
        /// <value>The array.</value>
        public NamedValueItem[] ItemArray { get; set; } = [];
    }

    /// <summary>
    /// A model with an <see cref="ObservableCollection{T}" /> member.
    /// </summary>
    private sealed class ObservableCollectionModel
    {
        /// <summary>
        /// Gets or sets the strings.
        /// </summary>
        /// <value>The collection.</value>
        public ObservableCollection<string> Global { get; set; } = [];
    }

    /// <summary>
    /// A model with an integer array member, written <c>items</c> under the snake-case policy.
    /// </summary>
    private sealed class ItemsArrayModel
    {
        /// <summary>
        /// Gets or sets the integers.
        /// </summary>
        /// <value>The integers.</value>
        public int[] Items { get; set; } = [];
    }

    /// <summary>
    /// A model with a list of publish entries written under the key <c>sub</c>.
    /// </summary>
    private sealed class PublishListModel
    {
        /// <summary>
        /// Gets or sets the publish entries.
        /// </summary>
        /// <value>The entries.</value>
        [PropertyName("sub")]
        public List<PublishEntryModel> Sub { get; set; } = [];
    }

    /// <summary>
    /// A publish entry with an identifier and a flag.
    /// </summary>
    private sealed class PublishEntryModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [PropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the entry is published.
        /// </summary>
        /// <value><see langword="true" /> when published; <see langword="false" /> by default.</value>
        [PropertyName("publish")]
        public bool Publish { get; set; }
    }
}
