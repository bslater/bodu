// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Verifies the object-mapping behavior of <see cref="TomlSerializer" />: round trips, scalar and collection mapping,
/// the table-root rule, null handling, and naming policies.
/// </summary>
[TestClass]
public class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a flat object round-trips through serialization and deserialization unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundTrip_WhenValueIsFlatObject_ShouldPreserveMembers()
    {
        var config = new ServerConfig { Host = "localhost", Port = 8080, Secure = true, Level = LogLevel.Warning };

        ServerConfig result = TomlSerializer.Deserialize<ServerConfig>(TomlSerializer.Serialize(config));

        Assert.AreEqual("localhost", result.Host);
        Assert.AreEqual(8080, result.Port);
        Assert.IsTrue(result.Secure);
        Assert.AreEqual(LogLevel.Warning, result.Level);
    }

    /// <summary>
    /// Verifies that a nested object with a child table and a collection round-trips.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsNested_ShouldPreserveGraph()
    {
        var config = new AppConfig
        {
            Title = "demo",
            Server = new ServerConfig { Host = "host", Port = 1 },
            Tags = ["a", "b"],
        };

        AppConfig result = TomlSerializer.Deserialize<AppConfig>(TomlSerializer.Serialize(config));

        Assert.AreEqual("demo", result.Title);
        Assert.AreEqual("host", result.Server.Host);
        CollectionAssert.AreEqual(config.Tags, result.Tags);
    }

    /// <summary>
    /// Verifies that the date-time, float, and byte-array kinds round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueHasDateTimeAndBinary_ShouldPreserveContent()
    {
        var record = new EventRecord
        {
            When = new DateTimeOffset(2026, 6, 10, 7, 32, 0, TimeSpan.Zero),
            Seconds = 1.5,
            Payload = [0x01, 0x02, 0x03],
        };

        EventRecord result = TomlSerializer.Deserialize<EventRecord>(TomlSerializer.Serialize(record));

        Assert.AreEqual(record.When, result.When);
        Assert.AreEqual(1.5, result.Seconds);
        CollectionAssert.AreEqual(record.Payload, result.Payload);
    }

    /// <summary>
    /// Verifies that a flat object serializes to the expected canonical TOML text.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsFlatObject_ShouldProduceCanonicalText()
    {
        var config = new ServerConfig { Host = "localhost", Port = 8080, Secure = true, Level = LogLevel.Info };

        var text = TomlSerializer.Serialize(config);

        Assert.AreEqual("Host = \"localhost\"\nPort = 8080\nSecure = true\nLevel = \"Info\"\n", text);
    }

    /// <summary>
    /// Verifies that a property naming policy is applied to the emitted keys.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicySet_ShouldRenameKeys()
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = FormatNamingPolicy.SnakeCaseLower };
        var config = new ServerConfig { Host = "h", Port = 1 };

        var text = TomlSerializer.Serialize(config, options);

        Assert.IsTrue(text.Contains("host = \"h\"", StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("port = 1", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a null member is omitted from the output under the default null handling.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIsNull_ShouldOmitMember()
    {
        var config = new AppConfig { Title = "t", Description = null };

        var text = TomlSerializer.Serialize(config);

        Assert.IsFalse(text.Contains("Description", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that serializing a value that does not map to a table at the root throws
    /// <see cref="FormatSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsNotTable_ShouldThrowFormatSerializationException()
    {
        Assert.ThrowsExactly<FormatSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(42);
        });
    }

    /// <summary>
    /// Verifies that a value is deserialized from hand-written TOML text.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenReadingText_ShouldBindMembers()
    {
        const string Source = "Title = \"demo\"\nTags = [\"x\", \"y\"]\n\n[Server]\nHost = \"h\"\nPort = 9\n";

        AppConfig result = TomlSerializer.Deserialize<AppConfig>(Source);

        Assert.AreEqual("demo", result.Title);
        Assert.AreEqual("h", result.Server.Host);
        Assert.AreEqual(9, result.Server.Port);
        CollectionAssert.AreEqual(new[] { "x", "y" }, result.Tags);
    }

    /// <summary>
    /// Verifies that deserializing malformed TOML throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTextIsMalformed_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<ServerConfig>("Host = \n");
        });
    }

    /// <summary>
    /// Verifies that a value round-trips through a UTF-8 stream.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenReadingFromStream_ShouldProduceEquivalentValue()
    {
        var config = new ServerConfig { Host = "h", Port = 3 };
        using MemoryStream stream = new();
        TomlSerializer.Serialize(stream, config);
        stream.Position = 0;

        ServerConfig result = TomlSerializer.Deserialize<ServerConfig>(stream);

        Assert.AreEqual("h", result.Host);
        Assert.AreEqual(3, result.Port);
    }
}
