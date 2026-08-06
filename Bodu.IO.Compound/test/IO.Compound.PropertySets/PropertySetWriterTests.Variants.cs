// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.Variants.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies the writer's variant-type inference, date encoding, and code-page selection.
/// </summary>
public partial class PropertySetWriterTests
{
    /// <summary>
    /// Verifies that a variant vector infers the inner type of every supported CLR value shape. A variant element
    /// carries no declared type, so the writer must derive one from the runtime value or the property is unwritable.
    /// </summary>
    [TestMethod]
    public void Write_WhenVariantVectorSpansEveryScalarShape_ShouldInferEachElementType()
    {
        var date = new DateTimeOffset(2021, 5, 6, 7, 8, 9, TimeSpan.Zero);
        object?[] items =
        [
            true, (sbyte)-1, (byte)2, (short)-3, (ushort)4, 5, 6u, -7L, 8UL, 9.5f, 10.5d, 11.5m, "twelve", date,
        ];

        OlePropertyValue parsed = RoundTrip(new OlePropertyValue
        {
            Type = OlePropertyType.Variant,
            IsVector = true,
            Value = items,
        });

        CollectionAssert.AreEqual(items, (object?[])parsed.Value!);
    }

    /// <summary>
    /// Verifies that a blob element inside a variant vector round-trips its bytes. Blobs are compared element-wise
    /// because the round-trip guarantees value identity, not reference identity.
    /// </summary>
    [TestMethod]
    public void Write_WhenVariantVectorContainsBlob_ShouldRoundTripBytes()
    {
        object?[] items = [new byte[] { 1, 2, 3 }];

        OlePropertyValue parsed = RoundTrip(new OlePropertyValue
        {
            Type = OlePropertyType.Variant,
            IsVector = true,
            Value = items,
        });

        var elements = (object?[])parsed.Value!;
        Assert.HasCount(1, elements);
        CollectionAssert.AreEqual((byte[])items[0]!, (byte[])elements[0]!);
    }

    /// <summary>
    /// Verifies that a date property accepts a <see cref="DateTime" /> as well as a
    /// <see cref="DateTimeOffset" />, normalising it to UTC. The reader always decodes a date to
    /// <see cref="DateTimeOffset" />, so the round trip is value-identical rather than type-identical.
    /// </summary>
    [TestMethod]
    public void Write_WhenDateValueIsDateTime_ShouldNormaliseToUtc()
    {
        var utc = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        OlePropertyValue parsed = RoundTrip(new OlePropertyValue { Type = OlePropertyType.Date, Value = utc });

        Assert.AreEqual(new DateTimeOffset(utc, TimeSpan.Zero), parsed.AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a local-kind <see cref="DateTime" /> is converted to UTC rather than written as-is, so the same
    /// instant is recovered regardless of the writing machine's time zone.
    /// </summary>
    [TestMethod]
    public void Write_WhenDateValueIsLocalDateTime_ShouldConvertToUtc()
    {
        var local = new DateTime(2021, 5, 6, 7, 8, 9, DateTimeKind.Local);

        OlePropertyValue parsed = RoundTrip(new OlePropertyValue { Type = OlePropertyType.Date, Value = local });

        Assert.AreEqual(new DateTimeOffset(local.ToUniversalTime(), TimeSpan.Zero), parsed.AsDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a date property carrying a non-date value is rejected at write time rather than producing a
    /// container a reader cannot interpret.
    /// </summary>
    [TestMethod]
    public void Write_WhenDateValueIsNotADate_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = RoundTrip(new OlePropertyValue { Type = OlePropertyType.Date, Value = 42 });
        });
    }
}
