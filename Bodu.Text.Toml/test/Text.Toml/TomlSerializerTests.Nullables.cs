// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Nullables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the nullable value model of <see cref="TomlSerializer" />: a present <see cref="System.Nullable{T}" />
/// delegates to the underlying converter, a <see langword="null" /> member is omitted because TOML has no null token,
/// and an absent member reads back as <see langword="null" />. This applies to both nullable value types and reference
/// types.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a present nullable value type serializes through the underlying converter to the same canonical
    /// form as the non-nullable value and round-trips.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullableValueTypePresent_ShouldDelegateToUnderlying()
    {
        string text = TomlSerializer.Serialize(new NullableValueModel { Number = 5, Flag = true, When = new DateOnly(2026, 6, 10) });

        Assert.AreEqual("Number = 5\nFlag = true\nWhen = 2026-06-10\n", text);
    }

    /// <summary>
    /// Verifies that a present nullable value type round-trips to an equal value carrying the same content.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableValueTypePresent_ShouldRoundTrip()
    {
        var original = new NullableValueModel { Number = 42, Flag = false, When = new DateOnly(2026, 1, 1) };

        NullableValueModel roundTripped = TomlSerializer.Deserialize<NullableValueModel>(TomlSerializer.Serialize(original));

        Assert.AreEqual(42, roundTripped.Number);
        Assert.AreEqual(false, roundTripped.Flag);
        Assert.AreEqual(new DateOnly(2026, 1, 1), roundTripped.When);
    }

    /// <summary>
    /// Verifies that a nullable value-type member whose value is <see langword="null" /> is omitted from the output,
    /// because TOML has no null token.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullableValueTypeNull_ShouldOmitMember()
    {
        string text = TomlSerializer.Serialize(new NullableValueModel { Number = 7, Flag = null, When = null });

        Assert.AreEqual("Number = 7\n", text);
    }

    /// <summary>
    /// Verifies that every nullable value-type member being <see langword="null" /> produces an empty document.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenAllNullableValueTypesNull_ShouldEmitEmptyDocument()
    {
        string text = TomlSerializer.Serialize(new NullableValueModel { Number = null, Flag = null, When = null });

        Assert.AreEqual(string.Empty, text);
    }

    /// <summary>
    /// Verifies that a member absent from the document reads back as <see langword="null" /> for a nullable value type.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullableValueTypeAbsent_ShouldReadNull()
    {
        NullableValueModel model = TomlSerializer.Deserialize<NullableValueModel>("Number = 3\n");

        Assert.AreEqual(3, model.Number);
        Assert.IsNull(model.Flag);
        Assert.IsNull(model.When);
    }

    /// <summary>
    /// Verifies that a present nullable value type read from the document carries the stored value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullableValueTypePresent_ShouldReadValue()
    {
        NullableValueModel model = TomlSerializer.Deserialize<NullableValueModel>("Number = 1\nFlag = true\nWhen = 2026-12-31\n");

        Assert.AreEqual(1, model.Number);
        Assert.AreEqual(true, model.Flag);
        Assert.AreEqual(new DateOnly(2026, 12, 31), model.When);
    }

    /// <summary>
    /// Verifies that a present reference-type member serializes to its value and round-trips, while a
    /// <see langword="null" /> reference-type member is omitted and reads back as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableReferenceType_ShouldOmitNullAndRoundTripPresent()
    {
        string text = TomlSerializer.Serialize(new NullableReferenceModel { Present = "here", Absent = null });

        Assert.AreEqual("Present = \"here\"\n", text);

        NullableReferenceModel roundTripped = TomlSerializer.Deserialize<NullableReferenceModel>(text);
        Assert.AreEqual("here", roundTripped.Present);
        Assert.IsNull(roundTripped.Absent);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> nullable value type read directly at a generic member reads as
    /// <see langword="null" /> when absent and as the stored value when present, exercising the
    /// <c>NullableConverter</c> end to end.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullableDoubleMember_ShouldDistinguishPresentFromAbsent()
    {
        Assert.AreEqual(1.5, TomlSerializer.Deserialize<NullableValueModel>("Ratio = 1.5\n").Ratio);
        Assert.IsNull(TomlSerializer.Deserialize<NullableValueModel>("Number = 1\n").Ratio);
    }

    /// <summary>
    /// A model whose members are nullable value types, used to verify present-delegation, null-omission, and
    /// absent-as-null behavior.
    /// </summary>
    private sealed class NullableValueModel
    {
        /// <summary>Gets or sets the nullable integer.</summary>
        /// <returns>The integer, or <see langword="null" />.</returns>
        public int? Number { get; set; }

        /// <summary>Gets or sets the nullable Boolean.</summary>
        /// <returns>The Boolean, or <see langword="null" />.</returns>
        public bool? Flag { get; set; }

        /// <summary>Gets or sets the nullable local date.</summary>
        /// <returns>The date, or <see langword="null" />.</returns>
        public DateOnly? When { get; set; }

        /// <summary>Gets or sets the nullable floating-point ratio.</summary>
        /// <returns>The ratio, or <see langword="null" />.</returns>
        public double? Ratio { get; set; }
    }

    /// <summary>
    /// A model with nullable reference-type members, used to verify null-omission and present round-tripping.
    /// </summary>
    private sealed class NullableReferenceModel
    {
        /// <summary>Gets or sets the present member.</summary>
        /// <returns>The present value, or <see langword="null" />.</returns>
        public string? Present { get; set; }

        /// <summary>Gets or sets the absent member.</summary>
        /// <returns>The absent value, or <see langword="null" />.</returns>
        public string? Absent { get; set; }
    }
}
