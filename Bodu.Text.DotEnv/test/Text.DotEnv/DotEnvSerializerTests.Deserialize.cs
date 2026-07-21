// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the <see cref="DotEnvSerializer.Deserialize{T}(string, DotEnvSerializerOptions?)" /> backbone tests.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a document binds to a POCO, converting typed values with invariant parsing.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPoco_ShouldBindMappedKeysAndConvertValues()
    {
        ServerConfig config = DotEnvSerializer.Deserialize<ServerConfig>("HOST=example\nPORT=8080\nTLS=true\n");

        Assert.AreEqual("example", config.Host);
        Assert.AreEqual(8080, config.Port);
        Assert.IsTrue(config.UseTls);
    }

    /// <summary>
    /// Verifies that a document binds to a string-keyed dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringDictionary_ShouldBindEachEntry()
    {
        Dictionary<string, string> map = DotEnvSerializer.Deserialize<Dictionary<string, string>>("A=1\nB=two\n");

        Assert.AreEqual("1", map["A"]);
        Assert.AreEqual("two", map["B"]);
    }

    /// <summary>
    /// Verifies that an <see cref="int" />-valued dictionary converts each value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypedDictionary_ShouldConvertValues()
    {
        Dictionary<string, int> map = DotEnvSerializer.Deserialize<Dictionary<string, int>>("A=1\nB=2\n");

        Assert.AreEqual(1, map["A"]);
        Assert.AreEqual(2, map["B"]);
    }

    /// <summary>
    /// Verifies that a missing required key throws <see cref="DotEnvSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeyMissing_ShouldThrowDotEnvSerializationException()
    {
        Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Deserialize<RequiredConfig>("OTHER=1\n");
        });
    }

    /// <summary>
    /// Verifies that case-insensitive matching binds keys whose case differs from the member name.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseInsensitive_ShouldBindDifferentlyCasedKeys()
    {
        var options = new DotEnvSerializerOptions { PropertyNameCaseInsensitive = true };

        RequiredConfig config = DotEnvSerializer.Deserialize<RequiredConfig>("token=abc\n", options);

        Assert.AreEqual("abc", config.Token);
    }

    /// <summary>
    /// Verifies that an unconvertible value throws <see cref="DotEnvSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueNotConvertible_ShouldThrowDotEnvSerializationException()
    {
        Assert.ThrowsExactly<DotEnvSerializationException>(() =>
        {
            _ = DotEnvSerializer.Deserialize<ServerConfig>("HOST=x\nPORT=notanumber\n");
        });
    }

    /// <summary>
    /// Verifies that the deserialized callback fires after binding.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypeImplementsOnDeserialized_ShouldInvokeCallback()
    {
        CallbackConfig config = DotEnvSerializer.Deserialize<CallbackConfig>("Name=x\n");

        Assert.IsTrue(config.DeserializedCalled);
    }
}
