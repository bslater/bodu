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
/// <see cref="Uri" />, <see cref="Version" />, <see cref="TimeSpan" />, <see cref="Half" />, the 128-bit integers,
/// byte arrays and memory-of-byte under both handlings, and the precedence of a registered converter over a built-in.
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
    public void Serialize_WhenFloatValue_ShouldEmitCanonicalText(ValidKat<double, string> kat)
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
    /// Verifies that each string value serializes to its expected canonical basic-quoted TOML form, exercising the
    /// escape rules and the pass-through of printable non-ASCII characters.
    /// </summary>
    /// <param name="kat">The string canonical-text row under test.</param>
    [TestMethod]
    [DynamicData(nameof(StringCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenStringValue_ShouldEmitCanonicalText(ValidKat<string, string> kat)
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
    public void SerializeDeserialize_WhenStringValue_ShouldRoundTrip(ValidKat<string, string> kat)
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
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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
    /// Verifies that serializing an <see cref="Int128" /> outside the signed 64-bit range TOML can store throws
    /// <see cref="TomlSerializationException" /> carrying the checked-conversion <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenInt128ExceedsInt64Range_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = Serialize(Int128.MaxValue);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that serializing a <see cref="UInt128" /> outside the signed 64-bit range TOML can store throws
    /// <see cref="TomlSerializationException" /> carrying the checked-conversion <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenUInt128ExceedsInt64Range_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = Serialize((UInt128)long.MaxValue + 1);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that reading a negative TOML integer into a <see cref="UInt128" /> member throws
    /// <see cref="TomlSerializationException" />, demonstrating the checked conversion on the read path.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntoUInt128_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<UInt128>>("Value = -1\n");
        });
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
    /// Verifies that reading a <see cref="Version" /> from a string with leading or trailing whitespace throws
    /// <see cref="TomlSerializationException" />, matching the strictness of the
    /// <see cref="System.Text.Json" /> converter.
    /// </summary>
    /// <param name="padded">The padded version text under test.</param>
    [TestMethod]
    [DataRow(" 1.2.3")]
    [DataRow("1.2.3 ")]
    public void Deserialize_WhenVersionStringPadded_ShouldThrowTomlSerializationException(string padded)
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Version>>($"Value = \"{padded}\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="Version" /> from a string that is not a parsable version throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenVersionStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Version>>("Value = \"not-a-version\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="Version" /> from a non-string token throws
    /// <see cref="TomlSerializationException" />, because the converter requires a string.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenVersionFromInteger_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Version>>("Value = 1\n");
        });
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
    /// Verifies that reading a <see cref="TimeSpan" /> from a string that does not match the constant format throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTimeSpanStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<TimeSpan>>("Value = \"not-a-timespan\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="TimeSpan" /> from a non-string token throws
    /// <see cref="TomlSerializationException" />, because the converter requires the constant-format string.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTimeSpanFromInteger_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<TimeSpan>>("Value = 30\n");
        });
    }

    /// <summary>
    /// Verifies that a <see cref="Half" /> value serializes to a TOML float through the exact widening to
    /// <see cref="double" />, including the <c>nan</c>, <c>inf</c>, and <c>-inf</c> sentinels.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenHalf_ShouldEmitFloatText()
    {
        Assert.AreEqual("Value = 1.5\n", Serialize((Half)1.5));
        Assert.AreEqual("Value = 65504.0\n", Serialize(Half.MaxValue));
        Assert.AreEqual("Value = nan\n", Serialize(Half.NaN));
        Assert.AreEqual("Value = inf\n", Serialize(Half.PositiveInfinity));
        Assert.AreEqual("Value = -inf\n", Serialize(Half.NegativeInfinity));
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
    /// Verifies that reading a finite TOML float outside the <see cref="Half" /> range saturates to infinity rather than
    /// throwing, matching IEEE 754 narrowing and the behavior of the <see cref="float" /> converter.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFloatExceedsHalfRange_ShouldSaturateToInfinity()
    {
        Half actual = TomlSerializer.Deserialize<ValueModel<Half>>("Value = 1e10\n").Value;

        Assert.IsTrue(Half.IsPositiveInfinity(actual));
    }

    /// <summary>
    /// Verifies that reading a <see cref="Half" /> from a non-float token throws
    /// <see cref="TomlSerializationException" />, mirroring the strictness of the <see cref="float" /> converter.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenHalfFromInteger_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Half>>("Value = 1\n");
        });
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
    /// Verifies that a memory-of-byte member written as a Base64 string reads back under the default integer-array
    /// handling, because the shared read path accepts either form.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemoryOfByteFromBase64UnderDefaultHandling_ShouldDecode()
    {
        Memory<byte> actual = TomlSerializer.Deserialize<ValueModel<Memory<byte>>>("Value = \"YWJj\"\n").Value;

        CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x63 }, actual.ToArray());
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
}
