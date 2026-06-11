// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Dictionaries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the dictionary value model of <see cref="TomlSerializer" />: a string-keyed dictionary maps to a TOML
/// table preserving insertion order and quoting non-bare keys, the dictionary interfaces materialize a concrete
/// dictionary on read while concrete types are preserved, dictionaries nest and may be empty, and a dictionary whose
/// key is not a string is treated as a sequence of key/value pairs rather than a table.
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
    /// Verifies that a dictionary whose key is not a string is written as a TOML array of key/value tables rather than a
    /// single table, because TOML keys are strings and the dictionary is treated as a sequence of pairs.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryKeyNotString_ShouldEmitArrayOfPairTables()
    {
        var model = new IntKeyedDictionaryModel { Lookup = new() { [1] = "a" } };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("[[Lookup]]\nKey = 1\nValue = \"a\"\n", text);
    }

    /// <summary>
    /// Verifies that serializing a top-level dictionary whose key is not a string throws
    /// <see cref="TomlSerializationException" />, because the non-string-keyed dictionary maps to an array rather than a
    /// table and a TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsNonStringKeyedDictionary_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new Dictionary<int, string> { [1] = "a" });
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
    /// A model with an integer-keyed dictionary member, used to confirm the non-string-key mapping to a sequence of
    /// pairs.
    /// </summary>
    private sealed class IntKeyedDictionaryModel
    {
        /// <summary>Gets or sets the integer-keyed dictionary.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<int, string> Lookup { get; set; } = [];
    }
}
