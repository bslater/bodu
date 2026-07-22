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
}
