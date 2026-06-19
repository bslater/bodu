// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.AddTomlStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.IO;
using Bodu.Text.Toml;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TomlConfigurationTests
{
    /// <summary>
    /// Verifies that loading a TOML stream flattens nested tables into colon-delimited keys with values
    /// stringified.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenLoaded_ShouldFlattenNestedKeys()
    {
        var config = BuildFromStream(Sample);

        Assert.AreEqual("app", config["title"]);
        Assert.AreEqual("localhost", config["server:host"]);
        Assert.AreEqual("8080", config["server:port"]);
        Assert.AreEqual("true", config["server:enabled"]);
    }

    /// <summary>
    /// Verifies that a TOML array is flattened into zero-based indexed colon-delimited keys.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenArray_ShouldFlattenToIndexedKeys()
    {
        var config = BuildFromStream(Sample);

        Assert.AreEqual("info", config["logging:levels:0"]);
        Assert.AreEqual("warn", config["logging:levels:1"]);
    }

    /// <summary>
    /// Verifies that <see cref="IConfiguration.GetSection(string)" /> over loaded TOML exposes the table's leaf
    /// keys as ordered children.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenGettingSection_ShouldExposeChildren()
    {
        var config = BuildFromStream(Sample);

        IConfigurationSection server = config.GetSection("server");
        Assert.AreEqual("localhost", server["host"]);

        string[] childKeys = server.GetChildren().Select(c => c.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[] { "enabled", "host", "port" }, childKeys);
    }

    /// <summary>
    /// Verifies that loading malformed TOML from a stream throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenInvalidToml_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
            new ConfigurationBuilder().AddTomlStream(new MemoryStream(Encoding.UTF8.GetBytes("a = "))).Build());
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> builder or stream argument throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void AddTomlStream_WhenNullArguments_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ((IConfigurationBuilder)null!).AddTomlStream(new MemoryStream()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigurationBuilder().AddTomlStream(null!));
    }
}
