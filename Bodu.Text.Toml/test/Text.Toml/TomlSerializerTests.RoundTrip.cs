// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that every value the <see cref="TomlSerializer" /> can emit is read back by the same serializer to an
/// equal value — the serializer must never produce a document that its own <c>Deserialize</c> rejects or decodes to a
/// different value.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with <see cref="DateTimeKind.Utc" /> round-trips through
    /// serialization to the same instant.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeKindUtc_ShouldRoundTripToSameInstant()
    {
        var original = new DateTimeModel { Stamp = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Utc) };

        string text = TomlSerializer.Serialize(original);
        DateTimeModel roundTripped = TomlSerializer.Deserialize<DateTimeModel>(text);

        Assert.AreEqual(original.Stamp.ToUniversalTime(), roundTripped.Stamp.ToUniversalTime());
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> with <see cref="DateTimeKind.Local" /> round-trips through
    /// serialization to the same instant.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeKindLocal_ShouldRoundTripToSameInstant()
    {
        var original = new DateTimeModel { Stamp = new DateTime(2026, 6, 10, 9, 30, 0, DateTimeKind.Local) };

        string text = TomlSerializer.Serialize(original);
        DateTimeModel roundTripped = TomlSerializer.Deserialize<DateTimeModel>(text);

        Assert.AreEqual(original.Stamp.ToUniversalTime(), roundTripped.Stamp.ToUniversalTime());
    }

    /// <summary>
    /// Verifies that <see cref="long.MinValue" /> and <see cref="long.MaxValue" /> round-trip through serialization
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt64Extremes_ShouldRoundTripUnchanged()
    {
        var original = new Int64ExtremesModel { Minimum = long.MinValue, Maximum = long.MaxValue };

        string text = TomlSerializer.Serialize(original);
        Int64ExtremesModel roundTripped = TomlSerializer.Deserialize<Int64ExtremesModel>(text);

        Assert.AreEqual(long.MinValue, roundTripped.Minimum);
        Assert.AreEqual(long.MaxValue, roundTripped.Maximum);
    }

    /// <summary>
    /// Verifies that non-finite and extreme double values round-trip through serialization unchanged, including the
    /// sign of negative zero.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDoubleExtremes_ShouldRoundTripUnchanged()
    {
        var original = new DoubleExtremesModel
        {
            PositiveInfinity = double.PositiveInfinity,
            NegativeInfinity = double.NegativeInfinity,
            NotANumber = double.NaN,
            Epsilon = double.Epsilon,
            Large = 1e308,
            NegativeZero = -0.0,
        };

        string text = TomlSerializer.Serialize(original);
        DoubleExtremesModel roundTripped = TomlSerializer.Deserialize<DoubleExtremesModel>(text);

        Assert.IsTrue(double.IsPositiveInfinity(roundTripped.PositiveInfinity));
        Assert.IsTrue(double.IsNegativeInfinity(roundTripped.NegativeInfinity));
        Assert.IsTrue(double.IsNaN(roundTripped.NotANumber));
        Assert.AreEqual(double.Epsilon, roundTripped.Epsilon);
        Assert.AreEqual(1e308, roundTripped.Large);
        Assert.IsTrue(double.IsNegative(roundTripped.NegativeZero));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTimeOffset" /> carrying maximum tick precision round-trips through
    /// serialization unchanged.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDateTimeOffsetAtTickPrecision_ShouldRoundTripUnchanged()
    {
        var original = new DateTimeOffsetModel
        {
            Stamp = new DateTimeOffset(2026, 6, 10, 9, 30, 15, TimeSpan.FromHours(10)).AddTicks(1234567),
        };

        string text = TomlSerializer.Serialize(original);
        DateTimeOffsetModel roundTripped = TomlSerializer.Deserialize<DateTimeOffsetModel>(text);

        Assert.AreEqual(original.Stamp, roundTripped.Stamp);
    }

    /// <summary>
    /// Verifies that serializing a type whose members map to the same wire name throws
    /// <see cref="TomlSerializationException" />, because emitting both would produce a duplicate-key document that
    /// the serializer's own reader rejects.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMembersShareWireName_ShouldThrowTomlSerializationException()
    {
        var model = new DuplicateWireNameModel { First = 1, Second = 2 };

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// Verifies that serializing a model whose extension-data dictionary contains a key equal to a declared member's
    /// wire name throws <see cref="TomlSerializationException" />, because emitting both would produce a
    /// duplicate-key document that the serializer's own reader rejects.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionDataKeyCollidesWithMember_ShouldThrowTomlSerializationException()
    {
        var model = new CollidingExtensionDataModel
        {
            Name = "declared",
            Extra = new Dictionary<string, TomlNode?> { ["Name"] = TomlValue.Create("overflow") },
        };

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// A model carrying a single <see cref="DateTime" /> member.
    /// </summary>
    private sealed class DateTimeModel
    {
        /// <summary>
        /// Gets or sets the date-time under test.
        /// </summary>
        /// <value>The date-time value.</value>
        public DateTime Stamp { get; set; }
    }

    /// <summary>
    /// A model carrying a single <see cref="DateTimeOffset" /> member.
    /// </summary>
    private sealed class DateTimeOffsetModel
    {
        /// <summary>
        /// Gets or sets the date-time offset under test.
        /// </summary>
        /// <value>The date-time offset value.</value>
        public DateTimeOffset Stamp { get; set; }
    }

    /// <summary>
    /// A model carrying the <see cref="long" /> boundary values.
    /// </summary>
    private sealed class Int64ExtremesModel
    {
        /// <summary>
        /// Gets or sets the minimum value member.
        /// </summary>
        /// <value>The minimum value.</value>
        public long Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value member.
        /// </summary>
        /// <value>The maximum value.</value>
        public long Maximum { get; set; }
    }

    /// <summary>
    /// A model carrying non-finite and extreme <see cref="double" /> values.
    /// </summary>
    private sealed class DoubleExtremesModel
    {
        /// <summary>
        /// Gets or sets the positive-infinity member.
        /// </summary>
        /// <value>The positive-infinity value.</value>
        public double PositiveInfinity { get; set; }

        /// <summary>
        /// Gets or sets the negative-infinity member.
        /// </summary>
        /// <value>The negative-infinity value.</value>
        public double NegativeInfinity { get; set; }

        /// <summary>
        /// Gets or sets the not-a-number member.
        /// </summary>
        /// <value>The NaN value.</value>
        public double NotANumber { get; set; }

        /// <summary>
        /// Gets or sets the smallest-positive-subnormal member.
        /// </summary>
        /// <value>The epsilon value.</value>
        public double Epsilon { get; set; }

        /// <summary>
        /// Gets or sets the near-maximum-magnitude member.
        /// </summary>
        /// <value>The large value.</value>
        public double Large { get; set; }

        /// <summary>
        /// Gets or sets the negative-zero member.
        /// </summary>
        /// <value>The negative-zero value.</value>
        public double NegativeZero { get; set; }
    }

    /// <summary>
    /// A model whose two members are mapped to the same wire name via <see cref="TomlPropertyNameAttribute" />.
    /// </summary>
    private sealed class DuplicateWireNameModel
    {
        /// <summary>
        /// Gets or sets the first member mapped to the shared wire name.
        /// </summary>
        /// <value>The first value.</value>
        [TomlPropertyName("shared")]
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the second member mapped to the shared wire name.
        /// </summary>
        /// <value>The second value.</value>
        [TomlPropertyName("shared")]
        public int Second { get; set; }
    }

    /// <summary>
    /// A model whose extension-data dictionary can carry a key that collides with the declared member.
    /// </summary>
    private sealed class CollidingExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension-data member.
        /// </summary>
        /// <value>The overflow entries, or <see langword="null" /> when none exist.</value>
        [TomlExtensionData]
        public Dictionary<string, TomlNode?>? Extra { get; set; }
    }
}
