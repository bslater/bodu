// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Values.cs" company="Bodu Pty. Ltd.">
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
/// Verifies the native scalar value mapping of <see cref="BencodeSerializer" />: byte strings (<see cref="string" />
/// and <see cref="byte" /> arrays), the fixed-width integer family, enumerations, integer overflow handling, and the
/// boundary between Bencode's natively supported types and the types that require a user converter.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that an empty string round-trips and serializes to the empty Bencode byte string <c>0:</c>.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringIsEmpty_ShouldRoundTripToEmptyByteString()
    {
        var model = new StringModel { Value = string.Empty };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Value0:e", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<StringModel>(bytes);
        Assert.AreEqual(string.Empty, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that an ASCII string round-trips and serializes to a length-prefixed Bencode byte string.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringIsAscii_ShouldRoundTripToLengthPrefixedByteString()
    {
        var model = new StringModel { Value = "hello" };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Value5:helloe", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<StringModel>(bytes);
        Assert.AreEqual("hello", roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a string containing multibyte UTF-8 characters round-trips and is length-prefixed by its UTF-8
    /// byte count rather than its character count.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringIsMultibyteUtf8_ShouldLengthPrefixByByteCount()
    {
        // "héllo" — the 'é' is two UTF-8 bytes, so the byte length is six for five characters.
        var model = new StringModel { Value = "héllo" };

        byte[] bytes = BencodeSerializer.Serialize(model);

        // The length prefix is the UTF-8 byte count (6), not the character count (5).
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("d5:Value6:héllo" + "e"), bytes);

        var roundTripped = BencodeSerializer.Deserialize<StringModel>(bytes);
        Assert.AreEqual("héllo", roundTripped.Value);
    }

    /// <summary>
    /// Verifies that an empty <see cref="byte" /> array round-trips and serializes to the empty Bencode byte string.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayIsEmpty_ShouldRoundTripToEmptyByteString()
    {
        var model = new BytesModel { Value = [] };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Value0:e", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<BytesModel>(bytes);
        CollectionAssert.AreEqual(Array.Empty<byte>(), roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a <see cref="byte" /> array containing arbitrary binary bytes, including a zero byte and a high
    /// byte, round-trips losslessly through the Bencode byte string.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayIsBinary_ShouldRoundTripLosslessly()
    {
        byte[] payload = [0x00, 0x01, 0x7f, 0x80, 0xff];
        var model = new BytesModel { Value = payload };

        byte[] bytes = BencodeSerializer.Serialize(model);
        // Header "d5:Value5:" then the five raw payload bytes then the trailing 'e'.
        byte[] expected = [.. Encoding.Latin1.GetBytes("d5:Value5:"), 0x00, 0x01, 0x7f, 0x80, 0xff, (byte)'e'];
        CollectionAssert.AreEqual(expected, bytes);

        var roundTripped = BencodeSerializer.Deserialize<BytesModel>(bytes);
        CollectionAssert.AreEqual(payload, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that each fixed-width integer type serializes to the canonical <c>i…e</c> byte sequence at its minimum,
    /// maximum, zero, and a typical value, and round-trips back to the same value.
    /// </summary>
    /// <param name="name">The scenario label.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(IntegerCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenIntegerValue_ShouldRoundTripToCanonicalBytes(BinaryKat<object, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        // Each row carries the boxed model and its expected canonical encoding; the helper round-trips by runtime type.
        byte[] bytes = SerializeBoxed(kat.Input);
        Assert.AreEqual(kat.Expected, Encoding.Latin1.GetString(bytes));

        // Round-trip by re-serializing the deserialized model: equal bytes confirm the integer value survived intact
        // without comparing the distinct boxed model instances by reference.
        object roundTripped = DeserializeBoxed(kat.Input.GetType(), bytes);
        byte[] reserialized = SerializeBoxed(roundTripped);
        CollectionAssert.AreEqual(bytes, reserialized);
    }

    /// <summary>
    /// Verifies that a <see cref="ulong" /> value within the signed 64-bit range round-trips through the same wire
    /// form a <see cref="long" /> of equal value produces.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUInt64WithinInt64Range_ShouldRoundTrip()
    {
        var model = new ULongModel { Value = long.MaxValue };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Valuei9223372036854775807ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<ULongModel>(bytes);
        Assert.AreEqual((ulong)long.MaxValue, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that a <see cref="ulong" /> larger than <see cref="long.MaxValue" /> round-trips losslessly, because
    /// Bencode integers are arbitrary-precision in BEP 3 and the serializer reads and writes the full unsigned 64-bit
    /// range.
    /// </summary>
    /// <param name="name">The scenario label.</param>
    /// <param name="value">The unsigned value to round-trip.</param>
    /// <param name="encoded">The expected canonical Bencode document.</param>
    [TestMethod]
    [DataRow("ulong max", ulong.MaxValue, "d5:Valuei18446744073709551615ee")]
    [DataRow("long max plus one", 9223372036854775808UL, "d5:Valuei9223372036854775808ee")]
    public void SerializeDeserialize_WhenUInt64ExceedsInt64Range_ShouldRoundTrip(string name, ulong value, string encoded)
    {
        _ = name;
        var model = new ULongModel { Value = value };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual(encoded, Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<ULongModel>(bytes);
        Assert.AreEqual(value, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that deserializing a negative Bencode integer into a <see cref="ulong" /> member throws
    /// <see cref="BencodeSerializationException" />, matching the overflow contract of the other fixed-width integer
    /// types.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntegerIntoUInt64_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei-1ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<ULongModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a Bencode integer whose value overflows the target integer type throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    /// <param name="name">The scenario label.</param>
    /// <param name="encoded">The Bencode document to read.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("byte > 255", "d5:Valuei300ee")]
    [DataRow("byte negative", "d5:Valuei-1ee")]
    public void Deserialize_WhenIntegerOverflowsByte_ShouldThrowBencodeSerializationException(string name, string encoded)
    {
        _ = name;
        byte[] bytes = Encoding.Latin1.GetBytes(encoded);

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<ByteModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a Bencode integer beyond <see cref="long.MaxValue" /> into a signed
    /// <see cref="long" /> member throws <see cref="BencodeFormatException" />, because the value is readable only
    /// through the unsigned surface.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerExceedsInt64RangeIntoInt64_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei18446744073709551615ee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<LongModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that deserializing a Bencode integer literal beyond <see cref="ulong.MaxValue" /> throws
    /// <see cref="BencodeFormatException" />, surfacing at the reader before the converter is consulted.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerExceedsUInt64Range_ShouldThrowBencodeFormatException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei18446744073709551616ee");

        Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<ULongModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Int128" /> values within the signed 64-bit surface and <see cref="UInt128" /> values
    /// within the unsigned 64-bit surface round-trip to the canonical <c>i…e</c> form, including at the boundaries.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_When128BitWithin64BitSurface_ShouldRoundTrip()
    {
        byte[] signed = BencodeSerializer.Serialize(new SingleValueModel<Int128> { Value = long.MinValue });
        Assert.AreEqual("d5:Valuei-9223372036854775808ee", Encoding.Latin1.GetString(signed));
        Assert.AreEqual((Int128)long.MinValue, BencodeSerializer.Deserialize<SingleValueModel<Int128>>(signed).Value);

        byte[] unsigned = BencodeSerializer.Serialize(new SingleValueModel<UInt128> { Value = ulong.MaxValue });
        Assert.AreEqual("d5:Valuei18446744073709551615ee", Encoding.Latin1.GetString(unsigned));
        Assert.AreEqual((UInt128)ulong.MaxValue, BencodeSerializer.Deserialize<SingleValueModel<UInt128>>(unsigned).Value);
    }

    /// <summary>
    /// Verifies that serializing an <see cref="Int128" /> outside the signed 64-bit surface or a
    /// <see cref="UInt128" /> outside the unsigned 64-bit surface throws
    /// <see cref="BencodeSerializationException" /> carrying the checked-conversion <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_When128BitExceeds64BitSurface_ShouldThrowBencodeSerializationException()
    {
        var signedEx = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(new SingleValueModel<Int128> { Value = Int128.MaxValue });
        });

        Assert.IsNotNull(signedEx.InnerException);
        Assert.IsInstanceOfType<OverflowException>(signedEx.InnerException);

        var unsignedEx = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(new SingleValueModel<UInt128> { Value = (UInt128)ulong.MaxValue + 1 });
        });

        Assert.IsNotNull(unsignedEx.InnerException);
        Assert.IsInstanceOfType<OverflowException>(unsignedEx.InnerException);
    }

    /// <summary>
    /// Verifies that deserializing a negative Bencode integer into a <see cref="UInt128" /> member throws
    /// <see cref="BencodeSerializationException" />, matching the overflow contract of <see cref="ulong" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntegerIntoUInt128_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei-1ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<SingleValueModel<UInt128>>(bytes);
        });
    }

    /// <summary>
    /// Verifies that a defined enumeration value serializes to its member-name byte string and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumIsDefined_ShouldWriteMemberName()
    {
        var model = new ColorModel { Color = Color.Green };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Color5:Greene", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<ColorModel>(bytes);
        Assert.AreEqual(Color.Green, roundTripped.Color);
    }

    /// <summary>
    /// Verifies that a combination of flags on a <see cref="FlagsAttribute" /> enumeration serializes to the
    /// comma-separated member-name byte string and round-trips back to the combined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFlagsEnumCombination_ShouldWriteCommaSeparatedNames()
    {
        var model = new PermissionModel { Permissions = Permissions.Read | Permissions.Write };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d11:Permissions11:Read, Writee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<PermissionModel>(bytes);
        Assert.AreEqual(Permissions.Read | Permissions.Write, roundTripped.Permissions);
    }

    /// <summary>
    /// Verifies that an undefined enumeration value serializes to its decimal string form, mirroring the by-name
    /// converter's fallback, and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumValueUndefined_ShouldWriteDecimalString()
    {
        var model = new ColorModel { Color = (Color)99 };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Color2:99e", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<ColorModel>(bytes);
        Assert.AreEqual((Color)99, roundTripped.Color);
    }

    /// <summary>
    /// Verifies that a model whose member is one of the types Bencode cannot natively represent throws
    /// <see cref="NotSupportedException" /> on serialization when no converter is registered for that type.
    /// </summary>
    /// <param name="kat">The unsupported-type scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(UnsupportedTypeCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenMemberTypeHasNoBuiltInConverter_ShouldThrowNotSupportedException(UnsupportedTypeKat kat)
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            kat.Serialize();
        });
    }

    /// <summary>
    /// Verifies that a model whose member is one of the types Bencode cannot natively represent throws
    /// <see cref="NotSupportedException" /> on deserialization when no converter is registered for that type.
    /// </summary>
    /// <param name="kat">The unsupported-type scenario.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(UnsupportedTypeCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Deserialize_WhenMemberTypeHasNoBuiltInConverter_ShouldThrowNotSupportedException(UnsupportedTypeKat kat)
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            kat.Deserialize();
        });
    }

    /// <summary>
    /// Verifies that registering a user <see cref="BencodeConverter{T}" /> for <see cref="bool" /> — a type with no
    /// built-in converter — lets a Boolean member round-trip, proving the converter escape hatch.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBooleanConverterRegistered_ShouldRoundTripUnsupportedType()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BooleanConverter());

        var model = new FlagModel { Enabled = true, Disabled = false };

        byte[] bytes = BencodeSerializer.Serialize(model, options);
        Assert.AreEqual("d8:Disabledi0e7:Enabledi1ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<FlagModel>(bytes, options);
        Assert.IsTrue(roundTripped.Enabled);
        Assert.IsFalse(roundTripped.Disabled);
    }

    /// <summary>
    /// Verifies that registering a user <see cref="BencodeConverter{T}" /> for <see cref="double" /> — a type with no
    /// built-in converter — lets a floating-point member round-trip through a Bencode byte string, proving the converter
    /// escape hatch for a second otherwise-unsupported type.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDoubleConverterRegistered_ShouldRoundTripUnsupportedType()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new DoubleConverter());

        var model = new RatioModel { Ratio = 1.5 };

        byte[] bytes = BencodeSerializer.Serialize(model, options);
        Assert.AreEqual("d5:Ratio3:1.5e", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<RatioModel>(bytes, options);
        Assert.AreEqual(1.5, roundTripped.Ratio);
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
        var method = typeof(BencodeSerializer)
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
        var method = typeof(BencodeSerializer)
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The ratio.</returns>
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
        /// <returns>The color.</returns>
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
        /// <returns>The permissions.</returns>
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
        /// <returns>The value.</returns>
        public T? Value { get; set; }
    }
}
