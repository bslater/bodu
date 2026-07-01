// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that every value the <see cref="TomlSerializer" /> can emit is read back by the same serializer to an
/// equal value — the serializer must never produce a document that its own <c>Deserialize</c> rejects or decodes to a
/// different value.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with <see cref="DateTimeKind.Utc" /> round-trips through
    /// serialization to the same instant.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeKindUtc_ShouldRoundTripToSameInstant()
    {
        var original = new DateTimeModel { Stamp = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Utc) };

        string text = TomlSerializer.Serialize(original);
        DateTimeModel roundTripped = TomlSerializer.Deserialize<DateTimeModel>(text);

        Assert.AreEqual(original.Stamp.ToUniversalTime(), roundTripped.Stamp.ToUniversalTime());
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with <see cref="DateTimeKind.Local" /> round-trips through
    /// serialization to the same instant.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeKindLocal_ShouldRoundTripToSameInstant()
    {
        var original = new DateTimeModel { Stamp = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Local) };

        string text = TomlSerializer.Serialize(original);
        DateTimeModel roundTripped = TomlSerializer.Deserialize<DateTimeModel>(text);

        Assert.AreEqual(original.Stamp.ToUniversalTime(), roundTripped.Stamp.ToUniversalTime());
    }

    /// <summary>
    /// Verifies that <see cref="long.MinValue" /> and <see cref="long.MaxValue" /> round-trip through serialization
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt64Extremes_ShouldRoundTripUnchanged()
    {
        var original = new Int64ExtremesModel { Minimum = long.MinValue, Maximum = long.MaxValue };

        string text = TomlSerializer.Serialize(original);
        Int64ExtremesModel roundTripped = TomlSerializer.Deserialize<Int64ExtremesModel>(text);

        Assert.AreEqual(long.MinValue, roundTripped.Minimum);
        Assert.AreEqual(long.MaxValue, roundTripped.Maximum);
    }

    /// <summary>
    /// Verifies that non-finite and extreme double values round-trip through serialization unchanged, including the
    /// sign of negative zero.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDoubleExtremes_ShouldRoundTripUnchanged()
    {
        var original = new DoubleExtremesModel
        {
            PositiveInfinity = double.PositiveInfinity,
            NegativeInfinity = double.NegativeInfinity,
            NotANumber = double.NaN,
            Epsilon = double.Epsilon,
            Large = 1e308,
            NegativeZero = -0.0,
        };

        string text = TomlSerializer.Serialize(original);
        DoubleExtremesModel roundTripped = TomlSerializer.Deserialize<DoubleExtremesModel>(text);

        Assert.IsTrue(double.IsPositiveInfinity(roundTripped.PositiveInfinity));
        Assert.IsTrue(double.IsNegativeInfinity(roundTripped.NegativeInfinity));
        Assert.IsTrue(double.IsNaN(roundTripped.NotANumber));
        Assert.AreEqual(double.Epsilon, roundTripped.Epsilon);
        Assert.AreEqual(1e308, roundTripped.Large);
        Assert.IsTrue(double.IsNegative(roundTripped.NegativeZero));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTimeOffset" /> carrying maximum tick precision round-trips through
    /// serialization unchanged.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeOffsetAtTickPrecision_ShouldRoundTripUnchanged()
    {
        var original = new DateTimeOffsetModel
        {
            Stamp = new DateTimeOffset(2026, 6, 10, 9, 30, 15, TimeSpan.FromHours(10)).AddTicks(1234567),
        };

        string text = TomlSerializer.Serialize(original);
        DateTimeOffsetModel roundTripped = TomlSerializer.Deserialize<DateTimeOffsetModel>(text);

        Assert.AreEqual(original.Stamp, roundTripped.Stamp);
    }

    /// <summary>
    /// Verifies that serializing a type whose members map to the same wire name throws
    /// <see cref="TomlSerializationException" />, because emitting both would produce a duplicate-key document that
    /// the serializer's own reader rejects.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMembersShareWireName_ShouldThrowTomlSerializationException()
    {
        var model = new DuplicateWireNameModel { First = 1, Second = 2 };

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// Verifies that serializing a model whose extension-data dictionary contains a key equal to a declared member's
    /// wire name throws <see cref="TomlSerializationException" />, because emitting both would produce a
    /// duplicate-key document that the serializer's own reader rejects.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionDataKeyCollidesWithMember_ShouldThrowTomlSerializationException()
    {
        var model = new CollidingExtensionDataModel
        {
            Name = "declared",
            Extra = new Dictionary<string, TomlNode?> { ["Name"] = TomlValue.Create("overflow") },
        };

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// A model carrying a single <see cref="DateTime" /> member.
    /// </summary>
    private sealed class DateTimeModel
    {
        /// <summary>
        /// Gets or sets the date-time under test.
        /// </summary>
        /// <value>The date-time value.</value>
        public DateTime Stamp { get; set; }
    }

    /// <summary>
    /// A model carrying a single <see cref="DateTimeOffset" /> member.
    /// </summary>
    private sealed class DateTimeOffsetModel
    {
        /// <summary>
        /// Gets or sets the date-time offset under test.
        /// </summary>
        /// <value>The date-time offset value.</value>
        public DateTimeOffset Stamp { get; set; }
    }

    /// <summary>
    /// A model carrying the <see cref="long" /> boundary values.
    /// </summary>
    private sealed class Int64ExtremesModel
    {
        /// <summary>
        /// Gets or sets the minimum value member.
        /// </summary>
        /// <value>The minimum value.</value>
        public long Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value member.
        /// </summary>
        /// <value>The maximum value.</value>
        public long Maximum { get; set; }
    }

    /// <summary>
    /// A model carrying non-finite and extreme <see cref="double" /> values.
    /// </summary>
    private sealed class DoubleExtremesModel
    {
        /// <summary>
        /// Gets or sets the positive-infinity member.
        /// </summary>
        /// <value>The positive-infinity value.</value>
        public double PositiveInfinity { get; set; }

        /// <summary>
        /// Gets or sets the negative-infinity member.
        /// </summary>
        /// <value>The negative-infinity value.</value>
        public double NegativeInfinity { get; set; }

        /// <summary>
        /// Gets or sets the not-a-number member.
        /// </summary>
        /// <value>The NaN value.</value>
        public double NotANumber { get; set; }

        /// <summary>
        /// Gets or sets the smallest-positive-subnormal member.
        /// </summary>
        /// <value>The epsilon value.</value>
        public double Epsilon { get; set; }

        /// <summary>
        /// Gets or sets the near-maximum-magnitude member.
        /// </summary>
        /// <value>The large value.</value>
        public double Large { get; set; }

        /// <summary>
        /// Gets or sets the negative-zero member.
        /// </summary>
        /// <value>The negative-zero value.</value>
        public double NegativeZero { get; set; }
    }

    /// <summary>
    /// A model whose two members are mapped to the same wire name via <see cref="TomlPropertyNameAttribute" />.
    /// </summary>
    private sealed class DuplicateWireNameModel
    {
        /// <summary>
        /// Gets or sets the first member mapped to the shared wire name.
        /// </summary>
        /// <value>The first value.</value>
        [TomlPropertyName("shared")]
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the second member mapped to the shared wire name.
        /// </summary>
        /// <value>The second value.</value>
        [TomlPropertyName("shared")]
        public int Second { get; set; }
    }

    /// <summary>
    /// A model whose extension-data dictionary can carry a key that collides with the declared member.
    /// </summary>
    private sealed class CollidingExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension-data member.
        /// </summary>
        /// <value>The overflow entries, or <see langword="null" /> when none exist.</value>
        [TomlExtensionData]
        public Dictionary<string, TomlNode?>? Extra { get; set; }
    }

    /// <summary>
    /// Verifies that each <see cref="double" /> value, including the special sentinels, round-trips bit-for-bit through
    /// TOML.
    /// </summary>
    /// <param name="kat">The float canonical-text row, reused as a round-trip input.</param>
    [TestMethod]
    [DynamicData(nameof(FloatCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenFloatValue_ShouldRoundTripBitExact(ValidKat<double, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        string text = Serialize(kat.Input);
        double actual = TomlSerializer.Deserialize<ValueModel<double>>(text).Value;

        if (double.IsNaN(kat.Input))
            Assert.IsTrue(double.IsNaN(actual));
        else
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(kat.Input), BitConverter.DoubleToInt64Bits(actual));
    }

    /// <summary>
    /// Verifies that a <see cref="float" /> value serializes through the binary64 boundary and round-trips to the equal
    /// single-precision value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSingleValue_ShouldRoundTrip()
    {
        string text = Serialize(0.1f);
        float actual = TomlSerializer.Deserialize<ValueModel<float>>(text).Value;

        Assert.AreEqual(0.1f, actual);
    }

    /// <summary>
    /// Verifies that a <see cref="float" /> value of <see cref="float.MaxValue" /> round-trips to the equal
    /// single-precision value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSingleMaxValue_ShouldRoundTrip()
    {
        string text = Serialize(float.MaxValue);
        float actual = TomlSerializer.Deserialize<ValueModel<float>>(text).Value;

        Assert.AreEqual(float.MaxValue, actual);
    }

    /// <summary>
    /// Verifies that each string value round-trips through TOML to an equal value, including the escaped and non-ASCII
    /// cases.
    /// </summary>
    /// <param name="kat">The string canonical-text row, reused as a round-trip input.</param>
    [TestMethod]
    [DynamicData(nameof(StringCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenStringValue_ShouldRoundTrip(ValidKat<string, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        string text = Serialize(kat.Input);
        string actual = TomlSerializer.Deserialize<ValueModel<string>>(text).Value;

        Assert.AreEqual(kat.Input, actual);
    }

    /// <summary>
    /// Verifies that an integer-family member round-trips through TOML to an equal value across the signed, unsigned,
    /// and native-width types.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SerializeDeserialize_WhenIntegerFamily_ShouldRoundTrip()
    {
        Assert.AreEqual((sbyte)-128, RoundTrip<sbyte>(-128));
        Assert.AreEqual((byte)255, RoundTrip<byte>(255));
        Assert.AreEqual((short)-32768, RoundTrip<short>(-32768));
        Assert.AreEqual(ushort.MaxValue, RoundTrip<ushort>(ushort.MaxValue));
        Assert.AreEqual(int.MinValue, RoundTrip(int.MinValue));
        Assert.AreEqual(uint.MaxValue, RoundTrip(uint.MaxValue));
        Assert.AreEqual(long.MaxValue, RoundTrip(long.MaxValue));
        Assert.AreEqual((nint)42, RoundTrip<nint>(42));
        Assert.AreEqual((nuint)42, RoundTrip<nuint>(42));
    }

    /// <summary>
    /// Verifies that <see cref="Int128" /> and <see cref="UInt128" /> values within the signed 64-bit range TOML can
    /// store round-trip exactly, including at the boundaries.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_When128BitWithinInt64Range_ShouldRoundTrip()
    {
        Assert.AreEqual((Int128)long.MaxValue, RoundTrip((Int128)long.MaxValue));
        Assert.AreEqual((Int128)long.MinValue, RoundTrip((Int128)long.MinValue));
        Assert.AreEqual((UInt128)long.MaxValue, RoundTrip((UInt128)long.MaxValue));
        Assert.AreEqual((Int128)0, RoundTrip((Int128)0));
        Assert.AreEqual((UInt128)0, RoundTrip((UInt128)0));
    }

    /// <summary>
    /// Verifies that a <see cref="bool" /> value serializes to the lowercase <c>true</c> and <c>false</c> literals and
    /// round-trips.
    /// </summary>
    /// <param name="value">The Boolean value under test.</param>
    /// <param name="expected">The expected canonical literal.</param>
    [TestMethod]
    [DataRow(true, "true")]
    [DataRow(false, "false")]
    public void SerializeDeserialize_WhenBoolean_ShouldEmitLiteralAndRoundTrip(bool value, string expected)
    {
        Assert.AreEqual($"Value = {expected}\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a <see cref="char" /> value serializes to a single-character TOML string and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenChar_ShouldEmitSingleCharStringAndRoundTrip()
    {
        Assert.AreEqual("Value = \"A\"\n", Serialize('A'));
        Assert.AreEqual('A', RoundTrip('A'));
    }

    /// <summary>
    /// Verifies that a <see cref="Guid" /> serializes to its canonical 36-character lowercase <c>D</c>-format string and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGuid_ShouldEmitDFormatAndRoundTrip()
    {
        var guid = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

        Assert.AreEqual("Value = \"01234567-89ab-cdef-0123-456789abcdef\"\n", Serialize(guid));
        Assert.AreEqual(guid, RoundTrip(guid));
    }

    /// <summary>
    /// Verifies that an absolute <see cref="Uri" /> serializes to its original string form and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAbsoluteUri_ShouldEmitOriginalStringAndRoundTrip()
    {
        var uri = new Uri("https://example.com/a?b=c");

        Assert.AreEqual("Value = \"https://example.com/a?b=c\"\n", Serialize(uri));
        Assert.AreEqual(uri, RoundTrip(uri));
    }

    /// <summary>
    /// Verifies that a relative <see cref="Uri" /> serializes to its original string form and round-trips, exercising
    /// the relative-or-absolute read path.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRelativeUri_ShouldRoundTrip()
    {
        var uri = new Uri("/path/to", UriKind.Relative);

        Assert.AreEqual("Value = \"/path/to\"\n", Serialize(uri));
        Assert.AreEqual(uri, RoundTrip(uri));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTimeOffset" /> with a zero offset serializes to an RFC 3339 offset date-time using
    /// the <c>Z</c> designator and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeOffsetZero_ShouldUseZAndRoundTrip()
    {
        var value = new DateTimeOffset(2026, 6, 10, 9, 30, 0, TimeSpan.Zero);

        Assert.AreEqual("Value = 2026-06-10T09:30:00Z\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTimeOffset" /> with a positive offset serializes to an RFC 3339 offset date-time
    /// using the <c>+hh:mm</c> form and round-trips, preserving the offset.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeOffsetPositive_ShouldUseSignedOffsetAndRoundTrip()
    {
        var value = new DateTimeOffset(2026, 6, 10, 9, 30, 0, new TimeSpan(5, 30, 0));

        Assert.AreEqual("Value = 2026-06-10T09:30:00+05:30\n", Serialize(value));

        DateTimeOffset actual = RoundTrip(value);
        Assert.AreEqual(value, actual);
        Assert.AreEqual(new TimeSpan(5, 30, 0), actual.Offset);
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> whose kind is <see cref="DateTimeKind.Unspecified" /> serializes to a
    /// TOML local date-time and round-trips, returning a value with <see cref="DateTimeKind.Unspecified" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeUnspecified_ShouldWriteLocalDateTimeAndRoundTrip()
    {
        var value = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Unspecified);

        Assert.AreEqual("Value = 2026-06-10T09:30:00\n", Serialize(value));

        DateTime actual = RoundTrip(value);
        Assert.AreEqual(value, actual);
        Assert.AreEqual(DateTimeKind.Unspecified, actual.Kind);
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with <see cref="DateTimeKind.Utc" /> serializes to an offset date-time
    /// that reads back into a <see cref="DateTimeOffset" /> member equal to the original instant, because the local
    /// date-time read path does not accept the offset form.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeUtc_ShouldRoundTripThroughDateTimeOffset()
    {
        var value = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Utc);

        string text = Serialize(value);
        DateTimeOffset actual = TomlSerializer.Deserialize<ValueModel<DateTimeOffset>>(text).Value;

        Assert.AreEqual(new DateTimeOffset(value), actual);
    }

    /// <summary>
    /// Verifies that a <see cref="DateOnly" /> serializes to a TOML local date and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateOnly_ShouldWriteLocalDateAndRoundTrip()
    {
        var value = new DateOnly(2026, 6, 10);

        Assert.AreEqual("Value = 2026-06-10\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a <see cref="TimeOnly" /> serializes to a TOML local time and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTimeOnly_ShouldWriteLocalTimeAndRoundTrip()
    {
        var value = new TimeOnly(9, 30, 0);

        Assert.AreEqual("Value = 09:30:00\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a fractional-second component of a <see cref="TimeOnly" /> is emitted with trailing zeros trimmed
    /// and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTimeOnlyHasFraction_ShouldEmitTrimmedFractionAndRoundTrip()
    {
        TimeOnly value = new TimeOnly(9, 30, 0).Add(TimeSpan.FromTicks(5_000_000));

        Assert.AreEqual("Value = 09:30:00.5\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a <see cref="Version" /> serializes to its component string form and round-trips, across the two-,
    /// three-, and four-component shapes.
    /// </summary>
    /// <param name="text">The version text under test.</param>
    [TestMethod]
    [DataRow("1.2")]
    [DataRow("1.2.3")]
    [DataRow("10.20.30.40")]
    public void SerializeDeserialize_WhenVersion_ShouldEmitStringAndRoundTrip(string text)
    {
        var value = Version.Parse(text);

        Assert.AreEqual($"Value = \"{text}\"\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a <see cref="TimeSpan" /> serializes to the invariant constant (<c>"c"</c>) string form and
    /// round-trips, across the zero, negative, multi-day, and fractional-second shapes.
    /// </summary>
    /// <param name="days">The day component of the value under test.</param>
    /// <param name="ticksWithinDay">The remaining ticks beyond whole days, carrying the sub-day component.</param>
    /// <param name="expected">The expected canonical constant-format text.</param>
    [TestMethod]
    [DataRow(0, 0L, "00:00:00")]
    [DataRow(0, -300_000_000L, "-00:00:30")]
    [DataRow(1, 73_845_670_000L, "1.02:03:04.5670000")]
    [DataRow(0, 335_400_000_000L, "09:19:00")]
    public void SerializeDeserialize_WhenTimeSpan_ShouldEmitConstantFormatAndRoundTrip(int days, long ticksWithinDay, string expected)
    {
        TimeSpan value = TimeSpan.FromDays(days) + TimeSpan.FromTicks(ticksWithinDay);

        Assert.AreEqual($"Value = \"{expected}\"\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that finite and non-finite <see cref="Half" /> values round-trip through TOML to equal values.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenHalf_ShouldRoundTrip()
    {
        Assert.AreEqual((Half)1.5, RoundTrip((Half)1.5));
        Assert.AreEqual(Half.MaxValue, RoundTrip(Half.MaxValue));
        Assert.AreEqual(Half.PositiveInfinity, RoundTrip(Half.PositiveInfinity));
        Assert.IsTrue(Half.IsNaN(RoundTrip(Half.NaN)));
    }

    /// <summary>
    /// Verifies that a custom converter registered for <see cref="decimal" /> takes precedence over the built-in
    /// decimal converter, supplying its own string representation on both write and read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimalConverterRegistered_ShouldRoundTrip()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new DecimalStringConverter());

        string text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = 19.95m }, options);
        Assert.AreEqual("Value = \"19.95\"\n", text);

        decimal actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text, options).Value;
        Assert.AreEqual(19.95m, actual);
    }

    /// <summary>
    /// Verifies that a byte array serializes by default to a TOML array of integers and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayDefault_ShouldUseIntegerArrayAndRoundTrip()
    {
        byte[] value = [0, 1, 127, 255];

        Assert.AreEqual("Value = [0, 1, 127, 255]\n", Serialize(value));
        CollectionAssert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a byte array serializes to a Base64 basic string when
    /// <see cref="TomlByteArrayHandling.Base64String" /> is selected and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenByteArrayBase64_ShouldUseBase64StringAndRoundTrip()
    {
        var options = new TomlSerializerOptions { ByteArrayHandling = TomlByteArrayHandling.Base64String };
        byte[] value = [0x61, 0x62, 0x63];

        string text = TomlSerializer.Serialize(new ValueModel<byte[]> { Value = value }, options);
        Assert.AreEqual("Value = \"YWJj\"\n", text);

        byte[] actual = TomlSerializer.Deserialize<ValueModel<byte[]>>(text, options).Value;
        CollectionAssert.AreEqual(value, actual);
    }

    /// <summary>
    /// Verifies that a <see cref="Memory{T}" /> of <see cref="byte" /> serializes under both byte-array handlings and
    /// round-trips, sharing the byte-array representation.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemoryOfByte_ShouldUseByteArrayFormsAndRoundTrip()
    {
        Memory<byte> value = new byte[] { 0x61, 0x62, 0x63 };

        Assert.AreEqual("Value = [97, 98, 99]\n", Serialize(value));
        CollectionAssert.AreEqual(value.ToArray(), RoundTrip(value).ToArray());

        var options = new TomlSerializerOptions { ByteArrayHandling = TomlByteArrayHandling.Base64String };
        string text = TomlSerializer.Serialize(new ValueModel<Memory<byte>> { Value = value }, options);
        Assert.AreEqual("Value = \"YWJj\"\n", text);
    }

    /// <summary>
    /// Verifies that a <see cref="ReadOnlyMemory{T}" /> of <see cref="byte" /> serializes under both byte-array
    /// handlings and round-trips, sharing the byte-array representation.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenReadOnlyMemoryOfByte_ShouldUseByteArrayFormsAndRoundTrip()
    {
        ReadOnlyMemory<byte> value = new byte[] { 0x61, 0x62, 0x63 };

        Assert.AreEqual("Value = [97, 98, 99]\n", Serialize(value));
        CollectionAssert.AreEqual(value.ToArray(), RoundTrip(value).ToArray());

        var options = new TomlSerializerOptions { ByteArrayHandling = TomlByteArrayHandling.Base64String };
        string text = TomlSerializer.Serialize(new ValueModel<ReadOnlyMemory<byte>> { Value = value }, options);
        Assert.AreEqual("Value = \"YWJj\"\n", text);
    }

    /// <summary>
    /// Verifies that an empty <see cref="ReadOnlyMemory{T}" /> of <see cref="byte" /> serializes to an empty TOML array
    /// and round-trips to an empty memory.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemoryOfByteEmpty_ShouldRoundTripEmpty()
    {
        ReadOnlyMemory<byte> value = ReadOnlyMemory<byte>.Empty;

        Assert.AreEqual("Value = []\n", Serialize(value));
        Assert.AreEqual(0, RoundTrip(value).Length);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member round-trips: the element read back re-serializes to the text
    /// its source value produced.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenObjectMember_ShouldRoundTripThroughElement()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new[] { 1, 2, 3 } });
        object element = TomlSerializer.Deserialize<ValueModel<object>>(text).Value;
        string again = TomlSerializer.Serialize(new ValueModel<object> { Value = element });

        Assert.AreEqual(text, again);
    }
    /// <summary>
    /// Verifies that a <see cref="TomlElement" /> member deserializes from each TOML value kind with the matching
    /// <see cref="TomlValueKind" /> and re-serializes to the identical text.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the element value.</param>
    /// <param name="kind">The expected value kind of the deserialized element.</param>
    [TestMethod]
    [DataRow("Value = \"x\"\n", TomlValueKind.String, DisplayName = "string")]
    [DataRow("Value = 5\n", TomlValueKind.Integer, DisplayName = "integer")]
    [DataRow("Value = 1.5\n", TomlValueKind.Float, DisplayName = "float")]
    [DataRow("Value = true\n", TomlValueKind.Boolean, DisplayName = "boolean")]
    [DataRow("Value = [1, 2, 3]\n", TomlValueKind.Array, DisplayName = "array")]
    [DataRow("Value = { A = 1 }\n", TomlValueKind.Table, DisplayName = "table")]
    public void SerializeDeserialize_WhenTomlElementMember_ShouldPreserveKindAndRoundTrip(string toml, TomlValueKind kind)
    {
        TomlElement element = TomlSerializer.Deserialize<ValueModel<TomlElement>>(toml).Value;

        Assert.AreEqual(kind, element.ValueKind);

        string text = TomlSerializer.Serialize(new ValueModel<TomlElement> { Value = element });
        TomlElement again = TomlSerializer.Deserialize<ValueModel<TomlElement>>(text).Value;
        Assert.AreEqual(kind, again.ValueKind);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlDocument" /> deserializes at the document root, is owned by the caller, and
    /// re-serializes to the identical text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTomlDocumentRoot_ShouldRoundTripText()
    {
        const string toml = "A = 1\nB = \"x\"\n";

        using TomlDocument document = TomlSerializer.Deserialize<TomlDocument>(toml);

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind);
        Assert.AreEqual(toml, TomlSerializer.Serialize(document));
    }

    /// <summary>
    /// Verifies that a <see cref="TomlElement" /> whose kind is a table deserializes at the document root and
    /// re-serializes to the identical text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTomlElementRoot_ShouldRoundTripText()
    {
        const string toml = "A = 1\n";

        TomlElement element = TomlSerializer.Deserialize<TomlElement>(toml);

        Assert.AreEqual(TomlValueKind.Table, element.ValueKind);
        Assert.AreEqual(toml, TomlSerializer.Serialize(element));
    }

    /// <summary>
    /// Verifies that a decimal written under <see cref="TomlDecimalHandling.String" /> round-trips exactly, including
    /// the full-precision and boundary values that the float form cannot hold.
    /// </summary>
    /// <param name="kat">The decimal canonical-text row, reused as a round-trip input.</param>
    [TestMethod]
    [DynamicData(nameof(DecimalCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenDecimalStringHandling_ShouldRoundTripExactly(DecimalCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new TomlSerializerOptions { DecimalHandling = TomlDecimalHandling.String };
        string text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = kat.Input }, options);
        decimal actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text, options).Value;

        Assert.AreEqual(kat.Input, actual);
    }

    /// <summary>
    /// Verifies that a decimal within double's exactly-representable range round-trips under the default
    /// <see cref="TomlDecimalHandling.Float" /> handling.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimalFloatHandlingWithinDoublePrecision_ShouldRoundTrip()
    {
        string text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = 19.95m });
        decimal actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text).Value;

        Assert.AreEqual(19.95m, actual);
    }

    /// <summary>
    /// Verifies that the default <see cref="TomlDecimalHandling.Float" /> handling loses precision beyond what an IEEE
    /// 754 binary64 value can hold, documenting the trade-off the string handling exists to avoid.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimalFloatHandlingExceedsDoublePrecision_ShouldLosePrecision()
    {
        const decimal value = 0.1234567890123456789012345678m;

        string text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = value });
        decimal actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text).Value;

        Assert.AreNotEqual(value, actual);
        Assert.AreEqual((decimal)(double)value, actual);
    }

    /// <summary>
    /// Verifies that every power-of-ten scale of a representative mantissa round-trips exactly under
    /// <see cref="TomlDecimalHandling.String" />, sweeping the full scale range of <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SerializeDeserialize_WhenDecimalScaleSweepUnderStringHandling_ShouldRoundTripExactly()
    {
        var options = new TomlSerializerOptions { DecimalHandling = TomlDecimalHandling.String };

        for (byte scale = 0; scale <= 28; scale++)
        {
            decimal value = new(987654321, 123456789, 0, isNegative: false, scale);
            string text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = value }, options);
            decimal actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text, options).Value;

            Assert.AreEqual(value, actual, $"scale {scale}");
        }
    }

}
