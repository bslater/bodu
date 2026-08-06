// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertyValueTests.Coercion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies the integral and date coercions of <see cref="OlePropertyValue" /> across every underlying value shape a
/// parsed property set can produce.
/// </summary>
/// <remarks>
/// <see cref="OlePropertyValue.AsInt32" /> and <see cref="OlePropertyValue.AsInt64" /> dispatch on the runtime type
/// of <see cref="OlePropertyValue.Value" />, not on <see cref="OlePropertyValue.Type" />. The reader decodes each OLE
/// variant type to a distinct CLR type - <c>VT_I1</c> to <see cref="sbyte" />, <c>VT_UI4</c> to <see cref="uint" />
/// and so on - so a document written by another producer can present any of them where a count is expected. Each is
/// paired with its variant type here so the intent stays readable.
/// </remarks>
public partial class OlePropertyValueTests
{
    /// <summary>
    /// Verifies that <see cref="OlePropertyValue.AsInt32" /> widens every signed and unsigned integral shape a
    /// property set can carry.
    /// </summary>
    /// <param name="type">The OLE variant type the value was decoded from.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="expected">The expected 32-bit result.</param>
    [TestMethod]
    [DynamicData(nameof(IntegralValues))]
    public void AsInt32_WhenValueIsIntegral_ShouldReturnWidenedValue(OlePropertyType type, object value, int expected)
    {
        var property = new OlePropertyValue { Type = type, Value = value };

        Assert.AreEqual(expected, property.AsInt32());
    }

    /// <summary>
    /// Verifies that <see cref="OlePropertyValue.AsInt64" /> widens every signed and unsigned integral shape a
    /// property set can carry.
    /// </summary>
    /// <param name="type">The OLE variant type the value was decoded from.</param>
    /// <param name="value">The decoded value.</param>
    /// <param name="expected">The expected 32-bit result, which is also the expected 64-bit result for these rows.</param>
    [TestMethod]
    [DynamicData(nameof(IntegralValues))]
    public void AsInt64_WhenValueIsIntegral_ShouldReturnWidenedValue(OlePropertyType type, object value, int expected)
    {
        var property = new OlePropertyValue { Type = type, Value = value };

        Assert.AreEqual((long)expected, property.AsInt64());
    }

    /// <summary>
    /// Verifies that a non-integral value coerces to <see langword="null" /> rather than throwing, so a caller
    /// reading a mistyped property degrades instead of faulting.
    /// </summary>
    [TestMethod]
    public void AsInt32_WhenValueIsNotIntegral_ShouldReturnNull()
    {
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.AnsiString, Value = "12" }.AsInt32());
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.Float64, Value = 1.5d }.AsInt32());
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.Empty, Value = null }.AsInt32());
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.AnsiString, Value = "12" }.AsInt64());
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.Empty, Value = null }.AsInt64());
    }

    /// <summary>
    /// Verifies that <see cref="OlePropertyValue.AsInt32" /> truncates a value too wide for 32 bits while
    /// <see cref="OlePropertyValue.AsInt64" /> preserves it. This unchecked narrowing is the only behavioural
    /// difference between the two accessors, so it is pinned rather than left to inference.
    /// </summary>
    [TestMethod]
    public void AsInt32_WhenValueExceeds32Bits_ShouldTruncateWhereAsInt64Preserves()
    {
        var property = new OlePropertyValue { Type = OlePropertyType.Int64, Value = 0x1_0000_0000L };

        Assert.AreEqual(0, property.AsInt32());
        Assert.AreEqual(4294967296L, property.AsInt64());
    }

    /// <summary>
    /// Verifies that an unsigned 64-bit value beyond <see cref="long.MaxValue" /> wraps rather than saturating.
    /// </summary>
    [TestMethod]
    public void AsInt64_WhenValueIsUInt64MaxValue_ShouldWrapToNegativeOne()
    {
        var property = new OlePropertyValue { Type = OlePropertyType.UInt64, Value = ulong.MaxValue };

        Assert.AreEqual(-1L, property.AsInt64());
    }

    /// <summary>
    /// Verifies that a FILETIME outside the representable range coerces to <see langword="null" /> rather than
    /// throwing out of <see cref="DateTime.FromFileTimeUtc(long)" />.
    /// </summary>
    /// <param name="ticks">The raw FILETIME tick count.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    [DataRow(long.MaxValue)]
    public void AsDateTimeOffset_WhenFileTimeOutOfRange_ShouldReturnNull(long ticks)
    {
        var property = new OlePropertyValue { Type = OlePropertyType.FileTime, Value = ticks };

        Assert.IsNull(property.AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a value whose type is not a date, and a date-typed value carrying a non-tick payload, both
    /// coerce to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AsDateTimeOffset_WhenValueIsNotADate_ShouldReturnNull()
    {
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.Int64, Value = 123L }.AsDateTimeOffset());
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.FileTime, Value = "not-ticks" }.AsDateTimeOffset());
        Assert.IsNull(new OlePropertyValue { Type = OlePropertyType.FileTime, Value = null }.AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a FILETIME within range decodes to the corresponding UTC instant.
    /// </summary>
    [TestMethod]
    public void AsDateTimeOffset_WhenFileTimeInRange_ShouldDecodeToUtcInstant()
    {
        var expected = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero);
        var property = new OlePropertyValue
        {
            Type = OlePropertyType.FileTime,
            Value = expected.UtcDateTime.ToFileTimeUtc(),
        };

        Assert.AreEqual(expected, property.AsDateTimeOffset());
    }

    /// <summary>
    /// Gets the integral value shapes a parsed property set can produce, paired with the variant type each is
    /// decoded from.
    /// </summary>
    /// <value>The coercion cases.</value>
    public static IEnumerable<object[]> IntegralValues =>
    [
        [OlePropertyType.Int8, (sbyte)-8, -8],
        [OlePropertyType.UInt8, (byte)8, 8],
        [OlePropertyType.Int16, (short)-16, -16],
        [OlePropertyType.UInt16, (ushort)16, 16],
        [OlePropertyType.Int32, -32, -32],
        [OlePropertyType.UInt32, 32u, 32],
        [OlePropertyType.Int64, -64L, -64],
        [OlePropertyType.UInt64, 64UL, 64],
    ];
}
