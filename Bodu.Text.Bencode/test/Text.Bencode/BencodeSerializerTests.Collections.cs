// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Collections.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> maps collection types to and from a Bencode list: the supported
/// collection shapes (including the queue-, stack-, and bag-shaped collections that do not implement
/// <see cref="ICollection{T}" />), element ordering and the stack-reversing round-trip, empty and nested collections,
/// the concrete type that each collection interface materializes to on read, the element types Bencode can carry, and
/// the rejection of null elements.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that an <see cref="int" /> array serializes to a Bencode list and round-trips, preserving order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void SerializeDeserialize_WhenIntArray_ShouldRoundTripToBencodeList()
    {
        int[] value = [3, 1, 2];

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("li3ei1ei2ee", Encoding.Latin1.GetString(bytes));

        int[] roundTripped = BencodeSerializer.Deserialize<int[]>(bytes);
        CollectionAssert.AreEqual(value, roundTripped);
    }

    /// <summary>
    /// Verifies that an empty collection serializes to the empty Bencode list <c>le</c> and round-trips to an empty
    /// collection.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCollectionEmpty_ShouldRoundTripToEmptyList()
    {
        var value = new List<int>();

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("le", Encoding.Latin1.GetString(bytes));

        List<int> roundTripped = BencodeSerializer.Deserialize<List<int>>(bytes);
        Assert.AreEqual(0, roundTripped.Count);
    }

    /// <summary>
    /// Verifies that a list of strings serializes to a Bencode list of byte strings and round-trips, preserving order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringList_ShouldRoundTripToListOfByteStrings()
    {
        var value = new List<string> { "a", "bb", "ccc" };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("l1:a2:bb3:ccce", Encoding.Latin1.GetString(bytes));

        List<string> roundTripped = BencodeSerializer.Deserialize<List<string>>(bytes);
        CollectionAssert.AreEqual(value, roundTripped);
    }

    /// <summary>
    /// Verifies that a list of byte arrays serializes to a Bencode list of byte strings and round-trips losslessly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayList_ShouldRoundTripToListOfByteStrings()
    {
        var value = new List<byte[]> { new byte[] { 0x01, 0x02 }, Array.Empty<byte>() };

        byte[] bytes = BencodeSerializer.Serialize(value);
        // l {2: 0x01 0x02}{0:} e — each byte-string element carries the raw bytes after its length prefix.
        byte[] expected = [.. Encoding.Latin1.GetBytes("l2:"), 0x01, 0x02, .. Encoding.Latin1.GetBytes("0:e")];
        CollectionAssert.AreEqual(expected, bytes);

        List<byte[]> roundTripped = BencodeSerializer.Deserialize<List<byte[]>>(bytes);
        Assert.AreEqual(2, roundTripped.Count);
        CollectionAssert.AreEqual(value[0], roundTripped[0]);
        CollectionAssert.AreEqual(value[1], roundTripped[1]);
    }

    /// <summary>
    /// Verifies that a list of enumeration values serializes to a Bencode list of member-name byte strings and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumList_ShouldRoundTripToListOfMemberNames()
    {
        var value = new List<Color> { Color.Red, Color.Blue };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("l3:Red4:Bluee", Encoding.Latin1.GetString(bytes));

        List<Color> roundTripped = BencodeSerializer.Deserialize<List<Color>>(bytes);
        CollectionAssert.AreEqual(value, roundTripped);
    }

    /// <summary>
    /// Verifies that a list of nested objects serializes to a Bencode list of dictionaries and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenObjectList_ShouldRoundTripToListOfDictionaries()
    {
        var value = new List<Item>
        {
            new() { Id = 1, Label = "a" },
            new() { Id = 2, Label = "b" },
        };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("ld2:Idi1e5:Label1:aed2:Idi2e5:Label1:bee", Encoding.Latin1.GetString(bytes));

        List<Item> roundTripped = BencodeSerializer.Deserialize<List<Item>>(bytes);
        Assert.AreEqual(2, roundTripped.Count);
        Assert.AreEqual(1, roundTripped[0].Id);
        Assert.AreEqual("a", roundTripped[0].Label);
        Assert.AreEqual(2, roundTripped[1].Id);
        Assert.AreEqual("b", roundTripped[1].Label);
    }

    /// <summary>
    /// Verifies that a nested <see cref="List{T}" /> of lists serializes to a Bencode list of lists and round-trips,
    /// preserving the inner element order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNestedListOfLists_ShouldRoundTrip()
    {
        var value = new List<List<int>> { new() { 1, 2 }, new() { 3 } };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("lli1ei2eeli3eee", Encoding.Latin1.GetString(bytes));

        List<List<int>> roundTripped = BencodeSerializer.Deserialize<List<List<int>>>(bytes);
        Assert.AreEqual(2, roundTripped.Count);
        CollectionAssert.AreEqual(value[0], roundTripped[0]);
        CollectionAssert.AreEqual(value[1], roundTripped[1]);
    }

    /// <summary>
    /// Verifies that an ordered collection preserves element order through a serialize/deserialize round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenOrderedCollection_ShouldPreserveOrder()
    {
        int[] value = [5, 4, 3, 2, 1];

        byte[] bytes = BencodeSerializer.Serialize(value);
        List<int> roundTripped = BencodeSerializer.Deserialize<List<int>>(bytes);

        CollectionAssert.AreEqual(value, roundTripped);
    }

    /// <summary>
    /// Verifies that a <see cref="HashSet{T}" /> serializes to a Bencode list and round-trips to an equivalent set.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenHashSet_ShouldRoundTripToEquivalentSet()
    {
        var value = new HashSet<int> { 1, 2, 3 };

        byte[] bytes = BencodeSerializer.Serialize(value);

        HashSet<int> roundTripped = BencodeSerializer.Deserialize<HashSet<int>>(bytes);
        Assert.IsInstanceOfType<HashSet<int>>(roundTripped);
        CollectionAssert.AreEquivalent(value.ToList(), roundTripped.ToList());
    }

    /// <summary>
    /// Verifies that serializing a collection that contains a <see langword="null" /> element throws
    /// <see cref="BencodeSerializationException" />, because Bencode has no null token.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCollectionContainsNullElement_ShouldThrowBencodeSerializationException()
    {
        var value = new List<string?> { "a", null };

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(value);
        });
    }

    /// <summary>
    /// Verifies that serializing an array that contains a <see langword="null" /> element throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenArrayContainsNullElement_ShouldThrowBencodeSerializationException()
    {
        string?[] value = ["a", null];

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(value);
        });
    }

    /// <summary>
    /// Verifies that each supported collection shape that round-trips through a Bencode list produces the same element
    /// sequence after deserialization, and that the supplied collection interfaces materialize to the expected concrete
    /// type.
    /// </summary>
    /// <param name="kat">The collection-shape scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(CollectionShapeCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenCollectionShape_ShouldRoundTripAndMaterializeExpectedType(CollectionShapeKat kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = kat.Serialize();
        Assert.AreEqual("li1ei2ei3ee", Encoding.Latin1.GetString(bytes));

        object roundTripped = kat.Deserialize(Encoding.Latin1.GetBytes("li1ei2ei3ee"));
        Assert.IsInstanceOfType(roundTripped, kat.ExpectedConcreteType);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ((IEnumerable<int>)roundTripped).ToArray());
    }

    /// <summary>
    /// Verifies that a <see cref="Queue{T}" /> serializes to a Bencode list in dequeue (first-in) order and
    /// round-trips to a queue that dequeues the same sequence.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenQueue_ShouldRoundTripInDequeueOrder()
    {
        var value = new Queue<int>();
        value.Enqueue(1);
        value.Enqueue(2);
        value.Enqueue(3);

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("li1ei2ei3ee", Encoding.Latin1.GetString(bytes));

        Queue<int> roundTripped = BencodeSerializer.Deserialize<Queue<int>>(bytes);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, roundTripped.ToArray());
        Assert.AreEqual(1, roundTripped.Dequeue());
    }

    /// <summary>
    /// Verifies that a <see cref="Stack{T}" /> serializes to a Bencode list in pop order (most recently pushed
    /// first), the stack's natural enumeration order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStack_ShouldWriteElementsInPopOrder()
    {
        var value = new Stack<int>();
        value.Push(1);
        value.Push(2);
        value.Push(3);

        byte[] bytes = BencodeSerializer.Serialize(value);

        Assert.AreEqual("li3ei2ei1ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing a Bencode list into a <see cref="Stack{T}" /> pushes the elements in document
    /// order, so the last document element becomes the top of the stack.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStack_ShouldPushElementsInDocumentOrder()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("li1ei2ei3ee");

        Stack<int> stack = BencodeSerializer.Deserialize<Stack<int>>(bytes);

        Assert.AreEqual(3, stack.Count);
        Assert.AreEqual(3, stack.Pop());
        Assert.AreEqual(2, stack.Pop());
        Assert.AreEqual(1, stack.Pop());
    }

    /// <summary>
    /// Verifies that a serialize/deserialize round-trip reverses a <see cref="Stack{T}" />: the writer enumerates pop
    /// order while the reader pushes in document order, mirroring the behavior of
    /// <see cref="System.Text.Json.JsonSerializer" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStack_ShouldReverseElementOrder()
    {
        var value = new Stack<int>(new[] { 1, 2 });

        byte[] bytes = BencodeSerializer.Serialize(value);
        Stack<int> roundTripped = BencodeSerializer.Deserialize<Stack<int>>(bytes);

        CollectionAssert.AreEqual(value.Reverse().ToArray(), roundTripped.ToArray());
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> serializes to a Bencode list
    /// in dequeue order and round-trips to a queue yielding the same sequence.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenConcurrentQueue_ShouldRoundTripInDequeueOrder()
    {
        var value = new System.Collections.Concurrent.ConcurrentQueue<int>();
        value.Enqueue(1);
        value.Enqueue(2);
        value.Enqueue(3);

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("li1ei2ei3ee", Encoding.Latin1.GetString(bytes));

        ConcurrentQueue<int> roundTripped = BencodeSerializer.Deserialize<System.Collections.Concurrent.ConcurrentQueue<int>>(bytes);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, roundTripped.ToArray());
        Assert.IsTrue(roundTripped.TryPeek(out int head));
        Assert.AreEqual(1, head);
    }

    /// <summary>
    /// Verifies that a serialize/deserialize round-trip reverses a
    /// <see cref="System.Collections.Concurrent.ConcurrentStack{T}" />, matching the <see cref="Stack{T}" />
    /// semantics: the writer enumerates pop order while the reader pushes in document order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenConcurrentStack_ShouldReverseElementOrder()
    {
        var value = new System.Collections.Concurrent.ConcurrentStack<int>();
        value.Push(1);
        value.Push(2);

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("li2ei1ee", Encoding.Latin1.GetString(bytes));

        ConcurrentStack<int> roundTripped = BencodeSerializer.Deserialize<System.Collections.Concurrent.ConcurrentStack<int>>(bytes);
        Assert.IsTrue(roundTripped.TryPeek(out int top));
        Assert.AreEqual(1, top);
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> serializes to a Bencode list and
    /// round-trips to a bag holding an equivalent set of elements, with no enumeration-order guarantee.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenConcurrentBag_ShouldRoundTripToEquivalentElements()
    {
        var value = new System.Collections.Concurrent.ConcurrentBag<int> { 1, 2, 3 };

        byte[] bytes = BencodeSerializer.Serialize(value);

        ConcurrentBag<int> roundTripped = BencodeSerializer.Deserialize<System.Collections.Concurrent.ConcurrentBag<int>>(bytes);
        CollectionAssert.AreEquivalent(value.ToList(), roundTripped.ToList());
    }

    /// <summary>
    /// Gets the supported collection-shape scenarios, each carrying the same logical sequence and the concrete type the
    /// shape materializes to when read.
    /// </summary>
    /// <returns>The collection-shape rows.</returns>
    public static IEnumerable<object[]> CollectionShapeCases()
    {
        yield return Row<int[]>("int[]", [1, 2, 3], typeof(int[]));
        yield return Row<List<int>>("List<int>", [1, 2, 3], typeof(List<int>));
        yield return Row<IList<int>>("IList<int>", new List<int> { 1, 2, 3 }, typeof(List<int>));
        yield return Row<ICollection<int>>("ICollection<int>", new List<int> { 1, 2, 3 }, typeof(List<int>));
        yield return Row<IEnumerable<int>>("IEnumerable<int>", new List<int> { 1, 2, 3 }, typeof(List<int>));
        yield return Row<IReadOnlyList<int>>("IReadOnlyList<int>", new List<int> { 1, 2, 3 }, typeof(List<int>));
        yield return Row<IReadOnlyCollection<int>>("IReadOnlyCollection<int>", new List<int> { 1, 2, 3 }, typeof(List<int>));
        yield return Row<HashSet<int>>("HashSet<int>", new HashSet<int> { 1, 2, 3 }, typeof(HashSet<int>));
        yield return Row<SortedSet<int>>("SortedSet<int>", new SortedSet<int> { 1, 2, 3 }, typeof(SortedSet<int>));
        yield return Row<Collection<int>>("Collection<int>", new Collection<int> { 1, 2, 3 }, typeof(Collection<int>));
        yield return Row<ObservableCollection<int>>("ObservableCollection<int>", new ObservableCollection<int> { 1, 2, 3 }, typeof(ObservableCollection<int>));
        yield return Row<LinkedList<int>>("LinkedList<int>", new LinkedList<int>(new[] { 1, 2, 3 }), typeof(LinkedList<int>));

        // Queue shapes enumerate in dequeue order, so they fit the shared exact-sequence assertion. Stack and
        // ConcurrentStack reverse on round-trip and ConcurrentBag is unordered, so those are covered by dedicated
        // tests above instead.
        yield return Row<Queue<int>>("Queue<int>", new Queue<int>(new[] { 1, 2, 3 }), typeof(Queue<int>));
        yield return Row<System.Collections.Concurrent.ConcurrentQueue<int>>(
            "ConcurrentQueue<int>",
            new System.Collections.Concurrent.ConcurrentQueue<int>(new[] { 1, 2, 3 }),
            typeof(System.Collections.Concurrent.ConcurrentQueue<int>));

        static object[] Row<T>(string name, T value, Type expectedConcreteType)
            where T : class =>
            [
                new CollectionShapeKat(
                    name,
                    () => BencodeSerializer.Serialize(value),
                    bytes => BencodeSerializer.Deserialize<T>(bytes)!,
                    expectedConcreteType),
            ];
    }

    /// <summary>
    /// A known-answer row describing a collection shape, carrying strongly-typed serialize and deserialize actions over
    /// a sequence of three integers and the concrete type the shape materializes to when read.
    /// </summary>
    public sealed class CollectionShapeKat
        : IKat
    {
        /// <summary>
        /// The action that serializes the shape's value.
        /// </summary>
        private readonly Func<byte[]> _serialize;

        /// <summary>
        /// The action that deserializes a Bencode list into the shape.
        /// </summary>
        private readonly Func<byte[], object> _deserialize;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionShapeKat" /> class.
        /// </summary>
        /// <param name="name">The scenario label.</param>
        /// <param name="serialize">The action that serializes the shape's value.</param>
        /// <param name="deserialize">The action that deserializes a Bencode list into the shape.</param>
        /// <param name="expectedConcreteType">The concrete type the shape materializes to when read.</param>
        public CollectionShapeKat(string name, Func<byte[]> serialize, Func<byte[], object> deserialize, Type expectedConcreteType)
        {
            Name = name;
            _serialize = serialize;
            _deserialize = deserialize;
            ExpectedConcreteType = expectedConcreteType;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets the concrete type the shape materializes to when read.
        /// </summary>
        /// <returns>The expected concrete type.</returns>
        public Type ExpectedConcreteType { get; }

        /// <summary>
        /// Serializes the shape's value to Bencode bytes.
        /// </summary>
        /// <returns>The Bencode encoding.</returns>
        public byte[] Serialize() =>
            _serialize();

        /// <summary>
        /// Deserializes a Bencode list into the shape.
        /// </summary>
        /// <param name="bytes">The Bencode bytes to read.</param>
        /// <returns>The deserialized collection.</returns>
        public object Deserialize(byte[] bytes) =>
            _deserialize(bytes);
    }

    /// <summary>
    /// A nested object used as a collection element.
    /// </summary>
    private sealed class Item
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <returns>The label.</returns>
        public string Label { get; set; } = string.Empty;
    }
}
