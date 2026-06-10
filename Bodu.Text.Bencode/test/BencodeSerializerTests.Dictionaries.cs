// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Dictionaries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> maps string-keyed dictionaries to and from a Bencode dictionary:
/// canonical key ordering, the supported dictionary shapes and their materialized concrete types, empty and nested
/// dictionaries, null-value omission, and the treatment of dictionaries whose key is not a <see cref="string" />.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a string-keyed dictionary serializes to a Bencode dictionary and round-trips to an equivalent
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringKeyedDictionary_ShouldRoundTripToBencodeDictionary()
    {
        var value = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d1:ai1e1:bi2ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<Dictionary<string, int>>(bytes);
        CollectionAssert.AreEquivalent(value, roundTripped);
    }

    /// <summary>
    /// Verifies that dictionary entries supplied out of byte order are emitted in ascending bytewise key order, so the
    /// output is canonical Bencode.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryKeysOutOfOrder_ShouldEmitSortedKeys()
    {
        var value = new Dictionary<string, int> { ["zebra"] = 1, ["apple"] = 2, ["mango"] = 3 };

        byte[] bytes = BencodeSerializer.Serialize(value);

        Assert.AreEqual("d5:applei2e5:mangoi3e5:zebrai1ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that an empty dictionary serializes to the empty Bencode dictionary <c>de</c> and round-trips to an
    /// empty dictionary.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDictionaryEmpty_ShouldRoundTripToEmptyDictionary()
    {
        var value = new Dictionary<string, int>();

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("de", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<Dictionary<string, int>>(bytes);
        Assert.AreEqual(0, roundTripped.Count);
    }

    /// <summary>
    /// Verifies that a string-keyed dictionary of byte arrays serializes to a Bencode dictionary of byte strings and
    /// round-trips losslessly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDictionaryOfByteArrays_ShouldRoundTripLosslessly()
    {
        var value = new Dictionary<string, byte[]> { ["a"] = [0x01, 0x02], ["b"] = [] };

        byte[] bytes = BencodeSerializer.Serialize(value);

        // d {1:a}{2: 0x01 0x02} {1:b}{0:} e — keys sort ascending, byte-string values carry the raw bytes.
        byte[] expected = [.. Encoding.Latin1.GetBytes("d1:a2:"), 0x01, 0x02, .. Encoding.Latin1.GetBytes("1:b0:e")];
        CollectionAssert.AreEqual(expected, bytes);

        var roundTripped = BencodeSerializer.Deserialize<Dictionary<string, byte[]>>(bytes);
        CollectionAssert.AreEqual(value["a"], roundTripped["a"]);
        CollectionAssert.AreEqual(value["b"], roundTripped["b"]);
    }

    /// <summary>
    /// Verifies that a string-keyed dictionary of nested objects serializes to a Bencode dictionary of dictionaries and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDictionaryOfObjects_ShouldRoundTrip()
    {
        var value = new Dictionary<string, Item>
        {
            ["x"] = new() { Id = 1, Label = "a" },
        };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d1:xd2:Idi1e5:Label1:aee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<Dictionary<string, Item>>(bytes);
        Assert.AreEqual(1, roundTripped["x"].Id);
        Assert.AreEqual("a", roundTripped["x"].Label);
    }

    /// <summary>
    /// Verifies that nested dictionaries serialize to nested Bencode dictionaries with canonically-sorted keys at each
    /// level and round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNestedDictionaries_ShouldRoundTrip()
    {
        var value = new Dictionary<string, Dictionary<string, int>>
        {
            ["outer"] = new() { ["b"] = 2, ["a"] = 1 },
        };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d5:outerd1:ai1e1:bi2eee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(bytes);
        Assert.AreEqual(1, roundTripped["outer"]["a"]);
        Assert.AreEqual(2, roundTripped["outer"]["b"]);
    }

    /// <summary>
    /// Verifies that a dictionary entry whose value is <see langword="null" /> is omitted from the serialized
    /// dictionary, because Bencode has no null token, while non-null entries are retained.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryValueIsNull_ShouldOmitEntry()
    {
        var value = new Dictionary<string, string?> { ["a"] = null, ["b"] = "x" };

        byte[] bytes = BencodeSerializer.Serialize(value);

        Assert.AreEqual("d1:b1:xe", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that each supported string-keyed dictionary shape round-trips through a Bencode dictionary and that the
    /// supplied dictionary interfaces materialize to the expected concrete type.
    /// </summary>
    /// <param name="kat">The dictionary-shape scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(DictionaryShapeCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenDictionaryShape_ShouldRoundTripAndMaterializeExpectedType(DictionaryShapeKat kat)
    {
        byte[] bytes = kat.Serialize();
        Assert.AreEqual("d1:ai1e1:bi2ee", Encoding.Latin1.GetString(bytes));

        object roundTripped = kat.Deserialize(Encoding.Latin1.GetBytes("d1:ai1e1:bi2ee"));
        Assert.IsInstanceOfType(roundTripped, kat.ExpectedConcreteType);

        var entries = ((IEnumerable<KeyValuePair<string, int>>)roundTripped)
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToList();
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual("a", entries[0].Key);
        Assert.AreEqual(1, entries[0].Value);
        Assert.AreEqual("b", entries[1].Key);
        Assert.AreEqual(2, entries[1].Value);
    }

    /// <summary>
    /// Verifies that a dictionary whose key is not a <see cref="string" /> is not mapped to a Bencode dictionary but is
    /// instead treated as a sequence of key/value pairs, producing a Bencode list of two-entry dictionaries.
    /// </summary>
    /// <remarks>
    /// Bencode dictionary keys are byte strings, so only string-keyed dictionaries map to a Bencode dictionary. A
    /// dictionary with a non-string key falls through to the collection path because it implements
    /// <see cref="IEnumerable{T}" /> over <see cref="KeyValuePair{TKey, TValue}" />.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenDictionaryKeyIsNotString_ShouldEmitListOfKeyValuePairs()
    {
        var value = new Dictionary<int, int> { [1] = 2 };

        byte[] bytes = BencodeSerializer.Serialize(value);

        Assert.AreEqual("ld3:Keyi1e5:Valuei2eee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing a Bencode dictionary into a non-string-keyed dictionary throws
    /// <see cref="BencodeSerializationException" />, because such a type is bound through the collection path and a
    /// dictionary token is not a list.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDictionaryKeyIsNotString_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:1i2ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<int, int>>(bytes);
        });
    }

    /// <summary>
    /// Gets the supported string-keyed dictionary-shape scenarios, each carrying the same logical entries and the
    /// concrete type the shape materializes to when read.
    /// </summary>
    /// <returns>The dictionary-shape rows.</returns>
    public static IEnumerable<object[]> DictionaryShapeCases()
    {
        yield return Row<Dictionary<string, int>>("Dictionary<string,int>", typeof(Dictionary<string, int>));
        yield return Row<IDictionary<string, int>>("IDictionary<string,int>", typeof(Dictionary<string, int>));
        yield return Row<IReadOnlyDictionary<string, int>>("IReadOnlyDictionary<string,int>", typeof(Dictionary<string, int>));
        yield return Row<SortedDictionary<string, int>>("SortedDictionary<string,int>", typeof(SortedDictionary<string, int>));

        static object[] Row<T>(string name, Type expectedConcreteType)
            where T : class
        {
            return
            [
                new DictionaryShapeKat(
                    name,
                    () => BencodeSerializer.Serialize((T)(object)BuildSource<T>()),
                    bytes => BencodeSerializer.Deserialize<T>(bytes)!,
                    expectedConcreteType),
            ];
        }
    }

    /// <summary>
    /// Builds a populated instance of a supported string-keyed dictionary shape for serialization.
    /// </summary>
    /// <typeparam name="T">The dictionary shape.</typeparam>
    /// <returns>A dictionary containing the entries <c>a=1</c> and <c>b=2</c>.</returns>
    private static object BuildSource<T>()
    {
        if (typeof(T) == typeof(SortedDictionary<string, int>))
            return new SortedDictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        return new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
    }

    /// <summary>
    /// A known-answer row describing a string-keyed dictionary shape, carrying strongly-typed serialize and deserialize
    /// actions over the entries <c>a=1</c> and <c>b=2</c> and the concrete type the shape materializes to when read.
    /// </summary>
    public sealed class DictionaryShapeKat
        : IKat
    {
        /// <summary>
        /// The action that serializes the shape's value.
        /// </summary>
        private readonly Func<byte[]> _serialize;

        /// <summary>
        /// The action that deserializes a Bencode dictionary into the shape.
        /// </summary>
        private readonly Func<byte[], object> _deserialize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryShapeKat" /> class.
        /// </summary>
        /// <param name="name">The scenario label.</param>
        /// <param name="serialize">The action that serializes the shape's value.</param>
        /// <param name="deserialize">The action that deserializes a Bencode dictionary into the shape.</param>
        /// <param name="expectedConcreteType">The concrete type the shape materializes to when read.</param>
        public DictionaryShapeKat(string name, Func<byte[]> serialize, Func<byte[], object> deserialize, Type expectedConcreteType)
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
        /// Deserializes a Bencode dictionary into the shape.
        /// </summary>
        /// <param name="bytes">The Bencode bytes to read.</param>
        /// <returns>The deserialized dictionary.</returns>
        public object Deserialize(byte[] bytes) =>
            _deserialize(bytes);
    }
}
