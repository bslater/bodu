// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.Scalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetWriterTests
{
    /// <summary>
    /// Verifies that an unsigned 16-bit integer round-trips with its exact CLR type.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarUInt16_ShouldRoundTrip() =>
        Assert.AreEqual((ushort)54321, RoundTrip(new OlePropertyValue { Type = OlePropertyType.UInt16, Value = (ushort)54321 }).GetValueOrDefault<ushort>());

    /// <summary>
    /// Verifies that a signed 8-bit integer round-trips with its exact CLR type.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarInt8_ShouldRoundTrip() =>
        Assert.AreEqual((sbyte)-100, RoundTrip(new OlePropertyValue { Type = OlePropertyType.Int8, Value = (sbyte)-100 }).GetValueOrDefault<sbyte>());

    /// <summary>
    /// Verifies that an unsigned 8-bit integer round-trips with its exact CLR type.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarUInt8_ShouldRoundTrip() =>
        Assert.AreEqual((byte)200, RoundTrip(new OlePropertyValue { Type = OlePropertyType.UInt8, Value = (byte)200 }).GetValueOrDefault<byte>());

    /// <summary>
    /// Verifies that an unsigned 32-bit integer round-trips with its exact CLR type.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarUInt32_ShouldRoundTrip() =>
        Assert.AreEqual(3_000_000_000u, RoundTrip(new OlePropertyValue { Type = OlePropertyType.UInt32, Value = 3_000_000_000u }).GetValueOrDefault<uint>());

    /// <summary>
    /// Verifies that an unsigned 64-bit integer round-trips with its exact CLR type.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarUInt64_ShouldRoundTrip() =>
        Assert.AreEqual(ulong.MaxValue, RoundTrip(new OlePropertyValue { Type = OlePropertyType.UInt64, Value = ulong.MaxValue }).GetValueOrDefault<ulong>());

    /// <summary>
    /// Verifies that a 32-bit floating-point value round-trips with its exact CLR type.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarFloat32_ShouldRoundTrip() =>
        Assert.AreEqual(1.25f, RoundTrip(new OlePropertyValue { Type = OlePropertyType.Float32, Value = 1.25f }).GetValueOrDefault<float>());

    /// <summary>
    /// Verifies that a currency value round-trips through its fixed-point 10,000ths encoding.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarCurrency_ShouldRoundTrip() =>
        Assert.AreEqual(123.4567m, RoundTrip(new OlePropertyValue { Type = OlePropertyType.Currency, Value = 123.4567m }).GetValueOrDefault<decimal>());

    /// <summary>
    /// Verifies that an OLE automation date round-trips as the same UTC point in time.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarDate_ShouldRoundTripAsUtc()
    {
        var date = new DateTimeOffset(2021, 5, 6, 0, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(date, RoundTrip(new OlePropertyValue { Type = OlePropertyType.Date, Value = date }).AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a <c>VT_EMPTY</c> scalar round-trips as a null value of type <see cref="OlePropertyType.Empty" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarEmpty_ShouldRoundTripAsNullValue()
    {
        OlePropertyValue parsed = RoundTrip(new OlePropertyValue { Type = OlePropertyType.Empty });

        Assert.AreEqual(OlePropertyType.Empty, parsed.Type);
        Assert.IsNull(parsed.Value);
    }

    /// <summary>
    /// Verifies that a self-describing scalar variant infers its inner type from the CLR value and round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarVariant_ShouldInferInnerTypeAndRoundTrip()
    {
        OlePropertyValue parsed = RoundTrip(new OlePropertyValue { Type = OlePropertyType.Variant, Value = 42 });

        Assert.AreEqual(OlePropertyType.Variant, parsed.Type);
        Assert.AreEqual(42, parsed.GetValueOrDefault<int>());
    }

    /// <summary>
    /// Verifies that a scalar of a type the reader also rejects throws
    /// <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenScalarTypeUnsupported_ShouldThrowCompoundFileSerializationException()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(10, new OlePropertyValue { Type = (OlePropertyType)9, Value = 1 });
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = set.ToArray();
        });
    }
}
