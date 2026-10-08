// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Nullables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that nullable value types, structs and flags enumerations included, round trip, exercising the converter
/// that wraps the underlying value converter and short-circuits on a null scalar.
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
    /// Verifies that a nullable struct round-trips: a value holding <c>Value = 2</c> reads back equal, and a null value
    /// reads back null.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableStructHasValue_ShouldRoundTrip()
    {
        ExampleStruct? original = new ExampleStruct { Value = 2 };
        ExampleStruct? none = null;

        ExampleStruct? restored = YamlSerializer.Deserialize<ExampleStruct?>(YamlSerializer.Serialize(original));
        ExampleStruct? restoredNone = YamlSerializer.Deserialize<ExampleStruct?>(YamlSerializer.Serialize(none));

        Assert.AreEqual(original, restored);
        Assert.IsNull(restoredNone);
    }

    /// <summary>
    /// Verifies that a flags enumeration round-trips as the enumeration and as its nullable form, for one member and
    /// for a combination of members, and that a null nullable value round-trips as null.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableEnumHasFlagsValue_ShouldRoundTrip()
    {
        PermissionFlags[] values = [PermissionFlags.Read, PermissionFlags.Read | PermissionFlags.Write];
        PermissionFlags? none = null;

        foreach (PermissionFlags value in values)
        {
            PermissionFlags? nullable = value;

            Assert.AreEqual(value, YamlSerializer.Deserialize<PermissionFlags>(YamlSerializer.Serialize(value)), $"{value} as the enumeration.");
            Assert.AreEqual(nullable, YamlSerializer.Deserialize<PermissionFlags?>(YamlSerializer.Serialize(nullable)), $"{value} as its nullable form.");
        }

        Assert.IsNull(YamlSerializer.Deserialize<PermissionFlags?>(YamlSerializer.Serialize(none)));
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

    /// <summary>
    /// A struct carried as a nullable value.
    /// </summary>
    private struct ExampleStruct
    {
        /// <summary>Gets or sets the value.</summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }
}
