// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;
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
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the 32-bit integer count.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the 64-bit integer length.
        /// </summary>
        /// <value>The length.</value>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the raw byte-string payload.
        /// </summary>
        /// <value>The payload bytes.</value>
        public byte[] Payload { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of integers.
        /// </summary>
        /// <value>The integer list.</value>
        public List<int> Numbers { get; set; } = [];

        /// <summary>
        /// Gets or sets the nested model.
        /// </summary>
        /// <value>The nested model.</value>
        public NestedModel Nested { get; set; } = new();

        /// <summary>
        /// Gets or sets the string-keyed integer dictionary.
        /// </summary>
        /// <value>The dictionary.</value>
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
        /// <value>The title.</value>
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
        /// <value>The value.</value>
        public string Zebra { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value whose key sorts first.
        /// </summary>
        /// <value>The value.</value>
        public string Apple { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value whose key sorts in the middle.
        /// </summary>
        /// <value>The value.</value>
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
        /// <value>The identifier.</value>
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
        /// <value>The retained value.</value>
        public string Kept { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the member excluded from the output.
        /// </summary>
        /// <value>The excluded value.</value>
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
        /// <value>The enabled flag.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the disabled flag.
        /// </summary>
        /// <value>The disabled flag.</value>
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
        /// <value>The ratio.</value>
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
        /// <value>The present value.</value>
        public string? Present { get; set; }

        /// <summary>
        /// Gets or sets the absent member, omitted when <see langword="null" />.
        /// </summary>
        /// <value>The absent value, or <see langword="null" />.</value>
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

    /// <summary>
    /// Gets the integer round-trip cases spanning each fixed-width integer type at its minimum, maximum, zero, and a
    /// typical value.
    /// </summary>
    /// <returns>The integer round-trip rows.</returns>
    public static IEnumerable<object[]> IntegerCases()
    {
        yield return Row(new SByteModel { Value = sbyte.MinValue }, "d5:Valuei-128ee", "sbyte min");
        yield return Row(new SByteModel { Value = sbyte.MaxValue }, "d5:Valuei127ee", "sbyte max");
        yield return Row(new SByteModel { Value = 0 }, "d5:Valuei0ee", "sbyte zero");
        yield return Row(new ByteModel { Value = byte.MinValue }, "d5:Valuei0ee", "byte min");
        yield return Row(new ByteModel { Value = byte.MaxValue }, "d5:Valuei255ee", "byte max");
        yield return Row(new ByteModel { Value = 42 }, "d5:Valuei42ee", "byte typical");
        yield return Row(new ShortModel { Value = short.MinValue }, "d5:Valuei-32768ee", "short min");
        yield return Row(new ShortModel { Value = short.MaxValue }, "d5:Valuei32767ee", "short max");
        yield return Row(new ShortModel { Value = 0 }, "d5:Valuei0ee", "short zero");
        yield return Row(new UShortModel { Value = ushort.MaxValue }, "d5:Valuei65535ee", "ushort max");
        yield return Row(new UShortModel { Value = 0 }, "d5:Valuei0ee", "ushort zero");
        yield return Row(new IntModel { Value = int.MinValue }, "d5:Valuei-2147483648ee", "int min");
        yield return Row(new IntModel { Value = int.MaxValue }, "d5:Valuei2147483647ee", "int max");
        yield return Row(new IntModel { Value = 0 }, "d5:Valuei0ee", "int zero");
        yield return Row(new IntModel { Value = -7 }, "d5:Valuei-7ee", "int typical");
        yield return Row(new UIntModel { Value = uint.MaxValue }, "d5:Valuei4294967295ee", "uint max");
        yield return Row(new UIntModel { Value = 0 }, "d5:Valuei0ee", "uint zero");
        yield return Row(new LongModel { Value = long.MinValue }, "d5:Valuei-9223372036854775808ee", "long min");
        yield return Row(new LongModel { Value = long.MaxValue }, "d5:Valuei9223372036854775807ee", "long max");
        yield return Row(new LongModel { Value = 0 }, "d5:Valuei0ee", "long zero");
        yield return Row(new ULongModel { Value = 0 }, "d5:Valuei0ee", "ulong zero");
        yield return Row(new ULongModel { Value = (ulong)long.MaxValue }, "d5:Valuei9223372036854775807ee", "ulong int64-max");
        yield return Row(new ULongModel { Value = ulong.MaxValue }, "d5:Valuei18446744073709551615ee", "ulong max");

        static object[] Row(object model, string expected, string name) =>
            [new BinaryKat<object, string>(name, model, expected)];
    }

    /// <summary>
    /// Gets the unsupported-type scenarios: a model per type Bencode cannot natively represent without a converter.
    /// </summary>
    /// <returns>The unsupported-type rows.</returns>
    public static IEnumerable<object[]> UnsupportedTypeCases()
    {
        yield return Row<bool>("bool", "d5:Valuei1ee");
        yield return Row<double>("double", "d5:Value3:1.5e");
        yield return Row<float>("float", "d5:Value3:1.5e");
        yield return Row<decimal>("decimal", "d5:Value3:1.5e");
        yield return Row<char>("char", "d5:Value1:ae");
        yield return Row<Guid>("Guid", "d5:Value1:xe");
        yield return Row<Uri>("Uri", "d5:Value1:xe");
        yield return Row<DateTime>("DateTime", "d5:Value1:xe");
        yield return Row<DateTimeOffset>("DateTimeOffset", "d5:Value1:xe");
        yield return Row<TimeSpan>("TimeSpan", "d5:Value1:xe");

        // Each row captures strongly-typed serialize/deserialize actions over a model whose single member is of the
        // unsupported type, so the exception surfaces directly rather than wrapped by reflection.
        static object[] Row<T>(string name, string encoded)
        {
            byte[] document = Encoding.Latin1.GetBytes(encoded);
            return
            [
                new UnsupportedTypeKat(
                    name,
                    () => _ = BencodeSerializer.Serialize(new SingleValueModel<T>()),
                    () => _ = BencodeSerializer.Deserialize<SingleValueModel<T>>(document)),
            ];
        }
    }

    /// <summary>
    /// Serializes a boxed model by its runtime type, so a single data-driven test can cover many model types.
    /// </summary>
    /// <param name="model">The model to serialize.</param>
    /// <returns>The Bencode encoding of <paramref name="model" />.</returns>
    private static byte[] SerializeBoxed(object model)
    {
        MethodInfo method = typeof(BencodeSerializer)
            .GetMethods()
            .First(m => m.Name == nameof(BencodeSerializer.Serialize) && m.IsGenericMethod && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType.IsGenericParameter)
            .MakeGenericMethod(model.GetType());
        return (byte[])method.Invoke(null, [model, null])!;
    }

    /// <summary>
    /// Deserializes a model of the specified runtime type from Bencode bytes.
    /// </summary>
    /// <param name="type">The model type.</param>
    /// <param name="bytes">The Bencode bytes to read.</param>
    /// <returns>The deserialized model.</returns>
    private static object DeserializeBoxed(Type type, byte[] bytes)
    {
        MethodInfo method = typeof(BencodeSerializer)
            .GetMethods()
            .First(m => m.Name == nameof(BencodeSerializer.Deserialize) && m.IsGenericMethod && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(byte[]))
            .MakeGenericMethod(type);
        return method.Invoke(null, [bytes, null])!;
    }

    /// <summary>
    /// A model with a single <see cref="string" /> member.
    /// </summary>
    private sealed class StringModel
    {
        /// <summary>
        /// Gets or sets the string value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a single <see cref="byte" /> array member.
    /// </summary>
    private sealed class BytesModel
    {
        /// <summary>
        /// Gets or sets the byte-array value.
        /// </summary>
        /// <value>The value.</value>
        public byte[] Value { get; set; } = [];
    }

    /// <summary>
    /// A model with a single <see cref="sbyte" /> member.
    /// </summary>
    private sealed class SByteModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public sbyte Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="byte" /> member.
    /// </summary>
    private sealed class ByteModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public byte Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="short" /> member.
    /// </summary>
    private sealed class ShortModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public short Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="ushort" /> member.
    /// </summary>
    private sealed class UShortModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public ushort Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="int" /> member.
    /// </summary>
    private sealed class IntModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="uint" /> member.
    /// </summary>
    private sealed class UIntModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public uint Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="long" /> member.
    /// </summary>
    private sealed class LongModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public long Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="ulong" /> member.
    /// </summary>
    private sealed class ULongModel
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public ulong Value { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="double" /> member, served only by a user converter.
    /// </summary>
    private sealed class RatioModel
    {
        /// <summary>
        /// Gets or sets the ratio.
        /// </summary>
        /// <value>The ratio.</value>
        public double Ratio { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="Color" /> enumeration value.
    /// </summary>
    private sealed class ColorModel
    {
        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color Color { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="Permissions" /> flags enumeration value.
    /// </summary>
    private sealed class PermissionModel
    {
        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        /// <value>The permissions.</value>
        public Permissions Permissions { get; set; }
    }

    /// <summary>
    /// A simple enumeration whose members map to their byte-string names.
    /// </summary>
    private enum Color
    {
        /// <summary>The red member.</summary>
        Red = 0,

        /// <summary>The green member.</summary>
        Green = 1,

        /// <summary>The blue member.</summary>
        Blue = 2,
    }

    /// <summary>
    /// A flags enumeration used to exercise combined-flag serialization.
    /// </summary>
    [Flags]
    private enum Permissions
    {
        /// <summary>No permission.</summary>
        None = 0,

        /// <summary>The read permission.</summary>
        Read = 1,

        /// <summary>The write permission.</summary>
        Write = 2,

        /// <summary>The execute permission.</summary>
        Execute = 4,
    }

    /// <summary>
    /// A custom converter mapping <see cref="double" /> to and from a Bencode byte string using the invariant culture.
    /// </summary>
    private sealed class DoubleConverter
        : BencodeConverter<double>
    {
        /// <inheritdoc />
        public override double Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            double.Parse(reader.GetString(), System.Globalization.CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, double value, BencodeSerializerOptions options) =>
            writer.WriteString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// A known-answer row describing a type Bencode cannot natively represent, carrying strongly-typed actions that
    /// serialize and deserialize a model whose single member is of that type.
    /// </summary>
    public sealed class UnsupportedTypeKat
        : IKat
    {
        /// <summary>
        /// The action that serializes a model whose member is of the unsupported type.
        /// </summary>
        private readonly Action _serialize;

        /// <summary>
        /// The action that deserializes a model whose member is of the unsupported type.
        /// </summary>
        private readonly Action _deserialize;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedTypeKat" /> class.
        /// </summary>
        /// <param name="name">The scenario label.</param>
        /// <param name="serialize">The action that serializes a model whose member is of the unsupported type.</param>
        /// <param name="deserialize">
        /// The action that deserializes a model whose member is of the unsupported type.
        /// </param>
        public UnsupportedTypeKat(string name, Action serialize, Action deserialize)
        {
            Name = name;
            _serialize = serialize;
            _deserialize = deserialize;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Serializes a default-valued model whose member is of the row's unsupported type.
        /// </summary>
        public void Serialize() =>
            _serialize();

        /// <summary>
        /// Deserializes a sample document into a model whose member is of the row's unsupported type.
        /// </summary>
        public void Deserialize() =>
            _deserialize();
    }

    /// <summary>
    /// A model with a single member of an arbitrary type, used to exercise unsupported-type behavior generically.
    /// </summary>
    /// <typeparam name="T">The member type.</typeparam>
    public sealed class SingleValueModel<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public T? Value { get; set; }
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

}
