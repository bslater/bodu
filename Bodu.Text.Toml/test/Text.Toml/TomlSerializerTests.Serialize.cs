// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Serializes a value to TOML text or bytes.
/// </summary>
public partial class TomlSerializerTests
{
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
    /// Verifies that serializing a <see cref="ulong" /> whose value exceeds the signed 64-bit range TOML can store
    /// throws <see cref="TomlSerializationException" />, from the first value past <see cref="long.MaxValue" /> to
    /// <see cref="ulong.MaxValue" />, rather than writing an integer a conforming reader rejects or a wrapped negative
    /// one.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    [TestMethod]
    [DataRow(9_223_372_036_854_775_808UL, DisplayName = "long.MaxValue + 1")]
    [DataRow(ulong.MaxValue, DisplayName = "ulong.MaxValue")]
    public void Serialize_WhenUnsignedExceedsInt64Range_ShouldThrowTomlSerializationException(ulong value)
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = Serialize(value);
        });
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
    /// Verifies that a control <see cref="char" /> is escaped when written, mirroring the string escape rules.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenControlChar_ShouldEscape()
    {
        Assert.AreEqual("Value = \"\\n\"\n", Serialize('\n'));
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
    /// The numeric offset depends on the host time zone, so this test asserts the structural form - an offset date-time
    /// rather than a bare local date-time - instead of an exact offset.
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
    /// Verifies that an <see cref="object" />-typed member serializes through its runtime type's converter across the
    /// representative scalar shapes.
    /// </summary>
    /// <param name="value">The boxed value under test.</param>
    /// <param name="expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    [TestMethod]
    [DataRow("x", "\"x\"", DisplayName = "boxed string")]
    [DataRow(5, "5", DisplayName = "boxed int")]
    [DataRow(1.5, "1.5", DisplayName = "boxed double")]
    [DataRow(true, "true", DisplayName = "boxed bool")]
    public void Serialize_WhenObjectMemberHoldsScalar_ShouldDispatchToRuntimeType(object value, string expected)
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = value });

        Assert.AreEqual($"Value = {expected}\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a boxed array serializes as a TOML array through the
    /// runtime type's converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsArray_ShouldWriteArray()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new[] { 1, 2, 3 } });

        Assert.AreEqual("Value = [1, 2, 3]\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a dictionary serializes as a TOML table through the
    /// runtime type's converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsDictionary_ShouldWriteTable()
    {
        var value = new Dictionary<string, int> { ["A"] = 1 };

        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = value });

        Assert.AreEqual("[Value]\nA = 1\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a plain object graph serializes as a TOML table
    /// through the runtime type's converter.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsPoco_ShouldWriteTable()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new ValueModel<int> { Value = 7 } });

        Assert.AreEqual("[Value]\nValue = 7\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member holding a bare <see cref="object" /> serializes as an empty
    /// table - the TOML analogue of the empty JSON object - which the writer canonically emits as an empty header
    /// section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsBareObject_ShouldWriteEmptyTable()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = new object() });

        Assert.AreEqual("[Value]\n", text);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member whose value is <see langword="null" /> is omitted from the
    /// output, because TOML has no null form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberNull_ShouldOmitMember()
    {
        string text = TomlSerializer.Serialize(new ValueModel<object> { Value = null! });

        Assert.AreEqual(string.Empty, text);
    }

    /// <summary>
    /// Verifies that serializing a boxed table-shaped value typed as <see cref="object" /> at the document root
    /// dispatches to the runtime type and emits the table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectRootHoldsTableShape_ShouldWriteTable()
    {
        object value = new Dictionary<string, int> { ["A"] = 1 };

        Assert.AreEqual("A = 1\n", TomlSerializer.Serialize(value));
    }

    /// <summary>
    /// Verifies that serializing a boxed scalar typed as <see cref="object" /> at the document root throws
    /// <see cref="InvalidOperationException" /> from the writer's root state machine, because a TOML document root must
    /// be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectRootHoldsScalar_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Serialize<object>(5);
        });
    }

    /// <summary>
    /// Verifies that an element obtained from a parsed document serializes as a member, re-emitting the value it views.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTomlElementFromParsedDocument_ShouldEmitViewedValue()
    {
        using var document = TomlDocument.Parse("Inner = 5\n");
        TomlElement element = document.RootElement.GetProperty("Inner");

        string text = TomlSerializer.Serialize(new ValueModel<TomlElement> { Value = element });

        Assert.AreEqual("Value = 5\n", text);
    }

    /// <summary>
    /// Verifies that serializing a scalar <see cref="TomlElement" /> at the document root throws
    /// <see cref="InvalidOperationException" /> from the writer's root state machine, because a TOML document root must
    /// be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenScalarTomlElementAtRoot_ShouldThrowInvalidOperationException()
    {
        TomlElement scalar = TomlSerializer.Deserialize<ValueModel<TomlElement>>("Value = 5\n").Value;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Serialize(scalar);
        });
    }

    /// <summary>
    /// Verifies that serializing a member whose <see cref="TomlElement" /> is the default value throws
    /// <see cref="InvalidOperationException" />, because the element belongs to no document.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultTomlElementMember_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Serialize(new ValueModel<TomlElement> { Value = default });
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.WriteTo" /> on an element of a disposed document throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTomlElementFromDisposedDocument_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("A = 1\n");
        TomlElement element = document.RootElement;
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = TomlSerializer.Serialize(element);
        });
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> <see cref="TomlDocument" /> at the root throws
    /// <see cref="TomlSerializationException" />, because TOML has no null form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullTomlDocument_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize<TomlDocument>(null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Serialize{T}(IBufferWriter{byte}, T, TomlSerializerOptions?)" />
    /// throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c> when the buffer writer is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsNull_ForBufferWriterOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            TomlSerializer.Serialize((IBufferWriter<byte>)null!, new GuardModel { Value = 5 });
        }, "destination");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Serialize{T}(IBufferWriter{byte}, T, TomlSerializerOptions?)" />
    /// writes the value's canonical text to the buffer writer when the destination is valid, confirming the guard
    /// admits the supported case.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsBufferWriter_ShouldWriteCanonicalText()
    {
        var buffer = new ArrayBufferWriter<byte>();

        TomlSerializer.Serialize(buffer, new GuardModel { Value = 5 });

        Assert.AreEqual("Value = 5\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a decimal serializes to its expected canonical value text under the handling each row selects.
    /// </summary>
    /// <param name="kat">The decimal canonical-text row under test.</param>
    [TestMethod]
    [DynamicData(nameof(DecimalCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenDecimal_ShouldEmitCanonicalTextForHandling(DecimalCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new TomlSerializerOptions { DecimalHandling = kat.Handling };
        string text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = kat.Input }, options);

        Assert.AreEqual($"Value = {kat.Expected}\n", text);
    }

    /// <summary>
    /// Verifies that a member whose converter writes no value makes serialization throw
    /// <see cref="InvalidOperationException" />, because the writer is left holding a key with no value, and that
    /// nothing is emitted, never a key without a value.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenConverterWritesNoValue_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            TomlSerializer.Serialize(buffer, new SilentMemberModel());
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that members marked with <see cref="Bodu.Text.Serialization.IgnoreAttribute" /> are skipped without a
    /// converter being resolved for them, so a delegate member, and a dictionary member holding a delegate, do not stop
    /// the rest of the model from being written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIgnoredMemberHasUnsupportedType_ShouldSkipIt()
    {
        var model = new IgnoredUnsupportedMembersModel
        {
            Func = () => { },
            Map = new Dictionary<string, object> { ["f"] = new Action(() => { }) },
        };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Str = \"a\"\n", text);
    }

    /// <summary>
    /// Verifies that serializing a model with a member of a type TOML cannot represent, a delegate, throws
    /// <see cref="NotSupportedException" /> naming the member's type, rather than an exception about one of the
    /// delegate's own members.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberTypeIsUnsupported_ShouldThrowNotSupportedException()
    {
        var model = new UnsupportedMemberModel { Callback = () => { } };

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains("System.Action", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that under <see cref="IgnoreCondition.WhenWritingDefault" /> a string member holding only whitespace is
    /// written, because it is not the member's default, while a <see langword="null" /> string member is omitted.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenWritingDefaultAndStringIsWhitespace_ShouldWriteIt()
    {
        var options = new TomlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingDefault };

        string text = TomlSerializer.Serialize(new NullableMemberModel { Present = " ", Absent = null }, options);

        Assert.AreEqual("Present = \" \"\n", text);
    }

    /// <summary>
    /// Verifies that floats are written with a period as the decimal separator, a whole value keeping its fraction,
    /// when the current culture uses a comma.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCurrentCultureUsesCommaDecimalSeparator_ShouldWritePeriod()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        string text;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.AreEqual(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "The culture under test uses a comma.");

            text = TomlSerializer.Serialize(new TwoDoublesModel());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.AreEqual("A = 2.0\nB = 1.5\n", text);
    }

    /// <summary>
    /// Verifies that an anonymous type is written with its property names, not the names of the compiler's backing
    /// fields.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsAnonymousType_ShouldUsePropertyNames()
    {
        string text = TomlSerializer.Serialize(new { Foo = "bar" });

        Assert.AreEqual("Foo = \"bar\"\n", text);
    }

    /// <summary>
    /// Verifies that a public field a derived type hides with <see langword="new" /> and a field of another type is
    /// written once, from the derived declaration, rather than failing as two members mapped to one key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenFieldIsHiddenWithNew_ShouldWriteTheDerivedField()
    {
        string text = TomlSerializer.Serialize(new HiddenFieldDerivedModel(), new TomlSerializerOptions { IncludeFields = true });

        Assert.AreEqual("Value = \"derived\"\n", text);
    }

    /// <summary>
    /// Verifies that a property a derived type hides with <see langword="new" /> and a property of another type is
    /// written once, from the derived declaration.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyIsHiddenByAnotherType_ShouldWriteTheDerivedProperty()
    {
        string text = TomlSerializer.Serialize(new HiddenPropertyDerivedModel());

        Assert.AreEqual("Value = \"derived\"\n", text);
    }

    /// <summary>
    /// Verifies that a property hidden at each of two levels of derivation is written once, from the most derived
    /// declaration.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsHiddenTwoLevelsDown_ShouldWriteTheMostDerivedMember()
    {
        string text = TomlSerializer.Serialize(new HiddenTwiceDerivedModel());

        Assert.AreEqual("Value = \"derived\"\n", text);
    }

    /// <summary>
    /// Verifies that serializing a member whose type is a generic delegate, <see cref="Func{TResult}" /> of
    /// <see cref="int" />, throws <see cref="NotSupportedException" /> naming that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsAFunc_ShouldThrowNotSupportedException()
    {
        var model = new ValueModel<Func<int>> { Value = () => 1 };

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(Func<int>).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that serializing a member whose type is the reflection type <see cref="Type" /> throws
    /// <see cref="NotSupportedException" /> naming that type.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsAType_ShouldThrowNotSupportedException()
    {
        var model = new ValueModel<Type> { Value = typeof(int) };

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains(typeof(Type).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// A model whose only member is written by <see cref="SilentStringConverter" />, which writes nothing.
    /// </summary>
    private sealed class SilentMemberModel
    {
        /// <summary>
        /// Gets or sets the value the converter fails to write.
        /// </summary>
        /// <value>The value; <c>x</c> by default.</value>
        [Converter(typeof(SilentStringConverter))]
        public string Value { get; set; } = "x";
    }

    /// <summary>
    /// A converter whose <see cref="Write" /> writes no value, leaving the key the serializer wrote without one.
    /// </summary>
    private sealed class SilentStringConverter
        : TomlConverter<string>
    {
        /// <inheritdoc />
        public override string Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            reader.GetString();

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, string value, TomlSerializerOptions options)
        {
            // Deliberately writes nothing, as a custom converter returning no value does.
        }
    }

    /// <summary>
    /// A model whose ignored members are of types TOML cannot represent.
    /// </summary>
    private sealed class IgnoredUnsupportedMembersModel
    {
        /// <summary>
        /// Gets or sets the string member, the only one written.
        /// </summary>
        /// <value>The string; <c>a</c> by default.</value>
        public string Str { get; set; } = "a";

        /// <summary>
        /// Gets or sets an ignored delegate member.
        /// </summary>
        /// <value>The delegate, or <see langword="null" />.</value>
        [Bodu.Text.Serialization.Ignore]
        public Action? Func { get; set; }

        /// <summary>
        /// Gets or sets an ignored dictionary member whose values may be delegates.
        /// </summary>
        /// <value>The dictionary.</value>
        [Bodu.Text.Serialization.Ignore]
        public Dictionary<string, object> Map { get; set; } = [];
    }

    /// <summary>
    /// A model with a member of a type TOML cannot represent.
    /// </summary>
    private sealed class UnsupportedMemberModel
    {
        /// <summary>
        /// Gets or sets the delegate member.
        /// </summary>
        /// <value>The delegate, or <see langword="null" />.</value>
        public Action? Callback { get; set; }
    }

    /// <summary>
    /// A model with two <see cref="double" /> members, one of them a whole number.
    /// </summary>
    private sealed class TwoDoublesModel
    {
        /// <summary>
        /// Gets or sets the whole-number value.
        /// </summary>
        /// <value>The value; 2.0 by default.</value>
        public double A { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets the fractional value.
        /// </summary>
        /// <value>The value; 1.5 by default.</value>
        public double B { get; set; } = 1.5;
    }
}
