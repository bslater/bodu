// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the <see cref="DotEnvSerializer.Serialize{T}(T, DotEnvSerializerOptions?)" /> backbone tests.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a POCO serializes to key/value lines using the mapped names and invariant value formats.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPoco_ShouldWriteMappedKeysAndValues()
    {
        var config = new ServerConfig { Host = "localhost", Port = 5432, UseTls = true, Secret = "hidden" };

        string text = DotEnvSerializer.Serialize(config);

        Assert.AreEqual("HOST=localhost\nPORT=5432\nTLS=true\n", text);
    }

    /// <summary>
    /// Verifies that a string-keyed dictionary serializes each entry as a key/value line.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringDictionary_ShouldWriteEachEntry()
    {
        var map = new Dictionary<string, string> { ["A"] = "1", ["B"] = "two" };

        string text = DotEnvSerializer.Serialize(map);

        Assert.AreEqual("A=1\nB=two\n", text);
    }

    /// <summary>
    /// Verifies that a scalar root throws <see cref="DotEnvSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenScalarRoot_ShouldThrowDotEnvSerializationException()
    {
        Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Serialize(42);
        });
    }

    /// <summary>
    /// Verifies that the serializing callback fires during serialization.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeImplementsOnSerializing_ShouldInvokeCallback()
    {
        var config = new CallbackConfig { Name = "x" };

        _ = DotEnvSerializer.Serialize(config);

        Assert.IsTrue(config.SerializingCalled);
    }
}
