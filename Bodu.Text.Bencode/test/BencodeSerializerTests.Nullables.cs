// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Nullables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> maps <see cref="Nullable{T}" /> members of natively supported value
/// types: a present value delegates to the underlying converter, while a <see langword="null" /> value is omitted on
/// write and read back as <see langword="null" /> when absent, because Bencode has no null token.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a non-null <see cref="Nullable{T}" /> integer member serializes through the underlying integer
    /// converter and round-trips to the same value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableIntHasValue_ShouldRoundTripThroughUnderlyingConverter()
    {
        var model = new NullableScalarsModel { Count = 42 };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Counti42ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<NullableScalarsModel>(bytes);
        Assert.AreEqual(42, roundTripped.Count);
        Assert.IsNull(roundTripped.Flag);
    }

    /// <summary>
    /// Verifies that a non-null <see cref="Nullable{T}" /> enumeration member serializes as its member-name byte string
    /// and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableEnumHasValue_ShouldRoundTripAsMemberName()
    {
        var model = new NullableEnumModel { Color = Color.Blue };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d5:Color4:Bluee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<NullableEnumModel>(bytes);
        Assert.AreEqual(Color.Blue, roundTripped.Color);
    }

    /// <summary>
    /// Verifies that a <see cref="Nullable{T}" /> member whose value is <see langword="null" /> is omitted from the
    /// serialized dictionary, because Bencode has no null token.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullableMemberIsNull_ShouldOmitMember()
    {
        var model = new NullableScalarsModel { Count = null };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("de", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a <see cref="Nullable{T}" /> member absent from the document reads back as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullableMemberAbsent_ShouldReadNull()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        var model = BencodeSerializer.Deserialize<NullableScalarsModel>(bytes);

        Assert.IsNull(model.Count);
        Assert.IsNull(model.Flag);
    }

    /// <summary>
    /// Verifies that a mix of present and null <see cref="Nullable{T}" /> members emits only the present member and
    /// round-trips it while leaving the omitted one <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSomeNullableMembersNull_ShouldOmitNullAndRoundTripPresent()
    {
        var model = new NullableScalarsModel { Count = null, Flag = Color.Green };

        byte[] bytes = BencodeSerializer.Serialize(model);
        Assert.AreEqual("d4:Flag5:Greene", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<NullableScalarsModel>(bytes);
        Assert.IsNull(roundTripped.Count);
        Assert.AreEqual(Color.Green, roundTripped.Flag);
    }

    /// <summary>
    /// Verifies that a non-null <see cref="Nullable{T}" /> served by a user converter for an otherwise-unsupported
    /// underlying type round-trips, while a null member is still omitted.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNullableUnderlyingTypeHasUserConverter_ShouldRoundTrip()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BooleanConverter());

        byte[] present = BencodeSerializer.Serialize(new NullableBoolModel { Flag = true }, options);
        Assert.AreEqual("d4:Flagi1ee", Encoding.Latin1.GetString(present));

        var roundTripped = BencodeSerializer.Deserialize<NullableBoolModel>(present, options);
        Assert.IsTrue(roundTripped.Flag);

        byte[] absent = BencodeSerializer.Serialize(new NullableBoolModel { Flag = null }, options);
        Assert.AreEqual("de", Encoding.Latin1.GetString(absent));

        var absentRoundTripped = BencodeSerializer.Deserialize<NullableBoolModel>(absent, options);
        Assert.IsNull(absentRoundTripped.Flag);
    }

    /// <summary>
    /// A model with several <see cref="Nullable{T}" /> members spanning supported value types, used to exercise
    /// present-value round-tripping and null-member omission.
    /// </summary>
    private sealed class NullableScalarsModel
    {
        /// <summary>
        /// Gets or sets the nullable count, mapped to a Bencode integer when present.
        /// </summary>
        /// <returns>The count, or <see langword="null" />.</returns>
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets the nullable color flag, mapped to a member-name byte string when present.
        /// </summary>
        /// <returns>The color flag, or <see langword="null" />.</returns>
        public Color? Flag { get; set; }
    }

    /// <summary>
    /// A model carrying a single nullable enumeration member.
    /// </summary>
    private sealed class NullableEnumModel
    {
        /// <summary>
        /// Gets or sets the nullable color.
        /// </summary>
        /// <returns>The color, or <see langword="null" />.</returns>
        public Color? Color { get; set; }
    }

    /// <summary>
    /// A model carrying a single nullable Boolean member served by a user converter.
    /// </summary>
    private sealed class NullableBoolModel
    {
        /// <summary>
        /// Gets or sets the nullable flag.
        /// </summary>
        /// <returns>The flag, or <see langword="null" />.</returns>
        public bool? Flag { get; set; }
    }
}
