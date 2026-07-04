// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Dictionaries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> maps dictionaries to and from a Bencode dictionary: canonical key
/// ordering, the supported dictionary shapes and their materialized concrete types, empty and nested dictionaries,
/// null-value omission, the stringified round-trip of the supported non-string key types (the integer family,
/// enumerations, <see cref="Guid" />, <see cref="bool" />, and <see cref="char" />), and the fall-through treatment
/// of dictionaries keyed by an unsupported type.
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

        Dictionary<string, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<string, int>>(bytes);
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

        Dictionary<string, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<string, int>>(bytes);
        Assert.IsEmpty(roundTripped);
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

        Dictionary<string, byte[]> roundTripped = BencodeSerializer.Deserialize<Dictionary<string, byte[]>>(bytes);
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

        Dictionary<string, Item> roundTripped = BencodeSerializer.Deserialize<Dictionary<string, Item>>(bytes);
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

        Dictionary<string, Dictionary<string, int>> roundTripped = BencodeSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(bytes);
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
        ArgumentNullException.ThrowIfNull(kat);

        byte[] bytes = kat.Serialize();
        Assert.AreEqual("d1:ai1e1:bi2ee", Encoding.Latin1.GetString(bytes));

        object roundTripped = kat.Deserialize(Encoding.Latin1.GetBytes("d1:ai1e1:bi2ee"));
        Assert.IsInstanceOfType(roundTripped, kat.ExpectedConcreteType);

        var entries = ((IEnumerable<KeyValuePair<string, int>>)roundTripped)
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToList();
        Assert.HasCount(2, entries);
        Assert.AreEqual("a", entries[0].Key);
        Assert.AreEqual(1, entries[0].Value);
        Assert.AreEqual("b", entries[1].Key);
        Assert.AreEqual(2, entries[1].Value);
    }

    /// <summary>
    /// Verifies that an <see cref="int" />-keyed dictionary serializes to a Bencode dictionary whose keys are the
    /// invariant decimal text of the integers and round-trips to an equivalent dictionary.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt32KeyedDictionary_ShouldRoundTripToBencodeDictionary()
    {
        var value = new Dictionary<int, int> { [1] = 2 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d1:1i2ee", Encoding.Latin1.GetString(bytes));

        Dictionary<int, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<int, int>>(bytes);
        CollectionAssert.AreEquivalent(value, roundTripped);
    }

    /// <summary>
    /// Verifies that integer keys are canonically sorted by their stringified form rather than their numeric value,
    /// so the key <c>10</c> precedes the key <c>2</c> in the output's bytewise order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenInt32KeysStringify_ShouldEmitBytewiseSortedTextKeys()
    {
        var value = new Dictionary<int, int> { [2] = 20, [10] = 100 };

        byte[] bytes = BencodeSerializer.Serialize(value);

        Assert.AreEqual("d2:10i100e1:2i20ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a <see cref="ulong" />-keyed dictionary round-trips <see cref="ulong.MaxValue" /> as a key,
    /// confirming key stringification spans the full unsigned 64-bit range.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUInt64KeyedDictionary_ShouldRoundTripMaxValue()
    {
        var value = new Dictionary<ulong, int> { [ulong.MaxValue] = 1 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d20:18446744073709551615i1ee", Encoding.Latin1.GetString(bytes));

        Dictionary<ulong, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<ulong, int>>(bytes);
        Assert.AreEqual(1, roundTripped[ulong.MaxValue]);
    }

    /// <summary>
    /// Verifies that an enumeration-keyed dictionary uses the member names as keys, sorted canonically, and
    /// round-trips by case-insensitive name matching.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumKeyedDictionary_ShouldUseMemberNames()
    {
        var value = new Dictionary<Color, int> { [Color.Red] = 1, [Color.Blue] = 2 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d4:Bluei2e3:Redi1ee", Encoding.Latin1.GetString(bytes));

        Dictionary<Color, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<Color, int>>(bytes);
        Assert.AreEqual(1, roundTripped[Color.Red]);
        Assert.AreEqual(2, roundTripped[Color.Blue]);
    }

    /// <summary>
    /// Verifies that a combined-flags enumeration key uses the comma-separated member-name form produced by
    /// <see cref="Enum.ToString()" /> and round-trips back to the combined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFlagsEnumKeyCombined_ShouldUseCommaSeparatedNames()
    {
        var value = new Dictionary<Permissions, int> { [Permissions.Read | Permissions.Write] = 1 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d11:Read, Writei1ee", Encoding.Latin1.GetString(bytes));

        Dictionary<Permissions, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<Permissions, int>>(bytes);
        Assert.AreEqual(1, roundTripped[Permissions.Read | Permissions.Write]);
    }

    /// <summary>
    /// Verifies that a <see cref="Guid" />-keyed dictionary uses the 32-digit hyphenated ("D") format as the key and
    /// round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGuidKeyedDictionary_ShouldUseHyphenatedFormat()
    {
        var key = new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3");
        var value = new Dictionary<Guid, int> { [key] = 1 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d36:08314fa2-b1fe-4792-bcd1-6e62338ac7f3i1ee", Encoding.Latin1.GetString(bytes));

        Dictionary<Guid, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<Guid, int>>(bytes);
        Assert.AreEqual(1, roundTripped[key]);
    }

    /// <summary>
    /// Verifies that a <see cref="bool" />-keyed dictionary uses <c>True</c> and <c>False</c> as keys and round-trips
    /// case-insensitively.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBoolKeyedDictionary_ShouldUseTrueFalseText()
    {
        var value = new Dictionary<bool, int> { [true] = 1, [false] = 2 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d5:Falsei2e4:Truei1ee", Encoding.Latin1.GetString(bytes));

        Dictionary<bool, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<bool, int>>(bytes);
        Assert.AreEqual(1, roundTripped[true]);
        Assert.AreEqual(2, roundTripped[false]);
    }

    /// <summary>
    /// Verifies that a <see cref="char" />-keyed dictionary uses single-character keys and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCharKeyedDictionary_ShouldUseSingleCharacterKeys()
    {
        var value = new Dictionary<char, int> { ['b'] = 2, ['a'] = 1 };

        byte[] bytes = BencodeSerializer.Serialize(value);
        Assert.AreEqual("d1:ai1e1:bi2ee", Encoding.Latin1.GetString(bytes));

        Dictionary<char, int> roundTripped = BencodeSerializer.Deserialize<Dictionary<char, int>>(bytes);
        Assert.AreEqual(1, roundTripped['a']);
        Assert.AreEqual(2, roundTripped['b']);
    }

    /// <summary>
    /// Verifies that a non-string-keyed dictionary declared through <see cref="IDictionary{TKey, TValue}" />
    /// materializes to <see cref="Dictionary{TKey, TValue}" /> on read, matching the string-keyed interface behavior.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInt32KeyedIDictionary_ShouldMaterializeDictionary()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:1i2ee");

        IDictionary<int, int> roundTripped = BencodeSerializer.Deserialize<IDictionary<int, int>>(bytes);

        Assert.IsInstanceOfType<Dictionary<int, int>>(roundTripped);
        Assert.AreEqual(2, roundTripped[1]);
    }

    /// <summary>
    /// Verifies that deserializing a dictionary key whose text is not a valid integer into an <see cref="int" />-keyed
    /// dictionary throws <see cref="BencodeSerializationException" /> naming the offending key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerKeyTextInvalid_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:xi1ee");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<int, int>>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("'x'", StringComparison.Ordinal));
        Assert.IsNotNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that deserializing a <see cref="Guid" /> key written in a non-hyphenated format throws
    /// <see cref="BencodeSerializationException" />, because keys round-trip only through the exact "D" format.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGuidKeyTextNotHyphenatedFormat_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d38:{00000000-0000-0000-0000-000000000000}i1ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<Guid, int>>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a dictionary key whose text overflows the key type into an <see cref="int" />-keyed
    /// dictionary throws <see cref="BencodeSerializationException" /> naming the offending key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerKeyTextOverflows_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d20:99999999999999999999i1ee");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<int, int>>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("'99999999999999999999'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that deserializing a dictionary key that names no member of the enumeration into an enum-keyed
    /// dictionary throws <see cref="BencodeSerializationException" /> naming the offending key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumKeyTextUndefined_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d6:Purplei1ee");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<Color, int>>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("'Purple'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that deserializing a dictionary key that is not a valid boolean into a <see cref="bool" />-keyed
    /// dictionary throws <see cref="BencodeSerializationException" /> naming the offending key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenBoolKeyTextInvalid_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:maybei1ee");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<bool, int>>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("'maybe'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that deserializing a dictionary key that is not a single character into a <see cref="char" />-keyed
    /// dictionary throws <see cref="BencodeSerializationException" /> naming the offending key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCharKeyTextNotSingleCharacter_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d3:abci1ee");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<char, int>>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("'abc'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a dictionary keyed by an unsupported type (a plain object) is not mapped to a Bencode dictionary
    /// but falls through to the collection path, producing a Bencode list of two-entry key/value dictionaries.
    /// </summary>
    /// <remarks>
    /// Supported key types are <see cref="string" />, the integer family, enumerations, <see cref="Guid" />,
    /// <see cref="bool" />, and <see cref="char" />. Any other key type has no round-trippable byte-string form, so
    /// the dictionary binds through <see cref="IEnumerable{T}" /> over <see cref="KeyValuePair{TKey, TValue}" />
    /// instead.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenDictionaryKeyIsUnsupportedType_ShouldEmitListOfKeyValuePairs()
    {
        var value = new Dictionary<Item, int> { [new Item { Id = 1, Label = "a" }] = 2 };

        byte[] bytes = BencodeSerializer.Serialize(value);

        Assert.AreEqual("ld3:Keyd2:Idi1e5:Label1:ae5:Valuei2eee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing a Bencode dictionary into a dictionary keyed by an unsupported type throws
    /// <see cref="BencodeSerializationException" />, because such a type is bound through the collection path and a
    /// dictionary token is not a list.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDictionaryKeyIsUnsupportedType_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:1i2ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<Dictionary<Item, int>>(bytes);
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
        /// <value>The expected concrete type.</value>
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
