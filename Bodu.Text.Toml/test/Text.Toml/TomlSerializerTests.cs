// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Document;
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
        RichModel roundTripped = TomlSerializer.Deserialize<RichModel>(text);

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
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.CamelCase };
        var model = new PascalModel { FirstName = "a", LastName = "b" };

        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("firstName = \"a\"\nlastName = \"b\"\n", text);

        PascalModel roundTripped = TomlSerializer.Deserialize<PascalModel>(text, options);
        Assert.AreEqual("a", roundTripped.FirstName);
        Assert.AreEqual("b", roundTripped.LastName);
    }

    /// <summary>
    /// Verifies that <see cref="PropertyNameAttribute" /> overrides the table key used on the wire.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyNameAttributePresent_ShouldUseOverriddenKey()
    {
        var model = new RenamedModel { Identifier = 5 };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("id = 5\n", text);

        RenamedModel roundTripped = TomlSerializer.Deserialize<RenamedModel>(text);
        Assert.AreEqual(5, roundTripped.Identifier);
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="IgnoreAttribute" /> is omitted from the
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

        PriceModel roundTripped = TomlSerializer.Deserialize<PriceModel>(text, options);
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
        Dictionary<string, int> roundTripped = TomlSerializer.Deserialize<Dictionary<string, int>>(text);

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

        PayloadModel roundTripped = TomlSerializer.Deserialize<PayloadModel>(text);
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

        PayloadModel roundTripped = TomlSerializer.Deserialize<PayloadModel>(text, options);
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
        SmallModel roundTripped = TomlSerializer.Deserialize<SmallModel>(buffer.WrittenSpan);

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

        NullableMemberModel roundTripped = TomlSerializer.Deserialize<NullableMemberModel>(text);
        Assert.AreEqual("here", roundTripped.Present);
        Assert.IsNull(roundTripped.Absent);
    }

    /// <summary>
    /// A model exercising the full range of supported TOML value kinds.
    /// </summary>
    private sealed class RichModel
    {
        /// <summary>Gets or sets the string name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the 32-bit integer count.</summary>
        /// <value>The count.</value>
        public int Count { get; set; }

        /// <summary>Gets or sets the 64-bit integer length.</summary>
        /// <value>The length.</value>
        public long Length { get; set; }

        /// <summary>Gets or sets the floating-point ratio.</summary>
        /// <value>The ratio.</value>
        public double Ratio { get; set; }

        /// <summary>Gets or sets the Boolean flag.</summary>
        /// <value>The flag.</value>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the offset date-time.</summary>
        /// <value>The instant.</value>
        public DateTimeOffset Instant { get; set; }

        /// <summary>Gets or sets the local date-time.</summary>
        /// <value>The local date-time.</value>
        public DateTime Local { get; set; }

        /// <summary>Gets or sets the local date.</summary>
        /// <value>The day.</value>
        public DateOnly Day { get; set; }

        /// <summary>Gets or sets the local time.</summary>
        /// <value>The time.</value>
        public TimeOnly Time { get; set; }

        /// <summary>Gets or sets the list of integers.</summary>
        /// <value>The integer list.</value>
        public List<int> Numbers { get; set; } = [];

        /// <summary>Gets or sets the nested model.</summary>
        /// <value>The nested model.</value>
        public NestedModel Nested { get; set; } = new();

        /// <summary>Gets or sets the string-keyed integer dictionary.</summary>
        /// <value>The dictionary.</value>
        public Dictionary<string, int> Counts { get; set; } = [];
    }

    /// <summary>
    /// A nested model mapped to a TOML table.
    /// </summary>
    private sealed class NestedModel
    {
        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// A small model used to assert canonical text output.
    /// </summary>
    private sealed class SmallModel
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the port.</summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        /// <summary>Gets or sets the nested model.</summary>
        /// <value>The nested model.</value>
        public NestedModel Nested { get; set; } = new();
    }

    /// <summary>
    /// A model whose members are Pascal-cased, used to validate naming policies.
    /// </summary>
    private sealed class PascalModel
    {
        /// <summary>Gets or sets the first name.</summary>
        /// <value>The first name.</value>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Gets or sets the last name.</summary>
        /// <value>The last name.</value>
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a renamed member.
    /// </summary>
    private sealed class RenamedModel
    {
        /// <summary>Gets or sets the identifier, written under the wire name <c>id</c>.</summary>
        /// <value>The identifier.</value>
        [PropertyName("id")]
        public int Identifier { get; set; }
    }

    /// <summary>
    /// A model with an ignored member.
    /// </summary>
    private sealed class IgnoredModel
    {
        /// <summary>Gets or sets the member retained in the output.</summary>
        /// <value>The retained value.</value>
        public string Kept { get; set; } = string.Empty;

        /// <summary>Gets or sets the member excluded from the output.</summary>
        /// <value>The excluded value.</value>
        [Bodu.Text.Serialization.Ignore]
        public string Skipped { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a <see cref="decimal" /> member served by a custom converter.
    /// </summary>
    private sealed class PriceModel
    {
        /// <summary>Gets or sets the price amount.</summary>
        /// <value>The amount.</value>
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// A model with a byte-array member.
    /// </summary>
    private sealed class PayloadModel
    {
        /// <summary>Gets or sets the payload bytes.</summary>
        /// <value>The payload.</value>
        public byte[] Payload { get; set; } = [];
    }

    /// <summary>
    /// A model with a single <see cref="DateTime" /> member.
    /// </summary>
    private sealed class TimestampModel
    {
        /// <summary>Gets or sets the timestamp.</summary>
        /// <value>The timestamp.</value>
        public DateTime When { get; set; }
    }

    /// <summary>
    /// A model with a member that may be <see langword="null" />.
    /// </summary>
    private sealed class NullableMemberModel
    {
        /// <summary>Gets or sets the present member.</summary>
        /// <value>The present value.</value>
        public string? Present { get; set; }

        /// <summary>Gets or sets the absent member, omitted when <see langword="null" />.</summary>
        /// <value>The absent value, or <see langword="null" />.</value>
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
    /// <summary>
    /// Provides the canonical-text known-answer rows for the integer family: each row pins the exact key/value line a
    /// boxed integer value serializes to.
    /// </summary>
    /// <returns>The sequence of single-element argument arrays, each carrying one integer canonical-text row.</returns>
    public static IEnumerable<object[]> IntegerCanonicalRows()
    {
        yield return [new IntCanon("int zero", () => Serialize(0), "0")];
        yield return [new IntCanon("int max", () => Serialize(int.MaxValue), "2147483647")];
        yield return [new IntCanon("int min", () => Serialize(int.MinValue), "-2147483648")];
        yield return [new IntCanon("long max", () => Serialize(long.MaxValue), "9223372036854775807")];
        yield return [new IntCanon("long min", () => Serialize(long.MinValue), "-9223372036854775808")];
        yield return [new IntCanon("byte max", () => Serialize((byte)255), "255")];
        yield return [new IntCanon("byte zero", () => Serialize((byte)0), "0")];
        yield return [new IntCanon("sbyte min", () => Serialize((sbyte)-128), "-128")];
        yield return [new IntCanon("short min", () => Serialize((short)-32768), "-32768")];
        yield return [new IntCanon("ushort max", () => Serialize(ushort.MaxValue), "65535")];
        yield return [new IntCanon("uint max", () => Serialize(uint.MaxValue), "4294967295")];
    }

    /// <summary>
    /// Provides the canonical-text known-answer rows for floats: each row pins the exact spelling a boxed
    /// <see cref="double" /> serializes to, including the special sentinels and the shortest round-trippable forms.
    /// </summary>
    /// <returns>The sequence of single-element argument arrays, each carrying one float canonical-text row.</returns>
    public static IEnumerable<object[]> FloatCanonicalRows()
    {
        yield return [new ValidKat<double, string>("float half", 1.5, "1.5")];
        yield return [new ValidKat<double, string>("float whole gets point", 1.0, "1.0")];
        yield return [new ValidKat<double, string>("float zero", 0.0, "0.0")];
        yield return [new ValidKat<double, string>("float negative zero", -0.0, "-0.0")];
        yield return [new ValidKat<double, string>("float large no exp", 1e10, "10000000000.0")];
        yield return [new ValidKat<double, string>("float small exp", 1e-10, "1E-10")];
        yield return [new ValidKat<double, string>("float avogadro", 6.022e23, "6.022E+23")];
        yield return [new ValidKat<double, string>("float pi", Math.PI, "3.141592653589793")];
        yield return [new ValidKat<double, string>("float nan", double.NaN, "nan")];
        yield return [new ValidKat<double, string>("float positive infinity", double.PositiveInfinity, "inf")];
        yield return [new ValidKat<double, string>("float negative infinity", double.NegativeInfinity, "-inf")];
        yield return [new ValidKat<double, string>("float max", double.MaxValue, "1.7976931348623157E+308")];
        yield return [new ValidKat<double, string>("float epsilon", double.Epsilon, "5E-324")];
    }

    /// <summary>
    /// Provides the canonical-text known-answer rows for strings: each row pins the exact basic-quoted form, exercising
    /// the TOML string escapes and the pass-through of printable non-ASCII characters.
    /// </summary>
    /// <returns>The sequence of single-element argument arrays, each carrying one string canonical-text row.</returns>
    public static IEnumerable<object[]> StringCanonicalRows()
    {
        yield return [new ValidKat<string, string>("string simple", "hello", "\"hello\"")];
        yield return [new ValidKat<string, string>("string empty", string.Empty, "\"\"")];
        yield return [new ValidKat<string, string>("string tab", "a\tb", "\"a\\tb\"")];
        yield return [new ValidKat<string, string>("string newline", "a\nb", "\"a\\nb\"")];
        yield return [new ValidKat<string, string>("string carriage return", "a\rb", "\"a\\rb\"")];
        yield return [new ValidKat<string, string>("string quote", "a\"b", "\"a\\\"b\"")];
        yield return [new ValidKat<string, string>("string backslash", "a\\b", "\"a\\\\b\"")];
        yield return [new ValidKat<string, string>("string backspace", "a\bb", "\"a\\bb\"")];
        yield return [new ValidKat<string, string>("string form feed", "a\fb", "\"a\\fb\"")];
        yield return [new ValidKat<string, string>("string nul", "a\0b", "\"a\\u0000b\"")];
        yield return [new ValidKat<string, string>("string delete", "ab", "\"a\\u007Fb\"")];
        yield return [new ValidKat<string, string>("string unit separator", "ab", "\"a\\u001Fb\"")];
        yield return [new ValidKat<string, string>("string accented passthrough", "café", "\"café\"")];
        yield return [new ValidKat<string, string>("string emoji passthrough", "\U0001F600", "\"\U0001F600\"")];
    }

    /// <summary>
    /// Serializes a boxed scalar value through a single-member table and returns the resulting TOML text.
    /// </summary>
    /// <typeparam name="T">The scalar type to box.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The TOML representation of a table whose sole <c>Value</c> member carries <paramref name="value" />.</returns>
    private static string Serialize<T>(T value) =>
        TomlSerializer.Serialize(new ValueModel<T> { Value = value });

    /// <summary>
    /// Serializes and then deserializes a boxed scalar value, returning the round-tripped value.
    /// </summary>
    /// <typeparam name="T">The scalar type to box.</typeparam>
    /// <param name="value">The value to round-trip.</param>
    /// <returns>The value read back from the serialized form.</returns>
    private static T RoundTrip<T>(T value) =>
        TomlSerializer.Deserialize<ValueModel<T>>(Serialize(value)).Value;

    /// <summary>
    /// Formats the expected canonical offset date-time text for a given offset, mirroring the writer's RFC 3339 output.
    /// </summary>
    /// <param name="value">The offset date-time to format.</param>
    /// <returns>The expected canonical TOML offset date-time text.</returns>
    private static string ExpectedOffsetText(DateTimeOffset value)
    {
        string body = value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        if (value.Offset == TimeSpan.Zero)
            return body + "Z";

        char sign = value.Offset < TimeSpan.Zero ? '-' : '+';
        return body + sign + value.Offset.Duration().ToString("hh':'mm", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A generic single-member model that boxes a scalar value under the key <c>Value</c>, used to exercise each value
    /// kind at a table member where TOML permits it.
    /// </summary>
    /// <typeparam name="T">The boxed value type.</typeparam>
    private sealed class ValueModel<T>
    {
        /// <summary>Gets or sets the boxed value.</summary>
        /// <value>The boxed value.</value>
        public T Value { get; set; } = default!;
    }

    // IntCanon carries a Func<string> Serialize delegate (not a plain Input value) so each row can exercise a
    // different strongly typed boxed-integer write path; this domain shape does not map to ValidKat<TInput, TExpected>,
    // so it stays a local record.

    /// <summary>
    /// A known-answer row pinning the canonical text an integer value serializes to, deferring the serialization so the
    /// boxed type stays strongly typed.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Serialize">A function that serializes the boxed integer to TOML text.</param>
    /// <param name="Expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    public sealed record IntCanon(string Name, Func<string> Serialize, string Expected) : IKat;

    /// <summary>
    /// A custom converter mapping <see cref="decimal" /> to and from a TOML basic string, supplying the native form TOML
    /// otherwise lacks.
    /// </summary>
    private sealed class DecimalStringConverter
        : TomlConverter<decimal>
    {
        /// <inheritdoc />
        public override decimal Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            decimal.Parse(reader.GetString(), CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, decimal value, TomlSerializerOptions options) =>
            writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.WriteTo" /> re-emits every value kind through a standalone writer,
    /// producing the writer's canonical text — a nested table surfaces as a <c>[T]</c> header section rather than the
    /// inline form it was parsed from.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenEveryValueKind_ShouldReEmitCanonicalText()
    {
        const string toml = "S = \"x\"\nI = 5\nF = 1.5\nB = true\nOdt = 2026-06-10T09:30:00Z\nLdt = 2026-06-10T09:30:00\nLd = 2026-06-10\nLt = 09:30:00\nA = [1, 2]\nT = { N = 1 }\n";
        const string expected = "S = \"x\"\nI = 5\nF = 1.5\nB = true\nOdt = 2026-06-10T09:30:00Z\nLdt = 2026-06-10T09:30:00\nLd = 2026-06-10\nLt = 09:30:00\nA = [1, 2]\n\n[T]\nN = 1\n";
        using var document = TomlDocument.Parse(toml);

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);
        document.RootElement.WriteTo(writer);

        Assert.AreEqual(expected, System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// A small model used by the stream overload tests.
    /// </summary>
    private sealed class StreamModel
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }

        /// <summary>Gets or sets the label.</summary>
        /// <value>The label.</value>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// A minimal table-shaped model used by the guard tests so the document root satisfies the root-must-be-table
    /// rule once parameter validation passes.
    /// </summary>
    private sealed class GuardModel
    {
        /// <summary>
        /// Gets or sets the single integer member.
        /// </summary>
        /// <value>The stored integer value.</value>
        public int Value { get; set; }
    }
    /// <summary>
    /// Provides the canonical-text known-answer rows for decimals under both handlings: each row pins the exact value
    /// text a decimal serializes to for a given <see cref="TomlDecimalHandling" />.
    /// </summary>
    /// <returns>The sequence of single-element argument arrays, each carrying one decimal canonical-text row.</returns>
    public static IEnumerable<object[]> DecimalCanonicalRows()
    {
        yield return [new DecimalCanon("float simple", 1.5m, TomlDecimalHandling.Float, "1.5")];
        yield return [new DecimalCanon("float whole gets point", 1m, TomlDecimalHandling.Float, "1.0")];
        yield return [new DecimalCanon("float negative", -19.95m, TomlDecimalHandling.Float, "-19.95")];
        yield return [new DecimalCanon("float zero", 0m, TomlDecimalHandling.Float, "0.0")];
        yield return [new DecimalCanon("string simple", 1.5m, TomlDecimalHandling.String, "\"1.5\"")];
        yield return [new DecimalCanon("string preserves scale", 1.50m, TomlDecimalHandling.String, "\"1.50\"")];
        yield return [new DecimalCanon("string negative", -19.95m, TomlDecimalHandling.String, "\"-19.95\"")];
        yield return [new DecimalCanon("string max", decimal.MaxValue, TomlDecimalHandling.String, "\"79228162514264337593543950335\"")];
        yield return [new DecimalCanon("string min", decimal.MinValue, TomlDecimalHandling.String, "\"-79228162514264337593543950335\"")];
        yield return [new DecimalCanon("string full precision", 0.1234567890123456789012345678m, TomlDecimalHandling.String, "\"0.1234567890123456789012345678\"")];
    }

    // DecimalCanon carries an extra TomlDecimalHandling Handling field (consumed by
    // Serialize_WhenDecimal_ShouldEmitCanonicalTextForHandling) beyond the input/expected pair; this domain shape does
    // not map to ValidKat<TInput, TExpected>, so it stays a local record.

    /// <summary>
    /// A known-answer row pinning the canonical value text a decimal serializes to under a specific handling.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Input">The decimal value to serialize.</param>
    /// <param name="Handling">The decimal handling the row selects.</param>
    /// <param name="Expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    public sealed record DecimalCanon(string Name, decimal Input, TomlDecimalHandling Handling, string Expected) : IKat;

}
