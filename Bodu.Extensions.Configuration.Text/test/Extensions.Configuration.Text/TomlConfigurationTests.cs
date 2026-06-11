// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Toml;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Behavioural tests for the read-only TOML configuration source — flattening into the colon-delimited key model,
/// file and stream loading, and the read-only contract.
/// </summary>
[TestClass]
public sealed class TomlConfigurationTests
{
    private const string Sample =
        "title = \"app\"\n" +
        "\n" +
        "[server]\n" +
        "host = \"localhost\"\n" +
        "port = 8080\n" +
        "enabled = true\n" +
        "\n" +
        "[logging]\n" +
        "levels = [\"info\", \"warn\"]\n";

    private static IConfigurationRoot BuildFromStream(string toml)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(toml));
        return new ConfigurationBuilder().AddTomlStream(stream).Build();
    }

    [TestMethod]
    public void AddTomlStream_WhenLoaded_ShouldFlattenNestedKeys()
    {
        var config = BuildFromStream(Sample);

        Assert.AreEqual("app", config["title"]);
        Assert.AreEqual("localhost", config["server:host"]);
        Assert.AreEqual("8080", config["server:port"]);
        Assert.AreEqual("true", config["server:enabled"]);
    }

    [TestMethod]
    public void AddTomlStream_WhenArray_ShouldFlattenToIndexedKeys()
    {
        var config = BuildFromStream(Sample);

        Assert.AreEqual("info", config["logging:levels:0"]);
        Assert.AreEqual("warn", config["logging:levels:1"]);
    }

    [TestMethod]
    public void AddTomlStream_WhenGettingSection_ShouldExposeChildren()
    {
        var config = BuildFromStream(Sample);

        IConfigurationSection server = config.GetSection("server");
        Assert.AreEqual("localhost", server["host"]);

        var childKeys = server.GetChildren().Select(c => c.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[] { "enabled", "host", "port" }, childKeys);
    }

    [TestMethod]
    public void AddTomlFile_WhenFileExists_ShouldLoadValues()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "name = \"from-file\"\n[db]\nport = 5432\n");
            var config = new ConfigurationBuilder().AddTomlFile(path).Build();

            Assert.AreEqual("from-file", config["name"]);
            Assert.AreEqual("5432", config["db:port"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void AddTomlFile_WhenOptionalAndMissing_ShouldBuildEmpty()
    {
        var config = new ConfigurationBuilder().AddTomlFile("does-not-exist.toml", optional: true).Build();

        Assert.IsNull(config["anything"]);
    }

    [TestMethod]
    public void AddTomlFile_WhenRequiredAndMissing_ShouldThrowFileNotFound()
    {
        Assert.ThrowsExactly<FileNotFoundException>(() =>
            new ConfigurationBuilder().AddTomlFile("does-not-exist.toml", optional: false).Build());
    }

    [TestMethod]
    public void Provider_WhenSet_ShouldRejectMutation()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Stream = new MemoryStream(Encoding.UTF8.GetBytes("a = 1")) });
        provider.Load();

        Assert.ThrowsExactly<NotSupportedException>(() => provider.Set("a", "2"));
    }

    [TestMethod]
    public void Provider_ShouldImplementConfigurationProvider()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource());

        Assert.IsInstanceOfType<IConfigurationProvider>(provider);
    }

    [TestMethod]
    public void AddTomlStream_WhenInvalidToml_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
            new ConfigurationBuilder().AddTomlStream(new MemoryStream(Encoding.UTF8.GetBytes("a = "))).Build());
    }

    [TestMethod]
    public void AddTomlStream_WhenNullArguments_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ((IConfigurationBuilder)null!).AddTomlStream(new MemoryStream()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ConfigurationBuilder().AddTomlStream(null!));
    }
}
