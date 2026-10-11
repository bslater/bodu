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
}
