// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
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
/// Round-trips a value through serialize then deserialize.
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

        StringModel roundTripped = BencodeSerializer.Deserialize<StringModel>(bytes);
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

        StringModel roundTripped = BencodeSerializer.Deserialize<StringModel>(bytes);
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

        StringModel roundTripped = BencodeSerializer.Deserialize<StringModel>(bytes);
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

        BytesModel roundTripped = BencodeSerializer.Deserialize<BytesModel>(bytes);
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

        BytesModel roundTripped = BencodeSerializer.Deserialize<BytesModel>(bytes);
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

        ULongModel roundTripped = BencodeSerializer.Deserialize<ULongModel>(bytes);
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

        ULongModel roundTripped = BencodeSerializer.Deserialize<ULongModel>(bytes);
        Assert.AreEqual(value, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that <see cref="Memory{T}" /> and <see cref="ReadOnlyMemory{T}" /> of <see cref="byte" /> serialize to
    /// the native length-prefixed byte string and round-trip losslessly, including binary payloads and the empty
    /// memory.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemoryOfByte_ShouldRoundTripAsByteString()
    {
        Memory<byte> payload = new byte[] { 0x00, 0x61, 0xff };

        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<Memory<byte>> { Value = payload });
        byte[] expected = [.. Encoding.Latin1.GetBytes("d5:Value3:"), 0x00, 0x61, 0xff, (byte)'e'];
        CollectionAssert.AreEqual(expected, bytes);

        Memory<byte> roundTripped = BencodeSerializer.Deserialize<SingleValueModel<Memory<byte>>>(bytes).Value;
        CollectionAssert.AreEqual(payload.ToArray(), roundTripped.ToArray());

        byte[] readOnly = BencodeSerializer.Serialize(new SingleValueModel<ReadOnlyMemory<byte>> { Value = payload });
        CollectionAssert.AreEqual(expected, readOnly);

        byte[] empty = BencodeSerializer.Serialize(new SingleValueModel<ReadOnlyMemory<byte>> { Value = ReadOnlyMemory<byte>.Empty });
        Assert.AreEqual("d5:Value0:e", Encoding.Latin1.GetString(empty));
        Assert.AreEqual(0, BencodeSerializer.Deserialize<SingleValueModel<ReadOnlyMemory<byte>>>(empty).Value.Length);
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
    /// Verifies that a defined enumeration value serializes to its member-name byte string and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumIsDefined_ShouldWriteMemberName()
    {
        var model = new ColorModel { Color = Color.Green };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Color5:Greene", Encoding.Latin1.GetString(bytes));

        ColorModel roundTripped = BencodeSerializer.Deserialize<ColorModel>(bytes);
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

        PermissionModel roundTripped = BencodeSerializer.Deserialize<PermissionModel>(bytes);
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

        ColorModel roundTripped = BencodeSerializer.Deserialize<ColorModel>(bytes);
        Assert.AreEqual((Color)99, roundTripped.Color);
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

        FlagModel roundTripped = BencodeSerializer.Deserialize<FlagModel>(bytes, options);
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

        RatioModel roundTripped = BencodeSerializer.Deserialize<RatioModel>(bytes, options);
        Assert.AreEqual(1.5, roundTripped.Ratio);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member round-trips: the element read back re-serializes to the
    /// bytes its source value produced.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenObjectMember_ShouldRoundTripThroughElement()
    {
        byte[] bytes = BencodeSerializer.Serialize(new SingleValueModel<object> { Value = new[] { 1, 2, 3 } });
        object element = BencodeSerializer.Deserialize<SingleValueModel<object>>(bytes).Value!;
        byte[] again = BencodeSerializer.Serialize(new SingleValueModel<object> { Value = element });

        CollectionAssert.AreEqual(bytes, again);
    }
    /// <summary>
    /// Verifies that a <see cref="BencodeElement" /> member deserializes from each Bencode value kind with the matching
    /// <see cref="BencodeValueKind" /> and re-serializes to the identical bytes.
    /// </summary>
    /// <param name="encoded">The Bencode document carrying the element value.</param>
    /// <param name="kind">The expected value kind of the deserialized element.</param>
    [TestMethod]
    [DataRow("d5:Value5:helloe", BencodeValueKind.ByteString, DisplayName = "byte string")]
    [DataRow("d5:Valuei5ee", BencodeValueKind.Integer, DisplayName = "integer")]
    [DataRow("d5:Valueli1ei2eee", BencodeValueKind.Array, DisplayName = "list")]
    [DataRow("d5:Valued1:Ai1eee", BencodeValueKind.Object, DisplayName = "dictionary")]
    public void SerializeDeserialize_WhenBencodeElementMember_ShouldPreserveKindAndRoundTrip(string encoded, BencodeValueKind kind)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(encoded);

        BencodeElement element = BencodeSerializer.Deserialize<SingleValueModel<BencodeElement>>(bytes).Value;
        Assert.AreEqual(kind, element.ValueKind);

        byte[] again = BencodeSerializer.Serialize(new SingleValueModel<BencodeElement> { Value = element });
        CollectionAssert.AreEqual(bytes, again);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeDocument" /> deserializes at the document root, is owned by the caller, and
    /// re-serializes to the identical bytes.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBencodeDocumentRoot_ShouldRoundTripBytes()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d1:Ai1e1:B1:xe");

        using BencodeDocument document = BencodeSerializer.Deserialize<BencodeDocument>(bytes);

        Assert.AreEqual(BencodeValueKind.Object, document.RootElement.ValueKind);
        CollectionAssert.AreEqual(bytes, BencodeSerializer.Serialize(document));
    }

    /// <summary>
    /// Verifies that a scalar value deserializes into a <see cref="BencodeElement" /> at the document root and
    /// re-serializes to the identical bytes, because a Bencode document roots any value kind.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBencodeElementRootIsScalar_ShouldRoundTripBytes()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("i42e");

        BencodeElement element = BencodeSerializer.Deserialize<BencodeElement>(bytes);

        Assert.AreEqual(BencodeValueKind.Integer, element.ValueKind);
        CollectionAssert.AreEqual(bytes, BencodeSerializer.Serialize(element));
    }

}
