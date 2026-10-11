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
    /// Verifies that empty text converts to <see langword="null" /> for a nullable value type.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNullableTextIsEmpty_ForConfigurationPolicy_ShouldReturnNull()
    {
        object? value = InvariantScalarCodec.Parse(
            string.Empty,
            typeof(double?),
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
            _ = InvariantScalarCodec.Parse("  ", typeof(double?), InvariantScalarCodec.Policy.Configuration);
        });
    }

    /// <summary>
    /// Verifies that round-trip text with a UTC offset parses to the instant and offset it states.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDateTimeOffsetHasOffset_ForConfigurationPolicy_ShouldKeepTheOffset()
    {
        const string Text = "2026-01-02T03:04:05.1234567+10:00";

        object? value = InvariantScalarCodec.Parse(
            Text,
            typeof(DateTimeOffset),
            InvariantScalarCodec.Policy.Configuration);

        Assert.AreEqual(DateTimeOffset.Parse(Text, CultureInfo.InvariantCulture), value);
    }
}
