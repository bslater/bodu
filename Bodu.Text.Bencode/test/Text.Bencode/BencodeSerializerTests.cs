// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="BencodeSerializer" /> POCO-mapping surface: round-tripping, canonical key ordering,
/// attribute and naming-policy handling, custom converters, unsupported-type behavior, and null-member omission.
/// </summary>
[TestClass]
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a POCO with a string, an int, a long, a byte array, an int list, a nested POCO, and a
    /// string-to-int dictionary round-trips to canonical Bencode bytes and back to an equal value.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void SerializeDeserialize_WhenRichPoco_ShouldRoundTripToCanonicalBytesAndBack()
    {
        var original = new RichModel
        {
            Name = "torrent",
            Count = 7,
            Length = 9_000_000_000L,
            Payload = Encoding.ASCII.GetBytes("abc"),
            Numbers = [3, 1, 2],
            Nested = new NestedModel { Title = "inner" },
            Counts = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 },
        };

        byte[] bytes = BencodeSerializer.Serialize(original);

        // Keys sort bytewise ascending: Count, Counts, Length, Name, Nested, Numbers, Payload.
        const string Expected =
            "d" +
            "5:Counti7e" +
            "6:Countsd1:ai1e1:bi2ee" +
            "6:Lengthi9000000000e" +
            "4:Name7:torrent" +
            "6:Nestedd5:Title5:innere" +
            "7:Numbersli3ei1ei2ee" +
            "7:Payload3:abc" +
            "e";
        Assert.AreEqual(Expected, Encoding.Latin1.GetString(bytes));

        RichModel roundTripped = BencodeSerializer.Deserialize<RichModel>(bytes);
        Assert.AreEqual(original.Name, roundTripped.Name);
        Assert.AreEqual(original.Count, roundTripped.Count);
        Assert.AreEqual(original.Length, roundTripped.Length);
        CollectionAssert.AreEqual(original.Payload, roundTripped.Payload);
        CollectionAssert.AreEqual(original.Numbers, roundTripped.Numbers);
        Assert.AreEqual(original.Nested.Title, roundTripped.Nested.Title);
        CollectionAssert.AreEquivalent(original.Counts, roundTripped.Counts);
    }

    /// <summary>
    /// Verifies that members declared out of byte order are emitted in ascending bytewise key order so the output is
    /// canonical Bencode.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMembersDeclaredOutOfOrder_ShouldEmitSortedKeys()
    {
        var model = new OutOfOrderModel { Zebra = "z", Apple = "a", Mango = "m" };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d5:Apple1:a5:Mango1:m5:Zebra1:ze", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="BencodePropertyNameAttribute" /> overrides the dictionary key used on the wire.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyNameAttributePresent_ShouldUseOverriddenKey()
    {
        var model = new RenamedModel { Identifier = 5 };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d2:idi5ee", Encoding.Latin1.GetString(bytes));

        RenamedModel roundTripped = BencodeSerializer.Deserialize<RenamedModel>(bytes);
        Assert.AreEqual(5, roundTripped.Identifier);
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="BencodeIgnoreAttribute" /> is omitted from the output.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIgnored_ShouldOmitMember()
    {
        var model = new IgnoredModel { Kept = "k", Skipped = "s" };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d4:Kept1:ke", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a camel-case naming policy lowercases the first character of each dictionary key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCamelCaseNamingPolicy_ShouldLowerCaseKeys()
    {
        var options = new BencodeSerializerOptions { PropertyNamingPolicy = BencodeNamingPolicy.CamelCase };
        var model = new OutOfOrderModel { Zebra = "z", Apple = "a", Mango = "m" };

        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d5:apple1:a5:mango1:m5:zebra1:ze", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a custom <see cref="BencodeConverter{T}" /> registered on the options round-trips a type that is
    /// otherwise unsupported, exercising the converter extension point.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCustomBooleanConverterRegistered_ShouldRoundTripBoolean()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BooleanConverter());

        var model = new FlagModel { Enabled = true, Disabled = false };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d8:Disabledi0e7:Enabledi1ee", Encoding.Latin1.GetString(bytes));

        FlagModel roundTripped = BencodeSerializer.Deserialize<FlagModel>(bytes, options);
        Assert.IsTrue(roundTripped.Enabled);
        Assert.IsFalse(roundTripped.Disabled);
    }

    /// <summary>
    /// Verifies that serializing a type whose member is of an unsupported type throws
    /// <see cref="NotSupportedException" /> when no converter is registered for that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberTypeUnsupported_ShouldThrowNotSupportedException()
    {
        var model = new UnsupportedModel { Ratio = 1.5 };

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = BencodeSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// Verifies that a member whose value is <see langword="null" /> is omitted from the serialized output, because
    /// Bencode has no null token.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsNull_ShouldOmitMember()
    {
        var model = new NullableMemberModel { Present = "here", Absent = null };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d7:Present4:heree", Encoding.Latin1.GetString(bytes));

        NullableMemberModel roundTripped = BencodeSerializer.Deserialize<NullableMemberModel>(bytes);
        Assert.AreEqual("here", roundTripped.Present);
        Assert.IsNull(roundTripped.Absent);
    }

    /// <summary>
    /// A model exercising the full range of supported Bencode value kinds.
    /// </summary>
    private sealed class RichModel
    {
        /// <summary>
        /// Gets or sets the byte-string-mapped name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the 32-bit integer count.
        /// </summary>
        /// <returns>The count.</returns>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the 64-bit integer length.
        /// </summary>
        /// <returns>The length.</returns>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the raw byte-string payload.
        /// </summary>
        /// <returns>The payload bytes.</returns>
        public byte[] Payload { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of integers.
        /// </summary>
        /// <returns>The integer list.</returns>
        public List<int> Numbers { get; set; } = [];

        /// <summary>
        /// Gets or sets the nested model.
        /// </summary>
        /// <returns>The nested model.</returns>
        public NestedModel Nested { get; set; } = new();

        /// <summary>
        /// Gets or sets the string-keyed integer dictionary.
        /// </summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, int> Counts { get; set; } = [];
    }

    /// <summary>
    /// A nested model mapped to a Bencode dictionary.
    /// </summary>
    private sealed class NestedModel
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <returns>The title.</returns>
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose members are declared out of byte order to validate canonical key sorting.
    /// </summary>
    private sealed class OutOfOrderModel
    {
        /// <summary>
        /// Gets or sets the value whose key sorts last.
        /// </summary>
        /// <returns>The value.</returns>
        public string Zebra { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value whose key sorts first.
        /// </summary>
        /// <returns>The value.</returns>
        public string Apple { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value whose key sorts in the middle.
        /// </summary>
        /// <returns>The value.</returns>
        public string Mango { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a renamed member.
    /// </summary>
    private sealed class RenamedModel
    {
        /// <summary>
        /// Gets or sets the identifier, written under the wire name <c>id</c>.
        /// </summary>
        /// <returns>The identifier.</returns>
        [BencodePropertyName("id")]
        public int Identifier { get; set; }
    }

    /// <summary>
    /// A model with an ignored member.
    /// </summary>
    private sealed class IgnoredModel
    {
        /// <summary>
        /// Gets or sets the member retained in the output.
        /// </summary>
        /// <returns>The retained value.</returns>
        public string Kept { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the member excluded from the output.
        /// </summary>
        /// <returns>The excluded value.</returns>
        [BencodeIgnore]
        public string Skipped { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with Boolean members served by a custom converter.
    /// </summary>
    private sealed class FlagModel
    {
        /// <summary>
        /// Gets or sets the enabled flag.
        /// </summary>
        /// <returns>The enabled flag.</returns>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the disabled flag.
        /// </summary>
        /// <returns>The disabled flag.</returns>
        public bool Disabled { get; set; }
    }

    /// <summary>
    /// A model whose member type Bencode cannot represent without a custom converter.
    /// </summary>
    private sealed class UnsupportedModel
    {
        /// <summary>
        /// Gets or sets the floating-point ratio, for which no built-in converter exists.
        /// </summary>
        /// <returns>The ratio.</returns>
        public double Ratio { get; set; }
    }

    /// <summary>
    /// A model with a member that may be <see langword="null" />.
    /// </summary>
    private sealed class NullableMemberModel
    {
        /// <summary>
        /// Gets or sets the present member.
        /// </summary>
        /// <returns>The present value.</returns>
        public string? Present { get; set; }

        /// <summary>
        /// Gets or sets the absent member, omitted when <see langword="null" />.
        /// </summary>
        /// <returns>The absent value, or <see langword="null" />.</returns>
        public string? Absent { get; set; }
    }

    /// <summary>
    /// A custom converter mapping <see cref="bool" /> to and from a Bencode integer (<c>i1e</c> / <c>i0e</c>), proving
    /// the converter extension point and that <see cref="bool" /> is otherwise unsupported.
    /// </summary>
    private sealed class BooleanConverter
        : BencodeConverter<bool>
    {
        /// <inheritdoc />
        public override bool Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            reader.GetInt64() != 0;

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, bool value, BencodeSerializerOptions options) =>
            writer.WriteInteger(value ? 1 : 0);
    }
}
