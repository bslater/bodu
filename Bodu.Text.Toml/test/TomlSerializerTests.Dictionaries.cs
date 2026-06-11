// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Dictionaries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the dictionary value model of <see cref="TomlSerializer" />: a dictionary maps to a TOML table preserving
/// insertion order and quoting non-bare keys, the dictionary interfaces materialize a concrete dictionary on read
/// while concrete types are preserved, dictionaries nest and may be empty, the supported non-string key types (the
/// integer family, enumerations, <see cref="Guid" />, <see cref="bool" />, and <see cref="char" />) stringify to table
/// keys, and a dictionary keyed by an unsupported type is treated as a sequence of key/value pairs rather than a
/// table.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a string-keyed dictionary member serializes to a TOML <c>[header]</c> table preserving insertion
    /// order and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringKeyedDictionaryMember_ShouldEmitTableAndRoundTrip()
    {
        var model = new DictionaryModel { Counts = new() { ["z"] = 1, ["a"] = 2, ["m"] = 3 } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Counts]\nz = 1\na = 2\nm = 3\n", text);

        var roundTripped = TomlSerializer.Deserialize<DictionaryModel>(text);
        Assert.AreEqual(3, roundTripped.Counts.Count);
        Assert.AreEqual(1, roundTripped.Counts["z"]);
        Assert.AreEqual(2, roundTripped.Counts["a"]);
        Assert.AreEqual(3, roundTripped.Counts["m"]);
    }

    /// <summary>
    /// Verifies that a dictionary key that is not a bare key is written as a basic-quoted key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryKeyNotBare_ShouldQuoteKey()
    {
        var model = new DictionaryModel { Counts = new() { ["a key"] = 1 } };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("[Counts]\n\"a key\" = 1\n", text);
    }

    /// <summary>
    /// Verifies that a string-keyed dictionary maps to a table at the document root and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringKeyedDictionaryAtRoot_ShouldRoundTrip()
    {
        var model = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("a = 1\nb = 2\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<string, int>>(text);
        CollectionAssert.AreEquivalent(model, roundTripped);
    }

    /// <summary>
    /// Verifies that an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />-typed member materializes a
    /// concrete <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIDictionaryMember_ShouldMaterializeDictionary()
    {
        var model = TomlSerializer.Deserialize<IDictionaryModel>("[Counts]\na = 1\n");

        Assert.IsInstanceOfType<Dictionary<string, int>>(model.Counts);
        Assert.AreEqual(1, model.Counts["a"]);
    }

    /// <summary>
    /// Verifies that a <see cref="System.Collections.Generic.SortedDictionary{TKey, TValue}" /> member is preserved as
    /// its concrete type on read and that its entries serialize in sorted key order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSortedDictionaryMember_ShouldPreserveTypeAndSortKeys()
    {
        var model = new SortedDictionaryModel { Counts = new() { ["b"] = 2, ["a"] = 1 } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Counts]\na = 1\nb = 2\n", text);

        var roundTripped = TomlSerializer.Deserialize<SortedDictionaryModel>(text);
        Assert.IsInstanceOfType<SortedDictionary<string, int>>(roundTripped.Counts);
        Assert.AreEqual(1, roundTripped.Counts["a"]);
        Assert.AreEqual(2, roundTripped.Counts["b"]);
    }

    /// <summary>
    /// Verifies that an empty dictionary member serializes to an empty <c>[header]</c> table and round-trips to a
    /// non-null empty dictionary.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEmptyDictionaryMember_ShouldEmitEmptyTableAndRoundTrip()
    {
        var model = new DictionaryModel { Counts = new() };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Counts]\n", text);

        var roundTripped = TomlSerializer.Deserialize<DictionaryModel>(text);
        Assert.IsNotNull(roundTripped.Counts);
        Assert.AreEqual(0, roundTripped.Counts.Count);
    }

    /// <summary>
    /// Verifies that a nested string-keyed dictionary member serializes to nested <c>[header]</c> tables and round-trips
    /// preserving the inner entries.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNestedDictionaryMember_ShouldEmitNestedTablesAndRoundTrip()
    {
        var model = new NestedDictionaryModel { Groups = new() { ["x"] = new() { ["a"] = 1 } } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Groups]\n\n[Groups.x]\na = 1\n", text);

        var roundTripped = TomlSerializer.Deserialize<NestedDictionaryModel>(text);
        Assert.AreEqual(1, roundTripped.Groups["x"]["a"]);
    }

    /// <summary>
    /// Verifies that a dictionary whose value type is a collection serializes the value as a TOML array on a key/value
    /// line and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDictionaryValueIsCollection_ShouldEmitArrayValueAndRoundTrip()
    {
        var model = new DictionaryOfListModel { Groups = new() { ["a"] = [1, 2] } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Groups]\na = [1, 2]\n", text);

        var roundTripped = TomlSerializer.Deserialize<DictionaryOfListModel>(text);
        CollectionAssert.AreEqual(new[] { 1, 2 }, roundTripped.Groups["a"]);
    }

    /// <summary>
    /// Verifies that a dictionary whose value is <see langword="null" /> omits that entry from the output, because TOML
    /// has no null token.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryValueIsNull_ShouldOmitEntry()
    {
        var model = new NullableValueDictionaryModel { Names = new() { ["a"] = "x", ["b"] = null } };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("[Names]\na = \"x\"\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="int" />-keyed dictionary member serializes to a TOML table whose keys are the
    /// invariant decimal text of the integers, written bare, and round-trips to an equivalent dictionary.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt32KeyedDictionaryMember_ShouldEmitTableAndRoundTrip()
    {
        var model = new IntKeyedDictionaryModel { Lookup = new() { [1] = "a" } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Lookup]\n1 = \"a\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<IntKeyedDictionaryModel>(text);
        Assert.AreEqual("a", roundTripped.Lookup[1]);
    }

    /// <summary>
    /// Verifies that stringified integer keys are written in the dictionary's insertion order rather than sorted, so
    /// the key <c>2</c> precedes the key <c>10</c> when inserted first — TOML output preserves document order, in
    /// contrast to the canonical bytewise key sorting of the Bencode serializer.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenInt32KeysStringify_ShouldPreserveInsertionOrder()
    {
        var model = new CountByIdModel { Counts = new() { [2] = 20, [10] = 100 } };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("[Counts]\n2 = 20\n10 = 100\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="int" />-keyed dictionary maps to a table at the document root and round-trips,
    /// because a dictionary with a supported key type satisfies the root-must-be-a-table rule.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt32KeyedDictionaryAtRoot_ShouldRoundTrip()
    {
        var model = new Dictionary<int, string> { [1] = "a" };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("1 = \"a\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<int, string>>(text);
        Assert.AreEqual("a", roundTripped[1]);
    }

    /// <summary>
    /// Verifies that a negative integer key is written as a bare key, because the bare-key grammar permits the
    /// <c>-</c> character, and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNegativeIntegerKey_ShouldEmitBareKey()
    {
        var model = new Dictionary<int, int> { [-5] = 1 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("-5 = 1\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<int, int>>(text);
        Assert.AreEqual(1, roundTripped[-5]);
    }

    /// <summary>
    /// Verifies that a <see cref="ulong" />-keyed dictionary round-trips <see cref="ulong.MaxValue" /> as a key,
    /// confirming key stringification spans the full unsigned 64-bit range even though TOML integer values are
    /// signed 64-bit.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUInt64KeyedDictionary_ShouldRoundTripMaxValue()
    {
        var model = new Dictionary<ulong, int> { [ulong.MaxValue] = 1 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("18446744073709551615 = 1\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<ulong, int>>(text);
        Assert.AreEqual(1, roundTripped[ulong.MaxValue]);
    }

    /// <summary>
    /// Verifies that an enumeration-keyed dictionary uses the member names as bare table keys, written in insertion
    /// order, and round-trips by case-insensitive name matching.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumKeyedDictionary_ShouldUseMemberNames()
    {
        var model = new Dictionary<Status, int> { [Status.Archived] = 1, [Status.Active] = 2 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("Archived = 1\nActive = 2\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<Status, int>>(text);
        Assert.AreEqual(1, roundTripped[Status.Archived]);
        Assert.AreEqual(2, roundTripped[Status.Active]);
    }

    /// <summary>
    /// Verifies that a combined-flags enumeration key uses the comma-separated member-name form produced by
    /// <see cref="Enum.ToString()" />, which the writer basic-quotes because it is not a bare key, and round-trips
    /// back to the combined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFlagsEnumKeyCombined_ShouldQuoteCommaSeparatedNames()
    {
        var model = new Dictionary<PermissionFlags, int> { [PermissionFlags.Read | PermissionFlags.Write] = 1 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("\"Read, Write\" = 1\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<PermissionFlags, int>>(text);
        Assert.AreEqual(1, roundTripped[PermissionFlags.Read | PermissionFlags.Write]);
    }

    /// <summary>
    /// Verifies that a <see cref="Guid" />-keyed dictionary uses the 32-digit hyphenated ("D") format as a bare table
    /// key — hexadecimal digits and hyphens all fall within the bare-key grammar — and round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGuidKeyedDictionary_ShouldUseBareHyphenatedKey()
    {
        var key = new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3");
        var model = new Dictionary<Guid, int> { [key] = 1 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("08314fa2-b1fe-4792-bcd1-6e62338ac7f3 = 1\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<Guid, int>>(text);
        Assert.AreEqual(1, roundTripped[key]);
    }

    /// <summary>
    /// Verifies that a <see cref="bool" />-keyed dictionary uses <c>True</c> and <c>False</c> as bare table keys and
    /// round-trips case-insensitively.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBoolKeyedDictionary_ShouldUseTrueFalseText()
    {
        var model = new Dictionary<bool, int> { [true] = 1, [false] = 2 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("True = 1\nFalse = 2\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<bool, int>>(text);
        Assert.AreEqual(1, roundTripped[true]);
        Assert.AreEqual(2, roundTripped[false]);
    }

    /// <summary>
    /// Verifies that a <see cref="char" />-keyed dictionary uses single-character keys — bare when the character falls
    /// within the bare-key grammar and basic-quoted otherwise — and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCharKeyedDictionary_ShouldQuoteNonBareKeys()
    {
        var model = new Dictionary<char, int> { ['a'] = 1, ['#'] = 2 };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("a = 1\n\"#\" = 2\n", text);

        var roundTripped = TomlSerializer.Deserialize<Dictionary<char, int>>(text);
        Assert.AreEqual(1, roundTripped['a']);
        Assert.AreEqual(2, roundTripped['#']);
    }

    /// <summary>
    /// Verifies that a nested dictionary keyed by integers serializes its stringified keys as nested table headers and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNestedInt32KeyedDictionary_ShouldEmitNestedTablesAndRoundTrip()
    {
        var model = new NestedIntKeyedDictionaryModel { Groups = new() { [1] = new() { [2] = 3 } } };

        string text = TomlSerializer.Serialize(model);
        Assert.AreEqual("[Groups]\n\n[Groups.1]\n2 = 3\n", text);

        var roundTripped = TomlSerializer.Deserialize<NestedIntKeyedDictionaryModel>(text);
        Assert.AreEqual(3, roundTripped.Groups[1][2]);
    }

    /// <summary>
    /// Verifies that a non-string-keyed dictionary declared through <see cref="IDictionary{TKey, TValue}" />
    /// materializes to <see cref="Dictionary{TKey, TValue}" /> on read, matching the string-keyed interface behavior.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenInt32KeyedIDictionary_ShouldMaterializeDictionary()
    {
        IDictionary<int, int> roundTripped = TomlSerializer.Deserialize<IDictionary<int, int>>("1 = 2\n");

        Assert.IsInstanceOfType<Dictionary<int, int>>(roundTripped);
        Assert.AreEqual(2, roundTripped[1]);
    }

    /// <summary>
    /// Verifies that deserializing a table key whose text is not a valid integer into an <see cref="int" />-keyed
    /// dictionary throws <see cref="TomlSerializationException" /> naming the offending key, with the parse failure
    /// preserved as the inner exception.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerKeyTextInvalid_ShouldThrowTomlSerializationException()
    {
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<Dictionary<int, int>>("x = 1\n");
        });

        Assert.IsTrue(ex.Message.Contains("'x'", StringComparison.Ordinal));
        Assert.IsNotNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that deserializing a table key that names no member of the enumeration into an enum-keyed dictionary
    /// throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumKeyTextUndefined_ShouldThrowTomlSerializationException()
    {
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<Dictionary<Status, int>>("Purple = 1\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Purple'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that deserializing a <see cref="Guid" /> key written in a non-hyphenated format throws
    /// <see cref="TomlSerializationException" />, because keys round-trip only through the exact "D" format.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGuidKeyTextNotHyphenatedFormat_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<Dictionary<Guid, int>>("00000000000000000000000000000000 = 1\n");
        });
    }

    /// <summary>
    /// Verifies that a dictionary member keyed by an unsupported type (here <see cref="double" />) is written as a
    /// TOML array of key/value tables rather than a single table, because such a key has no round-trippable table-key
    /// form and the dictionary is treated as a sequence of pairs.
    /// </summary>
    /// <remarks>
    /// Supported key types are <see cref="string" />, the integer family, enumerations, <see cref="Guid" />,
    /// <see cref="bool" />, and <see cref="char" />. Any other key type binds through
    /// <see cref="IEnumerable{T}" /> over <see cref="KeyValuePair{TKey, TValue}" /> instead.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenDictionaryKeyIsUnsupportedType_ShouldEmitArrayOfPairTables()
    {
        var model = new DoubleKeyedDictionaryModel { Lookup = new() { [1.5] = "a" } };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("[[Lookup]]\nKey = 1.5\nValue = \"a\"\n", text);
    }

    /// <summary>
    /// Verifies that serializing a top-level dictionary keyed by an unsupported type throws
    /// <see cref="TomlSerializationException" />, because the dictionary maps to an array rather than a table and a
    /// TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootDictionaryKeyIsUnsupportedType_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new Dictionary<double, string> { [1.5] = "a" });
        });
    }

    /// <summary>
    /// Verifies that deserializing a TOML table into a dictionary keyed by an unsupported type throws
    /// <see cref="TomlSerializationException" />, because such a type is bound through the collection path and a
    /// table is not an array.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDictionaryKeyIsUnsupportedType_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<Dictionary<double, string>>("a = \"b\"\n");
        });
    }

    /// <summary>
    /// A model with a string-keyed dictionary member.
    /// </summary>
    private sealed class DictionaryModel
    {
        /// <summary>Gets or sets the string-keyed integer dictionary.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, int> Counts { get; set; } = [];
    }

    /// <summary>
    /// A model with an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />-typed member.
    /// </summary>
    private sealed class IDictionaryModel
    {
        /// <summary>Gets or sets the string-keyed integer dictionary, typed as an interface.</summary>
        /// <returns>The dictionary.</returns>
        public IDictionary<string, int> Counts { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// A model with a <see cref="System.Collections.Generic.SortedDictionary{TKey, TValue}" /> member.
    /// </summary>
    private sealed class SortedDictionaryModel
    {
        /// <summary>Gets or sets the sorted string-keyed integer dictionary.</summary>
        /// <returns>The dictionary.</returns>
        public SortedDictionary<string, int> Counts { get; set; } = [];
    }

    /// <summary>
    /// A model with a nested string-keyed dictionary member.
    /// </summary>
    private sealed class NestedDictionaryModel
    {
        /// <summary>Gets or sets the nested dictionary of dictionaries.</summary>
        /// <returns>The nested dictionary.</returns>
        public Dictionary<string, Dictionary<string, int>> Groups { get; set; } = [];
    }

    /// <summary>
    /// A model with a dictionary whose value is a collection.
    /// </summary>
    private sealed class DictionaryOfListModel
    {
        /// <summary>Gets or sets the dictionary of integer lists.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, List<int>> Groups { get; set; } = [];
    }

    /// <summary>
    /// A model with a dictionary whose values may be <see langword="null" />.
    /// </summary>
    private sealed class NullableValueDictionaryModel
    {
        /// <summary>Gets or sets the dictionary of nullable names.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, string?> Names { get; set; } = [];
    }

    /// <summary>
    /// A model with an integer-keyed dictionary member, used to confirm the stringified-key table mapping.
    /// </summary>
    private sealed class IntKeyedDictionaryModel
    {
        /// <summary>Gets or sets the integer-keyed dictionary.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<int, string> Lookup { get; set; } = [];
    }

    /// <summary>
    /// A model with an integer-keyed count dictionary, used to confirm insertion-order key emission.
    /// </summary>
    private sealed class CountByIdModel
    {
        /// <summary>Gets or sets the integer-keyed counts.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<int, int> Counts { get; set; } = [];
    }

    /// <summary>
    /// A model with a nested integer-keyed dictionary member.
    /// </summary>
    private sealed class NestedIntKeyedDictionaryModel
    {
        /// <summary>Gets or sets the nested integer-keyed dictionary of dictionaries.</summary>
        /// <returns>The nested dictionary.</returns>
        public Dictionary<int, Dictionary<int, int>> Groups { get; set; } = [];
    }

    /// <summary>
    /// A model with a <see cref="double" />-keyed dictionary member, used to confirm the unsupported-key mapping to a
    /// sequence of pairs.
    /// </summary>
    private sealed class DoubleKeyedDictionaryModel
    {
        /// <summary>Gets or sets the double-keyed dictionary.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<double, string> Lookup { get; set; } = [];
    }
}
