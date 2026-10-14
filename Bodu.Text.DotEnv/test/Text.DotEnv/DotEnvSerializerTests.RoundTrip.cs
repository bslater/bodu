// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the serialize/deserialize round-trip tests for <see cref="DotEnvSerializer" />.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a POCO survives a serialize/deserialize round-trip with all mapped members preserved.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPoco_ShouldPreserveMappedMembers()
    {
        var original = new ServerConfig { Host = "db.internal", Port = 1234, UseTls = false };

        string text = DotEnvSerializer.Serialize(original);
        ServerConfig restored = DotEnvSerializer.Deserialize<ServerConfig>(text);

        Assert.AreEqual(original.Host, restored.Host);
        Assert.AreEqual(original.Port, restored.Port);
        Assert.AreEqual(original.UseTls, restored.UseTls);
    }

    /// <summary>
    /// Verifies that the Web defaults emit and match <c>SCREAMING_SNAKE_CASE</c> keys across a round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenWebDefaults_ShouldUseSnakeCaseUpperKeys()
    {
        var options = new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web);
        var original = new CallbackConfig { Name = "value" };

        string text = DotEnvSerializer.Serialize(original, options);
        Assert.IsTrue(text.StartsWith("NAME=value", StringComparison.Ordinal));

        CallbackConfig restored = DotEnvSerializer.Deserialize<CallbackConfig>(text, options);
        Assert.AreEqual("value", restored.Name);
    }

    /// <summary>
    /// Verifies that the asynchronous stream overloads round-trip a POCO.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsyncDeserializeAsync_WhenPoco_ShouldRoundTrip()
    {
        var original = new ServerConfig { Host = "h", Port = 9, UseTls = true };

        using var stream = new MemoryStream();
        await DotEnvSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        ServerConfig restored = await DotEnvSerializer.DeserializeAsync<ServerConfig>(stream);

        Assert.AreEqual(original.Host, restored.Host);
        Assert.AreEqual(original.Port, restored.Port);
        Assert.AreEqual(original.UseTls, restored.UseTls);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly" />, <see cref="TimeOnly" />, 128-bit, half-precision and
    /// <see cref="System.Numerics.BigInteger" /> members, and nullable forms, read back as the values written.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMembersAreTheExtendedScalars_ShouldRoundTripThem()
    {
        var original = new ExtendedScalarRecord
        {
            Day = new DateOnly(2026, 1, 2),
            Time = new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567)),
            Large = Int128.MinValue,
            Unsigned = UInt128.MaxValue,
            Small = (Half)0.1,
            Big = -(System.Numerics.BigInteger.Pow(10, 40) + 1),
            MaybeLarge = null,
            MaybeTime = new TimeOnly(23, 59, 59),
        };

        string text = DotEnvSerializer.Serialize(original);
        ExtendedScalarRecord restored = DotEnvSerializer.Deserialize<ExtendedScalarRecord>(text);

        Assert.AreEqual(original.Day, restored.Day);
        Assert.AreEqual(original.Time, restored.Time);
        Assert.AreEqual(original.Large, restored.Large);
        Assert.AreEqual(original.Unsigned, restored.Unsigned);
        Assert.AreEqual(original.Small, restored.Small);
        Assert.AreEqual(original.Big, restored.Big);
        Assert.IsNull(restored.MaybeLarge);
        Assert.AreEqual(original.MaybeTime, restored.MaybeTime);
    }

    /// <summary>
    /// Verifies that a POCO whose member hides a base member with <see langword="new" /> reads back the text it is
    /// serialized to, the hiding member keeping its value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAMemberHidesABaseMemberWithNew_ShouldReadBackTheOutput()
    {
        var original = new PortNumberHidingConfig { Port = 5 };
        ((PortTextBase)original).Port = "x";

        string text = DotEnvSerializer.Serialize(original);
        PortNumberHidingConfig restored = DotEnvSerializer.Deserialize<PortNumberHidingConfig>(text);

        Assert.AreEqual(5, restored.Port);
    }

    /// <summary>
    /// Verifies that <see cref="nint" /> and <see cref="nuint" /> members, and a nullable <see cref="nint" /> member
    /// with and without a value, read back as the values written.
    /// </summary>
    [TestMethod]
    [DataRow(true, DisplayName = "with a value")]
    [DataRow(false, DisplayName = "without a value")]
    public void SerializeDeserialize_WhenMembersAreNativeSizedIntegers_ShouldRoundTripThem(bool hasValue)
    {
        var original = new NativeIntegerRecord
        {
            Handle = -42,
            Size = nuint.MaxValue,
            MaybeHandle = hasValue ? nint.MinValue : null,
        };

        string text = DotEnvSerializer.Serialize(original);
        NativeIntegerRecord restored = DotEnvSerializer.Deserialize<NativeIntegerRecord>(text);

        Assert.AreEqual(original.Handle, restored.Handle, text);
        Assert.AreEqual(original.Size, restored.Size, text);
        Assert.AreEqual(original.MaybeHandle, restored.MaybeHandle, text);
    }

    /// <summary>
    /// Verifies that <see cref="DateTime" /> and <see cref="DateTimeOffset" /> members with fractions of a second, and
    /// their nullable forms, read back as the values written, with every tick, the kind and the offset kept.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMembersAreDatesAndTimesWithFractionsOfASecond_ShouldKeepEveryTickTheKindAndTheOffset()
    {
        var original = new TemporalConfig
        {
            At = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(8910123),
            Unspecified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified).AddTicks(1),
            AtOffset = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5)).AddTicks(1234567),
            MaybeAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddMilliseconds(891),
            MaybeAtOffset = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(-7)).AddMilliseconds(891),
        };

        string text = DotEnvSerializer.Serialize(original);
        TemporalConfig restored = DotEnvSerializer.Deserialize<TemporalConfig>(text);

        Assert.AreEqual(original.At, restored.At, text);
        Assert.AreEqual(DateTimeKind.Utc, restored.At.Kind, text);
        Assert.AreEqual(original.Unspecified, restored.Unspecified, text);
        Assert.AreEqual(DateTimeKind.Unspecified, restored.Unspecified.Kind, text);
        Assert.AreEqual(original.AtOffset, restored.AtOffset, text);
        Assert.AreEqual(original.AtOffset.Offset, restored.AtOffset.Offset, text);
        Assert.AreEqual(original.MaybeAt, restored.MaybeAt, text);
        Assert.AreEqual(DateTimeKind.Utc, restored.MaybeAt!.Value.Kind, text);
        Assert.AreEqual(original.MaybeAtOffset, restored.MaybeAtOffset, text);
        Assert.AreEqual(original.MaybeAtOffset!.Value.Offset, restored.MaybeAtOffset!.Value.Offset, text);
    }

    /// <summary>
    /// Verifies that a root that implements only the generic dictionary interface reads back as the entries written,
    /// in the same order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTheRootImplementsOnlyTheGenericDictionaryInterface_ShouldPreserveEveryEntry()
    {
        var original = new GenericOnlyDictionary<string> { ["HOST"] = "db.internal", ["PORT"] = "1234" };

        string text = DotEnvSerializer.Serialize(original);
        GenericOnlyDictionary<string> restored = DotEnvSerializer.Deserialize<GenericOnlyDictionary<string>>(text);

        CollectionAssert.AreEqual(original.ToArray(), restored.ToArray(), text);
    }

    /// <summary>
    /// Verifies that scalar members holding <see langword="null" /> or their defaults read back, under every default
    /// ignore condition, as the values written: a number, a flag and a nullable number unchanged, and a
    /// <see langword="null" /> text as itself when it is left out or as the empty text it is written as otherwise.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    /// <param name="expectedText">The text the <see langword="null" /> text member reads back as.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never, "")]
    [DataRow(IgnoreCondition.WhenWritingNull, null)]
    [DataRow(IgnoreCondition.WhenWritingDefault, null)]
    public void SerializeDeserialize_WhenScalarMembersAreNullOrDefault_ShouldReadBackEachValue(IgnoreCondition condition, string? expectedText)
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };

        ConditionalConfig result = DotEnvSerializer.Deserialize<ConditionalConfig>(DotEnvSerializer.Serialize(new ConditionalConfig(), options), options);

        Assert.AreEqual(expectedText, result.Text);
        Assert.AreEqual(0, result.Zero);
        Assert.AreEqual(1, result.One);
        Assert.IsFalse(result.Flag);
        Assert.IsNull(result.Maybe);
        Assert.AreEqual(0, result.ZeroByMember);
        Assert.AreEqual(string.Empty, result.NullByMember);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> dictionary entry whose value type is a nullable scalar reads back under
    /// every default ignore condition: as <see langword="null" /> where the condition writes it, and as an absent key
    /// where it leaves it out.
    /// </summary>
    /// <param name="condition">The options' default ignore condition.</param>
    /// <param name="written">Whether the condition writes the <see langword="null" /> entry.</param>
    [TestMethod]
    [DataRow(IgnoreCondition.Never, true)]
    [DataRow(IgnoreCondition.WhenWritingNull, false)]
    [DataRow(IgnoreCondition.WhenWritingDefault, false)]
    public void SerializeDeserialize_WhenADictionaryEntryIsANullScalar_ShouldReadBackUnderEveryCondition(IgnoreCondition condition, bool written)
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = condition };
        var map = new Dictionary<string, int?> { ["A"] = null, ["B"] = 1 };

        Dictionary<string, int?> result = DotEnvSerializer.Deserialize<Dictionary<string, int?>>(DotEnvSerializer.Serialize(map, options), options)!;

        Assert.AreEqual(written, result.ContainsKey("A"));
        Assert.IsNull(result.GetValueOrDefault("A"));
        Assert.AreEqual(1, result["B"]);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> member declared as <see cref="object" />, written as an empty value
    /// under <see cref="IgnoreCondition.Never" />, reads back as the empty text, since a member declared as
    /// <see cref="object" /> reads its value as text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAnObjectMemberIsNullUnderNever_ShouldReadBackTheEmptyText()
    {
        var options = new DotEnvSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };

        ObjectValueConfig result = DotEnvSerializer.Deserialize<ObjectValueConfig>(DotEnvSerializer.Serialize(new ObjectValueConfig(), options), options);

        Assert.AreEqual(string.Empty, result.Value);
    }

    /// <summary>
    /// Verifies that a root whose type is a nullable structure reads back as the value written.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTheRootIsANullableStruct_ShouldReadBackItsValue()
    {
        PortStruct? value = new PortStruct { Port = 5, Host = "h" };

        PortStruct? result = DotEnvSerializer.Deserialize<PortStruct?>(DotEnvSerializer.Serialize(value));

        Assert.AreEqual(value, result);
    }
}
