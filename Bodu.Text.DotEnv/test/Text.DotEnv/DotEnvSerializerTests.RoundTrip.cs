// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

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
}
