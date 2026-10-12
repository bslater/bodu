// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
