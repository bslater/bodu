// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.Vectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetWriterTests
{
    /// <summary>
    /// Round-trips a vector value through write and parse and asserts the parsed elements match.
    /// </summary>
    /// <param name="type">The vector element type.</param>
    /// <param name="items">The vector elements to write and expect back.</param>
    private static void AssertVectorRoundTrip(OlePropertyType type, object?[] items)
    {
        OlePropertyValue parsed = RoundTrip(new OlePropertyValue { Type = type, IsVector = true, Value = items });

        Assert.AreEqual(type, parsed.Type);
        Assert.IsTrue(parsed.IsVector);
        CollectionAssert.AreEqual(items, (object?[])parsed.Value!);
    }

    /// <summary>
    /// Verifies that a vector of 16-bit integers round-trips, exercising per-element alignment padding.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfInt16_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Int16, [(short)1, (short)-2, (short)3]);

    /// <summary>
    /// Verifies that a vector of 32-bit integers round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfInt32_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Int32, [1, -2, 3, int.MaxValue]);

    /// <summary>
    /// Verifies that a vector of unsigned 32-bit integers round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfUInt32_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.UInt32, [1u, 3_000_000_000u]);

    /// <summary>
    /// Verifies that a vector of 64-bit integers round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfInt64_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Int64, [long.MinValue, 0L, long.MaxValue]);

    /// <summary>
    /// Verifies that a vector of unsigned 64-bit integers round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfUInt64_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.UInt64, [0UL, ulong.MaxValue]);

    /// <summary>
    /// Verifies that a vector of signed 8-bit integers round-trips, exercising one-byte elements padded to
    /// four-byte boundaries.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfInt8_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Int8, [(sbyte)-1, (sbyte)2, (sbyte)-3]);

    /// <summary>
    /// Verifies that a vector of unsigned 8-bit integers round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfUInt8_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.UInt8, [(byte)0, (byte)128, (byte)255]);

    /// <summary>
    /// Verifies that a vector of 32-bit floating-point values round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfFloat32_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Float32, [1.25f, -0.5f]);

    /// <summary>
    /// Verifies that a vector of 64-bit floating-point values round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfFloat64_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Float64, [3.14159, -2.5]);

    /// <summary>
    /// Verifies that a vector of booleans round-trips through the -1/0 encoding.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfBoolean_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Boolean, [true, false, true]);

    /// <summary>
    /// Verifies that a vector of currency values round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfCurrency_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Currency, [1.5m, -0.0001m]);

    /// <summary>
    /// Verifies that a vector of FILETIME values round-trips as raw ticks.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfFileTime_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.FileTime, [116444736000000000L, 132537600000000000L]);

    /// <summary>
    /// Verifies that a vector of OLE automation dates round-trips as UTC points in time.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfDate_ShouldRoundTrip() =>
        AssertVectorRoundTrip(
            OlePropertyType.Date,
            [new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2021, 6, 15, 12, 0, 0, TimeSpan.Zero)]);

    /// <summary>
    /// Verifies that a vector of ANSI strings round-trips, exercising odd lengths that require element padding —
    /// the shape of a document-summary <c>TitlesOfParts</c> vector.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfAnsiString_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.AnsiString, ["Sheet1", "ab", "A longer worksheet name"]);

    /// <summary>
    /// Verifies that a vector of UTF-16 strings round-trips.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfUnicodeString_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.UnicodeString, ["café", "naïve", "x"]);

    /// <summary>
    /// Verifies that a vector of blobs round-trips each element's bytes.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfBlob_ShouldRoundTrip()
    {
        object?[] items = [new byte[] { 1, 2, 3 }, new byte[] { 4, 5 }];

        OlePropertyValue parsed = RoundTrip(new OlePropertyValue { Type = OlePropertyType.Blob, IsVector = true, Value = items });

        var elements = (object?[])parsed.Value!;
        Assert.HasCount(2, elements);
        CollectionAssert.AreEqual((byte[])items[0]!, (byte[])elements[0]!);
        CollectionAssert.AreEqual((byte[])items[1]!, (byte[])elements[1]!);
    }

    /// <summary>
    /// Verifies that a variant vector infers each element's inner type from its CLR value and round-trips — the
    /// shape of a document-summary <c>HeadingPairs</c> vector (alternating heading string and part count).
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Write_WhenVariantVector_ShouldInferElementTypesAndRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Variant, ["Worksheets", 3, "Charts", 1]);

    /// <summary>
    /// Verifies that a null element inside a variant vector re-emits as <c>VT_EMPTY</c> and round-trips as
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenVariantVectorContainsNull_ShouldRoundTripNullElement() =>
        AssertVectorRoundTrip(OlePropertyType.Variant, ["value", null, 7L]);

    /// <summary>
    /// Verifies that an empty vector round-trips as a zero-element array.
    /// </summary>
    [TestMethod]
    public void Write_WhenEmptyVector_ShouldRoundTrip() =>
        AssertVectorRoundTrip(OlePropertyType.Int32, []);

    /// <summary>
    /// Verifies that a variant vector element with no property-type mapping throws
    /// <see cref="CompoundFileSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Write_WhenVariantVectorElementUnsupported_ShouldThrowCompoundFileSerializationException()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(10, new OlePropertyValue { Type = OlePropertyType.Variant, IsVector = true, Value = new object?[] { TimeSpan.FromHours(1) } });
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = set.ToArray();
        });
    }

    /// <summary>
    /// Verifies that a vector of <c>VT_EMPTY</c> is rejected because its elements consume no bytes, which the
    /// reader treats as malformed.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorOfEmpty_ShouldThrowCompoundFileSerializationException()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(10, new OlePropertyValue { Type = OlePropertyType.Empty, IsVector = true, Value = new object?[] { null } });
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = set.ToArray();
        });
    }

    /// <summary>
    /// Verifies that a vector whose value is not an object array is rejected.
    /// </summary>
    [TestMethod]
    public void Write_WhenVectorValueNotObjectArray_ShouldThrowCompoundFileSerializationException()
    {
        var section = new OlePropertySection(TestFormatId, 1252);
        section.Set(10, new OlePropertyValue { Type = OlePropertyType.Int32, IsVector = true, Value = 42 });
        var set = new OlePropertySet(TestFormatId, Guid.Empty, 1252);
        set.AddSection(section);

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = set.ToArray();
        });
    }
}
