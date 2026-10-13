// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class InvariantScalarCodecTests
{
    /// <summary>
    /// Verifies that empty text converts to <see langword="null" /> for a nullable value type.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNullableTextIsEmpty_ForConfigurationPolicy_ShouldReturnNull()
    {
        object? value = InvariantScalarCodec.Parse(
            string.Empty,
            typeof(int?),
            InvariantScalarCodec.Policy.Configuration);

        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that white space is not an empty value under the configuration policy, so a nullable number throws
    /// <see cref="FormatException" /> for it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNullableTextIsWhiteSpace_ForConfigurationPolicy_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = InvariantScalarCodec.Parse("  ", typeof(int?), InvariantScalarCodec.Policy.Configuration);
        });
    }

    /// <summary>
    /// Verifies that a decimal written with the invariant separator parses to its value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDecimalUsesInvariantSeparator_ForConfigurationPolicy_ShouldReturnTheValue()
    {
        object? value = InvariantScalarCodec.Parse("1.25", typeof(decimal), InvariantScalarCodec.Policy.Configuration);

        Assert.AreEqual(1.25m, value);
    }

    /// <summary>
    /// Verifies that the text written for a <see cref="DateOnly" />, a <see cref="TimeOnly" /> with a fraction of a
    /// second, the extremes of <see cref="Int128" /> and <see cref="UInt128" />, a <see cref="Half" /> and a
    /// <see cref="System.Numerics.BigInteger" /> beyond 128 bits reads back as the value written.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTheTextIsAnExtendedScalarAsWritten_ForConfigurationPolicy_ShouldReturnTheValue()
    {
        object[] values =
        [
            new DateOnly(2026, 1, 2),
            new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567)),
            Int128.MinValue,
            UInt128.MaxValue,
            (Half)0.1,
            -(System.Numerics.BigInteger.Pow(10, 40) + 1),
        ];

        foreach (object value in values)
        {
            string text = InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Configuration);

            object? parsed = InvariantScalarCodec.Parse(text, value.GetType(), InvariantScalarCodec.Policy.Configuration);

            Assert.AreEqual(value, parsed, text);
        }
    }

    /// <summary>
    /// Verifies that the text written for a <see cref="nint" /> and a <see cref="nuint" /> at their extremes reads back
    /// as the value written.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTheTextIsANativeSizedIntegerAsWritten_ForConfigurationPolicy_ShouldReturnTheValue()
    {
        object[] values = [nint.MinValue, nint.MaxValue, nuint.MinValue, nuint.MaxValue];

        foreach (object value in values)
        {
            string text = InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Configuration);

            object? parsed = InvariantScalarCodec.Parse(text, value.GetType(), InvariantScalarCodec.Policy.Configuration);

            Assert.AreEqual(value, parsed, text);
        }
    }

    /// <summary>
    /// Verifies that the text written for a <see cref="DateTime" /> of each kind but local, and for a
    /// <see cref="DateTimeOffset" />, with fractions of a second, reads back with every tick, the kind and the offset.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTheTextIsADateAndTimeAsWritten_ForConfigurationPolicy_ShouldKeepEveryTickTheKindAndTheOffset()
    {
        var utc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(8910123);
        var unspecified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified).AddTicks(1);
        var offset = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5)).AddTicks(1234567);

        foreach (DateTime value in new[] { utc, unspecified })
        {
            string text = InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Configuration);

            var parsed = (DateTime)InvariantScalarCodec.Parse(text, typeof(DateTime), InvariantScalarCodec.Policy.Configuration)!;

            Assert.AreEqual(value.Ticks, parsed.Ticks, text);
            Assert.AreEqual(value.Kind, parsed.Kind, text);
        }

        string offsetText = InvariantScalarCodec.Format(offset, InvariantScalarCodec.Policy.Configuration);
        var parsedOffset = (DateTimeOffset)InvariantScalarCodec.Parse(offsetText, typeof(DateTimeOffset), InvariantScalarCodec.Policy.Configuration)!;

        Assert.AreEqual(offset.Ticks, parsedOffset.Ticks, offsetText);
        Assert.AreEqual(offset.Offset, parsedOffset.Offset, offsetText);
    }
}
