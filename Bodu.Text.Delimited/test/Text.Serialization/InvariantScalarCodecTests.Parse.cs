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
}
