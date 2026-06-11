// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Values.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the native scalar value model of <see cref="TomlSerializer" />: integers (including the range-checked
/// boundaries and overflow on read), floats (fractional, exponent, and the <c>inf</c>/<c>-inf</c>/<c>nan</c>
/// sentinels), the four date-time kinds, Booleans, strings with escaping, characters, <see cref="Guid" />,
/// <see cref="Uri" />, byte arrays under both handlings, and the types TOML cannot natively represent.
/// </summary>
public partial class TomlSerializerTests
{
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
        yield return [new FloatCanon("float half", 1.5, "1.5")];
        yield return [new FloatCanon("float whole gets point", 1.0, "1.0")];
        yield return [new FloatCanon("float zero", 0.0, "0.0")];
        yield return [new FloatCanon("float negative zero", -0.0, "-0.0")];
        yield return [new FloatCanon("float large no exp", 1e10, "10000000000.0")];
        yield return [new FloatCanon("float small exp", 1e-10, "1E-10")];
        yield return [new FloatCanon("float avogadro", 6.022e23, "6.022E+23")];
        yield return [new FloatCanon("float pi", Math.PI, "3.141592653589793")];
        yield return [new FloatCanon("float nan", double.NaN, "nan")];
        yield return [new FloatCanon("float positive infinity", double.PositiveInfinity, "inf")];
        yield return [new FloatCanon("float negative infinity", double.NegativeInfinity, "-inf")];
        yield return [new FloatCanon("float max", double.MaxValue, "1.7976931348623157E+308")];
        yield return [new FloatCanon("float epsilon", double.Epsilon, "5E-324")];
    }

    /// <summary>
    /// Provides the canonical-text known-answer rows for strings: each row pins the exact basic-quoted form, exercising
    /// the TOML string escapes and the pass-through of printable non-ASCII characters.
    /// </summary>
    /// <returns>The sequence of single-element argument arrays, each carrying one string canonical-text row.</returns>
    public static IEnumerable<object[]> StringCanonicalRows()
    {
        yield return [new StringCanon("string simple", "hello", "\"hello\"")];
        yield return [new StringCanon("string empty", string.Empty, "\"\"")];
        yield return [new StringCanon("string tab", "a\tb", "\"a\\tb\"")];
        yield return [new StringCanon("string newline", "a\nb", "\"a\\nb\"")];
        yield return [new StringCanon("string carriage return", "a\rb", "\"a\\rb\"")];
        yield return [new StringCanon("string quote", "a\"b", "\"a\\\"b\"")];
        yield return [new StringCanon("string backslash", "a\\b", "\"a\\\\b\"")];
        yield return [new StringCanon("string backspace", "a\bb", "\"a\\bb\"")];
        yield return [new StringCanon("string form feed", "a\fb", "\"a\\fb\"")];
        yield return [new StringCanon("string nul", "a\0b", "\"a\\u0000b\"")];
        yield return [new StringCanon("string delete", "ab", "\"a\\u007Fb\"")];
        yield return [new StringCanon("string unit separator", "ab", "\"a\\u001Fb\"")];
        yield return [new StringCanon("string accented passthrough", "café", "\"café\"")];
        yield return [new StringCanon("string emoji passthrough", "\U0001F600", "\"\U0001F600\"")];
    }

    /// <summary>
    /// Verifies that each integer-family value serializes to its expected canonical TOML key/value line.
    /// </summary>
    /// <param name="kat">The integer canonical-text row under test.</param>
    [TestMethod]
    [DynamicData(nameof(IntegerCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenIntegerValue_ShouldEmitCanonicalText(IntCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual($"Value = {kat.Expected}\n", kat.Serialize());
    }

    /// <summary>
    /// Verifies that each <see cref="double" /> value serializes to its expected canonical TOML spelling, including the
    /// <c>inf</c>, <c>-inf</c>, and <c>nan</c> sentinels.
    /// </summary>
    /// <param name="kat">The float canonical-text row under test.</param>
    [TestMethod]
    [DynamicData(nameof(FloatCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenFloatValue_ShouldEmitCanonicalText(FloatCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual($"Value = {kat.Expected}\n", Serialize(kat.Input));
    }

    /// <summary>
    /// Verifies that each <see cref="double" /> value, including the special sentinels, round-trips bit-for-bit through
    /// TOML.
    /// </summary>
    /// <param name="kat">The float canonical-text row, reused as a round-trip input.</param>
    [TestMethod]
    [DynamicData(nameof(FloatCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenFloatValue_ShouldRoundTripBitExact(FloatCanon kat)
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
    /// Verifies that each string value serializes to its expected canonical basic-quoted TOML form, exercising the
    /// escape rules and the pass-through of printable non-ASCII characters.
    /// </summary>
    /// <param name="kat">The string canonical-text row under test.</param>
    [TestMethod]
    [DynamicData(nameof(StringCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenStringValue_ShouldEmitCanonicalText(StringCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual($"Value = {kat.Expected}\n", Serialize(kat.Input));
    }

    /// <summary>
    /// Verifies that each string value round-trips through TOML to an equal value, including the escaped and non-ASCII
    /// cases.
    /// </summary>
    /// <param name="kat">The string canonical-text row, reused as a round-trip input.</param>
    [TestMethod]
    [DynamicData(nameof(StringCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenStringValue_ShouldRoundTrip(StringCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        string text = Serialize(kat.Input);
        string actual = TomlSerializer.Deserialize<ValueModel<string>>(text).Value;

        Assert.AreEqual(kat.Input, actual);
    }

    /// <summary>
    /// Verifies that reading an integer that is outside the target type's range throws
    /// <see cref="TomlSerializationException" /> rather than silently wrapping, demonstrating the checked conversion.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerOutOfTargetRange_ShouldThrowTomlSerializationException()
    {
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<byte>>("Value = 999\n");
        });

        Assert.IsTrue(ex.Message.Contains("999", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that reading a negative integer into an unsigned target throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntoUnsigned_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<ulong>>("Value = -1\n");
        });
    }

    /// <summary>
    /// Verifies that serializing a <see cref="ulong" /> whose value exceeds the signed 64-bit range TOML can store
    /// throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenUnsignedExceedsInt64Range_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = Serialize(ulong.MaxValue);
        });
    }

    /// <summary>
    /// Verifies that reading an integer literal that overflows the signed 64-bit range is rejected by the reader as a
    /// <see cref="TomlFormatException" />, because TOML stores integers as 64-bit signed.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerLiteralExceedsInt64_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<long>>("Value = 9223372036854775808\n");
        });
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
    /// Verifies that a control <see cref="char" /> is escaped when written, mirroring the string escape rules.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenControlChar_ShouldEscape()
    {
        Assert.AreEqual("Value = \"\\n\"\n", Serialize('\n'));
    }

    /// <summary>
    /// Verifies that reading a multi-character string into a <see cref="char" /> member throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCharStringTooLong_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<char>>("Value = \"ab\"\n");
        });
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
    /// Verifies that reading a string that is not a valid <c>D</c>-format GUID into a <see cref="Guid" /> member throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGuidStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Guid>>("Value = \"not-a-guid\"\n");
        });
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
    /// Verifies that a <see cref="DateTimeOffset" /> with a negative offset serializes to an RFC 3339 offset date-time
    /// using the <c>-hh:mm</c> form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDateTimeOffsetNegative_ShouldUseSignedOffset()
    {
        var value = new DateTimeOffset(2026, 6, 10, 9, 30, 0, new TimeSpan(-8, 0, 0));

        Assert.AreEqual("Value = 2026-06-10T09:30:00-08:00\n", Serialize(value));
    }

    /// <summary>
    /// Verifies that a fractional-second component of a <see cref="DateTimeOffset" /> is emitted with trailing zeros
    /// trimmed.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDateTimeOffsetHasFraction_ShouldEmitTrimmedFraction()
    {
        var value = new DateTimeOffset(new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Unspecified).AddTicks(1234500), TimeSpan.Zero);

        Assert.AreEqual("Value = 2026-06-10T09:30:00.12345Z\n", Serialize(value));
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
    /// Verifies that a <see cref="DateTime" /> whose kind is <see cref="DateTimeKind.Utc" /> serializes to a TOML offset
    /// date-time with the <c>Z</c> designator.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDateTimeUtc_ShouldWriteOffsetDateTime()
    {
        var value = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Utc);

        Assert.AreEqual("Value = 2026-06-10T09:30:00Z\n", Serialize(value));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> whose kind is <see cref="DateTimeKind.Local" /> serializes to a TOML
    /// offset date-time, carrying the local offset rather than the local form.
    /// </summary>
    /// <remarks>
    /// The numeric offset depends on the host time zone, so this test asserts the structural form — an offset date-time
    /// rather than a bare local date-time — instead of an exact offset.
    /// </remarks>
    [TestMethod]
    public void Serialize_WhenDateTimeLocal_ShouldWriteOffsetDateTime()
    {
        var value = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Local);
        var expectedOffset = new DateTimeOffset(value);

        string text = Serialize(value);

        Assert.AreEqual($"Value = {ExpectedOffsetText(expectedOffset)}\n", text);
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
        var value = new TimeOnly(9, 30, 0).Add(TimeSpan.FromTicks(5_000_000));

        Assert.AreEqual("Value = 09:30:00.5\n", Serialize(value));
        Assert.AreEqual(value, RoundTrip(value));
    }

    /// <summary>
    /// Verifies that serializing a <see cref="decimal" /> with no registered converter throws
    /// <see cref="NotSupportedException" />, because TOML has no native form for it.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDecimalAndNoConverter_ShouldThrowNotSupportedException()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = Serialize(1.5m);
        });
    }

    /// <summary>
    /// Verifies that deserializing a <see cref="decimal" /> with no registered converter throws
    /// <see cref="NotSupportedException" />, because TOML has no native form for it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDecimalAndNoConverter_ShouldThrowNotSupportedException()
    {
        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>("Value = 1.5\n");
        });
    }

    /// <summary>
    /// Verifies that a custom converter registered for <see cref="decimal" /> supplies the otherwise-absent native form,
    /// proving the escape hatch for a type TOML cannot represent.
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
    /// Verifies that a byte array written as a Base64 string is read back even under the default integer-array handling,
    /// because the reader accepts either form.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenByteArrayBase64UnderDefaultHandling_ShouldDecode()
    {
        byte[] actual = TomlSerializer.Deserialize<ValueModel<byte[]>>("Value = \"YWJj\"\n").Value;

        CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x63 }, actual);
    }

    /// <summary>
    /// Verifies that reading a byte array from an integer element outside the byte range throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenByteArrayElementOutOfRange_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<byte[]>>("Value = [256]\n");
        });
    }

    /// <summary>
    /// Verifies that reading a byte array from a string that is not valid Base64 throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenByteArrayStringNotBase64_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<byte[]>>("Value = \"!!!\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a TOML value whose kind does not match the target scalar member throws
    /// <see cref="TomlSerializationException" /> across the representative type-mismatch cases.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the mismatched value.</param>
    [TestMethod]
    [DataRow("Value = \"x\"\n", DisplayName = "string into int")]
    [DataRow("Value = 1.5\n", DisplayName = "float into int")]
    public void Deserialize_WhenValueKindMismatchForInt_ShouldThrowTomlSerializationException(string toml)
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<int>>(toml);
        });
    }

    /// <summary>
    /// Verifies that reading a TOML integer into a <see cref="string" /> member throws
    /// <see cref="TomlSerializationException" />, because the string converter requires a string token.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerIntoString_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<string>>("Value = 5\n");
        });
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
        /// <returns>The boxed value.</returns>
        public T Value { get; set; } = default!;
    }

    /// <summary>
    /// A known-answer row pinning the canonical text an integer value serializes to, deferring the serialization so the
    /// boxed type stays strongly typed.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Serialize">A function that serializes the boxed integer to TOML text.</param>
    /// <param name="Expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    public sealed record IntCanon(string Name, Func<string> Serialize, string Expected) : IKat;

    /// <summary>
    /// A known-answer row pinning the canonical spelling a <see cref="double" /> value serializes to.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Input">The double value to serialize.</param>
    /// <param name="Expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    public sealed record FloatCanon(string Name, double Input, string Expected) : IKat;

    /// <summary>
    /// A known-answer row pinning the canonical basic-quoted form a string value serializes to.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Input">The string value to serialize.</param>
    /// <param name="Expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    public sealed record StringCanon(string Name, string Input, string Expected) : IKat;

    /// <summary>
    /// A custom converter mapping <see cref="decimal" /> to and from a TOML basic string, supplying the native form TOML
    /// otherwise lacks.
    /// </summary>
    private sealed class DecimalStringConverter
        : TomlConverter<decimal>
    {
        /// <inheritdoc />
        public override decimal Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            decimal.Parse(reader.GetString(), CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, decimal value, TomlSerializerOptions options) =>
            writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
    }
}
