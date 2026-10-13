// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains round-trip tests that serialize a value and deserialize the result back.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a POCO with globals and sections survives a serialize/deserialize round trip.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPocoWithSections_ShouldRoundTrip()
    {
        var original = new ServerConfig
        {
            Name = "app",
            Retries = 5,
            Database = new DatabaseSection { Host = "db.example.com", Port = 5432 },
            Logging = new Dictionary<string, string> { ["level"] = "debug", ["sink"] = "console" },
        };

        ServerConfig result = IniSerializer.Deserialize<ServerConfig>(IniSerializer.Serialize(original));

        Assert.AreEqual(original.Name, result.Name);
        Assert.AreEqual(original.Retries, result.Retries);
        Assert.IsNotNull(result.Database);
        Assert.AreEqual(original.Database.Host, result.Database.Host);
        Assert.AreEqual(original.Database.Port, result.Database.Port);
        Assert.IsNotNull(result.Logging);
        CollectionAssert.AreEqual(original.Logging, result.Logging);
    }

    /// <summary>
    /// Verifies that a nested dictionary with a reserved global key survives a round trip under
    /// <see cref="IniSerializerOptions.GlobalSectionName" />.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGlobalSectionName_ShouldRoundTrip()
    {
        var options = new IniSerializerOptions { GlobalSectionName = "global" };
        var original = new Dictionary<string, Dictionary<string, string>>
        {
            ["global"] = new() { ["a"] = "1" },
            ["db"] = new() { ["host"] = "x" },
        };

        var result = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            IniSerializer.Serialize(original, options), options);

        Assert.AreEqual("1", result["global"]["a"]);
        Assert.AreEqual("x", result["db"]["host"]);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly" />, <see cref="TimeOnly" />, 128-bit, half-precision and
    /// <see cref="System.Numerics.BigInteger" /> members, and nullable forms, are written as global keys rather than
    /// sections and read back as the values written.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMembersAreTheExtendedScalars_ShouldRoundTripThemAsKeys()
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

        string text = IniSerializer.Serialize(original);
        ExtendedScalarRecord restored = IniSerializer.Deserialize<ExtendedScalarRecord>(text);

        Assert.IsFalse(text.Contains('[', StringComparison.Ordinal), text);
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
    /// Verifies that under the <see cref="IniSerializerDefaults.Strict" /> defaults, which reject a duplicate key, a
    /// root model whose member hides a base member with <see langword="new" /> reads back the text it is serialized to.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAMemberHidesABaseMemberWithNew_ForStrictDefaults_ShouldReadBackTheOutput()
    {
        var options = new IniSerializerOptions(IniSerializerDefaults.Strict);
        var original = new PortNumberHidingConfig { Port = 5 };
        ((PortTextBase)original).Port = "x";

        string text = IniSerializer.Serialize(original, options);
        PortNumberHidingConfig restored = IniSerializer.Deserialize<PortNumberHidingConfig>(text, options);

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

        string text = IniSerializer.Serialize(original);
        NativeIntegerRecord restored = IniSerializer.Deserialize<NativeIntegerRecord>(text);

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

        string text = IniSerializer.Serialize(original);
        TemporalConfig restored = IniSerializer.Deserialize<TemporalConfig>(text);

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
    /// Verifies that a dictionary root holding a <see langword="null" /> section entry, serialized under
    /// <see cref="IgnoreCondition.Never" />, reads back with that entry as an empty section and every other section
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenANullRootEntryIsASectionUnderNever_ShouldReadBackAnEmptySection()
    {
        var options = new IniSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };
        var root = new Dictionary<string, Dictionary<string, string>?> { ["db"] = null, ["x"] = new() { ["k"] = "v" } };

        Dictionary<string, Dictionary<string, string>?> result =
            IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>?>>(IniSerializer.Serialize(root, options), options)!;

        CollectionAssert.AreEqual(new[] { "db", "x" }, result.Keys.ToArray());
        Assert.IsNotNull(result["db"]);
        Assert.AreEqual(0, result["db"]!.Count);
        Assert.AreEqual("v", result["x"]!["k"]);
    }
}
