// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Nullables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that nullable value types round trip, exercising the converter that wraps the underlying value
/// converter and short-circuits on a null scalar.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a present nullable value binds to its underlying type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullableHasValue_ShouldBindUnderlyingValue()
    {
        NullableHolder holder = YamlSerializer.Deserialize<NullableHolder>("Count: 42\nRatio: 1.5\nActive: true\n")!;

        Assert.AreEqual(42, holder.Count);
        Assert.AreEqual(1.5d, holder.Ratio);
        Assert.AreEqual(true, holder.Active);
    }

    /// <summary>
    /// Verifies that an explicit YAML null binds to <see langword="null" /> rather than to the underlying type's
    /// default, so a missing value stays distinguishable from a zero.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullableIsNull_ShouldBindNullNotDefault()
    {
        NullableHolder holder = YamlSerializer.Deserialize<NullableHolder>("Count: ~\nRatio: null\nActive:\n")!;

        Assert.IsNull(holder.Count);
        Assert.IsNull(holder.Ratio);
        Assert.IsNull(holder.Active);
    }

    /// <summary>
    /// Verifies that a nullable value round trips through serialize and deserialize.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableHasValue_ShouldRoundTrip()
    {
        var original = new NullableHolder { Count = 7, Ratio = 2.5d, Active = false };

        string yaml = YamlSerializer.Serialize(original);
        NullableHolder actual = YamlSerializer.Deserialize<NullableHolder>(yaml)!;

        Assert.AreEqual(original.Count, actual.Count);
        Assert.AreEqual(original.Ratio, actual.Ratio);
        Assert.AreEqual(original.Active, actual.Active);
    }

    /// <summary>
    /// Verifies that a null nullable round trips as null rather than being dropped or defaulted.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableIsNull_ShouldRoundTripAsNull()
    {
        var original = new NullableHolder();

        string yaml = YamlSerializer.Serialize(original);
        NullableHolder actual = YamlSerializer.Deserialize<NullableHolder>(yaml)!;

        Assert.IsNull(actual.Count);
        Assert.IsNull(actual.Ratio);
        Assert.IsNull(actual.Active);
    }

    /// <summary>
    /// A holder exercising the nullable converter over several underlying value types.
    /// </summary>
    private sealed class NullableHolder
    {
        /// <summary>Gets or sets a nullable integer.</summary>
        /// <value>The count.</value>
        public int? Count { get; set; }

        /// <summary>Gets or sets a nullable float.</summary>
        /// <value>The ratio.</value>
        public double? Ratio { get; set; }

        /// <summary>Gets or sets a nullable boolean.</summary>
        /// <value>Whether the holder is active.</value>
        public bool? Active { get; set; }
    }
}
