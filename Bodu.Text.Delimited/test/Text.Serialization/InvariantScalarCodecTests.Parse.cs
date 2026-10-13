// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization;

public sealed partial class InvariantScalarCodecTests
{
    /// <summary>
    /// Verifies that a cell of white space holds no value, so a nullable number converts it to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNullableTextIsWhiteSpace_ForDelimitedPolicy_ShouldReturnNull()
    {
        object? value = InvariantScalarCodec.Parse(" \t ", typeof(decimal?), InvariantScalarCodec.Policy.Delimited);

        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that one space is a character, so a nullable <see cref="char" /> reads it rather than treating the cell
    /// as blank.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNullableCharIsOneSpace_ForDelimitedPolicy_ShouldReturnTheSpace()
    {
        object? value = InvariantScalarCodec.Parse(" ", typeof(char?), InvariantScalarCodec.Policy.Delimited);

        Assert.AreEqual(' ', value);
    }

    /// <summary>
    /// Verifies that two spaces are neither a blank cell nor one character for a nullable <see cref="char" />, so
    /// conversion throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNullableCharIsTwoSpaces_ForDelimitedPolicy_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = InvariantScalarCodec.Parse("  ", typeof(char?), InvariantScalarCodec.Policy.Delimited);
        });
    }

    /// <summary>
    /// Verifies that round-trip text reads back as the instant it was written from, ticks and kind included.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDateTimeIsRoundTripText_ForDelimitedPolicy_ShouldReturnTheInstant()
    {
        string text = s_instant.ToString("O", CultureInfo.InvariantCulture);

        object? value = InvariantScalarCodec.Parse(text, typeof(DateTime), InvariantScalarCodec.Policy.Delimited);

        Assert.AreEqual(s_instant, value);
        Assert.AreEqual(DateTimeKind.Utc, ((DateTime)value!).Kind);
    }

    /// <summary>
    /// Verifies that the text written for a <see cref="DateOnly" />, a <see cref="TimeOnly" /> with a fraction of a
    /// second, the extremes of <see cref="Int128" /> and <see cref="UInt128" />, a <see cref="Half" /> and a
    /// <see cref="System.Numerics.BigInteger" /> beyond 128 bits reads back as the value written.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTheTextIsAnExtendedScalarAsWritten_ForDelimitedPolicy_ShouldReturnTheValue()
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
            string text = InvariantScalarCodec.Format(value, InvariantScalarCodec.Policy.Delimited);

            object? parsed = InvariantScalarCodec.Parse(text, value.GetType(), InvariantScalarCodec.Policy.Delimited);

            Assert.AreEqual(value, parsed, text);
        }
    }
}
