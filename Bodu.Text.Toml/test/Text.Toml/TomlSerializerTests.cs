// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="TomlSerializer" /> POCO-mapping surface: round-tripping the full range of TOML value kinds,
/// canonical text output, naming-policy and attribute handling, custom converters, the root-must-be-a-table rule, and
/// null-member omission.
/// </summary>
[TestClass]
public partial class TomlSerializerTests
{
    /// <summary>
    /// The UTF-8 encoding used to decode serializer output for assertions; it omits a byte-order mark.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Verifies that a POCO exercising a string, an int, a long, a double, a bool, the four date-time types, an int
    /// array, a nested object, and a string-keyed dictionary round-trips through TOML to an equal value.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void SerializeDeserialize_WhenRichPoco_ShouldRoundTripToEqualValue()
    {
        var original = new RichModel
        {
            Name = "server",
            Count = 7,
            Length = 9_000_000_000L,
            Ratio = 1.5,
            Enabled = true,
            Instant = new DateTimeOffset(2026, 6, 10, 9, 30, 0, TimeSpan.Zero),
            Local = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Unspecified),
            Day = new DateOnly(2026, 6, 10),
            Time = new TimeOnly(9, 30, 0),
            Numbers = [3, 1, 2],
            Nested = new NestedModel { Title = "inner" },
            Counts = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 },
        };

        string text = TomlSerializer.Serialize(original);
        var roundTripped = TomlSerializer.Deserialize<RichModel>(text);

        Assert.AreEqual(original.Name, roundTripped.Name);
        Assert.AreEqual(original.Count, roundTripped.Count);
        Assert.AreEqual(original.Length, roundTripped.Length);
        Assert.AreEqual(original.Ratio, roundTripped.Ratio);
        Assert.AreEqual(original.Enabled, roundTripped.Enabled);
        Assert.AreEqual(original.Instant, roundTripped.Instant);
        Assert.AreEqual(original.Local, roundTripped.Local);
        Assert.AreEqual(original.Day, roundTripped.Day);
        Assert.AreEqual(original.Time, roundTripped.Time);
        CollectionAssert.AreEqual(original.Numbers, roundTripped.Numbers);
        Assert.AreEqual(original.Nested.Title, roundTripped.Nested.Title);
        CollectionAssert.AreEquivalent(original.Counts, roundTripped.Counts);
    }

    /// <summary>
    /// Verifies that a small POCO is serialized to the expected canonical TOML text, with scalars and arrays emitted as
    /// key/value lines in document order followed by the nested table as a <c>[header]</c> section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenSmallPoco_ShouldEmitExpectedCanonicalText()
    {
        var model = new SmallModel
        {
            Name = "x",
            Port = 8080,
            Nested = new NestedModel { Title = "inner" },
        };

        string text = TomlSerializer.Serialize(model);

        string expected =
            "Name = \"x\"\n" +
            "Port = 8080\n" +
            "\n" +
            "[Nested]\n" +
            "Title = \"inner\"\n";
        Assert.AreEqual(expected, text);
    }

    /// <summary>
    /// Verifies that a camel-case naming policy lowercases the first character of each table key, and that the result
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCamelCaseNamingPolicy_ShouldLowerCaseKeys()
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.CamelCase };
        var model = new PascalModel { FirstName = "a", LastName = "b" };

        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("firstName = \"a\"\nlastName = \"b\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<PascalModel>(text, options);
        Assert.AreEqual("a", roundTripped.FirstName);
        Assert.AreEqual("b", roundTripped.LastName);
    }

    /// <summary>
    /// Verifies that <see cref="Serialization.TomlPropertyNameAttribute" /> overrides the table key used on the wire.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyNameAttributePresent_ShouldUseOverriddenKey()
    {
        var model = new RenamedModel { Identifier = 5 };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("id = 5\n", text);

        var roundTripped = TomlSerializer.Deserialize<RenamedModel>(text);
        Assert.AreEqual(5, roundTripped.Identifier);
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="Serialization.TomlIgnoreAttribute" /> is omitted from the
    /// output.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIgnored_ShouldOmitMember()
    {
        var model = new IgnoredModel { Kept = "k", Skipped = "s" };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Kept = \"k\"\n", text);
    }

    /// <summary>
    /// Verifies that a custom <see cref="TomlConverter{T}" /> registered on the options round-trips a type that has no
    /// built-in converter, exercising the converter extension point.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCustomDecimalConverterRegistered_ShouldRoundTripDecimal()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new DecimalAsStringConverter());

        var model = new PriceModel { Amount = 19.95m };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Amount = \"19.95\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<PriceModel>(text, options);
        Assert.AreEqual(19.95m, roundTripped.Amount);
    }

    /// <summary>
    /// Verifies that serializing a top-level scalar throws <see cref="TomlSerializationException" /> because a TOML
    /// document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsScalar_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(42);
        });
    }

    /// <summary>
    /// Verifies that serializing a top-level array throws <see cref="TomlSerializationException" /> because a TOML
    /// document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsArray_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(new[] { 1, 2, 3 });
        });
    }

    /// <summary>
    /// Verifies that a string-keyed dictionary maps to a table at the document root and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRootIsDictionary_ShouldRoundTrip()
    {
        var model = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        string text = TomlSerializer.Serialize(model);
        var roundTripped = TomlSerializer.Deserialize<Dictionary<string, int>>(text);

        CollectionAssert.AreEquivalent(model, roundTripped);
    }

    /// <summary>
    /// Verifies that a byte array is written by default as a TOML array of integers and read back into an equal array.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayDefaultHandling_ShouldUseIntegerArray()
    {
        var model = new PayloadModel { Payload = [1, 2, 255] };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Payload = [1, 2, 255]\n", text);

        var roundTripped = TomlSerializer.Deserialize<PayloadModel>(text);
        CollectionAssert.AreEqual(model.Payload, roundTripped.Payload);
    }

    /// <summary>
    /// Verifies that a byte array is written as a Base64 basic string when
    /// <see cref="TomlSerializerOptions.ByteArrayHandling" /> selects
    /// <see cref="TomlByteArrayHandling.Base64String" />, and read back into an equal array.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayBase64Handling_ShouldUseBase64String()
    {
        var options = new TomlSerializerOptions { ByteArrayHandling = TomlByteArrayHandling.Base64String };
        var model = new PayloadModel { Payload = Encoding.ASCII.GetBytes("abc") };

        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Payload = \"YWJj\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<PayloadModel>(text, options);
        CollectionAssert.AreEqual(model.Payload, roundTripped.Payload);
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with <see cref="DateTimeKind.Utc" /> is written as a TOML offset
    /// date-time, mirroring the kind-based selection of the writer.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDateTimeIsUtc_ShouldWriteOffsetDateTime()
    {
        var model = new TimestampModel { When = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Utc) };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("When = 2026-06-10T09:30:00Z\n", text);
    }

    /// <summary>
    /// Verifies that serializing a value to a buffer writer and deserializing from the resulting UTF-8 span yields an
    /// equal value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBufferWriterAndSpan_ShouldRoundTrip()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var model = new SmallModel { Name = "x", Port = 1, Nested = new NestedModel { Title = "t" } };

        TomlSerializer.Serialize(buffer, model);
        var roundTripped = TomlSerializer.Deserialize<SmallModel>(buffer.WrittenSpan);

        Assert.AreEqual("x", roundTripped.Name);
        Assert.AreEqual(1, roundTripped.Port);
        Assert.AreEqual("t", roundTripped.Nested.Title);
    }

    /// <summary>
    /// Verifies that a member whose value is <see langword="null" /> is omitted from the output, because TOML has no
    /// null, and that the absent member round-trips as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsNull_ShouldOmitMember()
    {
        var model = new NullableMemberModel { Present = "here", Absent = null };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Present = \"here\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<NullableMemberModel>(text);
        Assert.AreEqual("here", roundTripped.Present);
        Assert.IsNull(roundTripped.Absent);
    }

    /// <summary>
    /// A model exercising the full range of supported TOML value kinds.
    /// </summary>
    private sealed class RichModel
    {
        /// <summary>Gets or sets the string name.</summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the 32-bit integer count.</summary>
        /// <returns>The count.</returns>
        public int Count { get; set; }

        /// <summary>Gets or sets the 64-bit integer length.</summary>
        /// <returns>The length.</returns>
        public long Length { get; set; }

        /// <summary>Gets or sets the floating-point ratio.</summary>
        /// <returns>The ratio.</returns>
        public double Ratio { get; set; }

        /// <summary>Gets or sets the Boolean flag.</summary>
        /// <returns>The flag.</returns>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the offset date-time.</summary>
        /// <returns>The instant.</returns>
        public DateTimeOffset Instant { get; set; }

        /// <summary>Gets or sets the local date-time.</summary>
        /// <returns>The local date-time.</returns>
        public DateTime Local { get; set; }

        /// <summary>Gets or sets the local date.</summary>
        /// <returns>The day.</returns>
        public DateOnly Day { get; set; }

        /// <summary>Gets or sets the local time.</summary>
        /// <returns>The time.</returns>
        public TimeOnly Time { get; set; }

        /// <summary>Gets or sets the list of integers.</summary>
        /// <returns>The integer list.</returns>
        public List<int> Numbers { get; set; } = [];

        /// <summary>Gets or sets the nested model.</summary>
        /// <returns>The nested model.</returns>
        public NestedModel Nested { get; set; } = new();

        /// <summary>Gets or sets the string-keyed integer dictionary.</summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, int> Counts { get; set; } = [];
    }

    /// <summary>
    /// A nested model mapped to a TOML table.
    /// </summary>
    private sealed class NestedModel
    {
        /// <summary>Gets or sets the title.</summary>
        /// <returns>The title.</returns>
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// A small model used to assert canonical text output.
    /// </summary>
    private sealed class SmallModel
    {
        /// <summary>Gets or sets the name.</summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the port.</summary>
        /// <returns>The port.</returns>
        public int Port { get; set; }

        /// <summary>Gets or sets the nested model.</summary>
        /// <returns>The nested model.</returns>
        public NestedModel Nested { get; set; } = new();
    }

    /// <summary>
    /// A model whose members are Pascal-cased, used to validate naming policies.
    /// </summary>
    private sealed class PascalModel
    {
        /// <summary>Gets or sets the first name.</summary>
        /// <returns>The first name.</returns>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Gets or sets the last name.</summary>
        /// <returns>The last name.</returns>
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a renamed member.
    /// </summary>
    private sealed class RenamedModel
    {
        /// <summary>Gets or sets the identifier, written under the wire name <c>id</c>.</summary>
        /// <returns>The identifier.</returns>
        [TomlPropertyName("id")]
        public int Identifier { get; set; }
    }

    /// <summary>
    /// A model with an ignored member.
    /// </summary>
    private sealed class IgnoredModel
    {
        /// <summary>Gets or sets the member retained in the output.</summary>
        /// <returns>The retained value.</returns>
        public string Kept { get; set; } = string.Empty;

        /// <summary>Gets or sets the member excluded from the output.</summary>
        /// <returns>The excluded value.</returns>
        [TomlIgnore]
        public string Skipped { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a <see cref="decimal" /> member served by a custom converter.
    /// </summary>
    private sealed class PriceModel
    {
        /// <summary>Gets or sets the price amount.</summary>
        /// <returns>The amount.</returns>
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// A model with a byte-array member.
    /// </summary>
    private sealed class PayloadModel
    {
        /// <summary>Gets or sets the payload bytes.</summary>
        /// <returns>The payload.</returns>
        public byte[] Payload { get; set; } = [];
    }

    /// <summary>
    /// A model with a single <see cref="DateTime" /> member.
    /// </summary>
    private sealed class TimestampModel
    {
        /// <summary>Gets or sets the timestamp.</summary>
        /// <returns>The timestamp.</returns>
        public DateTime When { get; set; }
    }

    /// <summary>
    /// A model with a member that may be <see langword="null" />.
    /// </summary>
    private sealed class NullableMemberModel
    {
        /// <summary>Gets or sets the present member.</summary>
        /// <returns>The present value.</returns>
        public string? Present { get; set; }

        /// <summary>Gets or sets the absent member, omitted when <see langword="null" />.</summary>
        /// <returns>The absent value, or <see langword="null" />.</returns>
        public string? Absent { get; set; }
    }

    /// <summary>
    /// A custom converter mapping <see cref="decimal" /> to and from a TOML string, proving the converter extension
    /// point and that <see cref="decimal" /> is otherwise unsupported.
    /// </summary>
    private sealed class DecimalAsStringConverter
        : TomlConverter<decimal>
    {
        /// <inheritdoc />
        public override decimal Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            decimal.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, decimal value, TomlSerializerOptions options) =>
            writer.WriteString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
