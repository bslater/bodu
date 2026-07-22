// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Contains tests for the <see cref="IniSerializer.Deserialize{T}(string, IniSerializerOptions?)" /> overloads.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a document with global keys and sections populates the matching POCO members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGlobalsAndSections_ShouldPopulatePoco()
    {
        ServerConfig config = IniSerializer.Deserialize<ServerConfig>(
            "Name=app\nRetries=3\n[Database]\nHost=localhost\nPort=5432\n[Logging]\nlevel=info\n");

        Assert.AreEqual("app", config.Name);
        Assert.AreEqual(3, config.Retries);
        Assert.IsNotNull(config.Database);
        Assert.AreEqual("localhost", config.Database.Host);
        Assert.AreEqual(5432, config.Database.Port);
        Assert.IsNotNull(config.Logging);
        Assert.AreEqual("info", config.Logging["level"]);
    }

    /// <summary>
    /// Verifies that a nested string-keyed dictionary root maps each section to a sub-dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedDictionaryRoot_ShouldMapSections()
    {
        var root = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            "[db]\nhost=x\n[log]\nlevel=warn\n");

        Assert.AreEqual(2, root.Count);
        Assert.AreEqual("x", root["db"]["host"]);
        Assert.AreEqual("warn", root["log"]["level"]);
    }

    /// <summary>
    /// Verifies that a scalar-valued dictionary root maps the global keys directly.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenScalarDictionaryRoot_ShouldMapGlobals()
    {
        var root = IniSerializer.Deserialize<Dictionary<string, string>>("a=1\nb=2\n");

        Assert.AreEqual(2, root.Count);
        Assert.AreEqual("1", root["a"]);
        Assert.AreEqual("2", root["b"]);
    }

    /// <summary>
    /// Verifies that sections cannot map into a scalar-valued dictionary root.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenScalarDictionaryRootWithSections_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, string>>("[db]\nhost=x\n");
        });
    }

    /// <summary>
    /// Verifies that global keys cannot map into a nested-dictionary root unless
    /// <see cref="IniSerializerOptions.GlobalSectionName" /> is set.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGlobalsWithoutGlobalSectionName_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>("a=1\n[db]\nhost=x\n");
        });
    }

    /// <summary>
    /// Verifies that global keys bind to the reserved key when <see cref="IniSerializerOptions.GlobalSectionName" />
    /// is set.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGlobalSectionNameSet_ShouldBindGlobalsToReservedKey()
    {
        var options = new IniSerializerOptions { GlobalSectionName = "global" };

        var root = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
            "a=1\n[db]\nhost=x\n", options);

        Assert.AreEqual("1", root["global"]["a"]);
        Assert.AreEqual("x", root["db"]["host"]);
    }

    /// <summary>
    /// Verifies that a missing required key throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeyMissing_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<RequiredConfig>("Other=1\n");
        });
    }

    /// <summary>
    /// Verifies that a value that cannot convert to the member type throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueNotConvertible_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<ServerConfig>("[Database]\nPort=not-a-number\n");
        });
    }

    /// <summary>
    /// Verifies that key and section matching honours <see cref="IniSerializerOptions.PropertyNameCaseInsensitive" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPropertyNameCaseInsensitive_ShouldMatchDifferentCase()
    {
        var options = new IniSerializerOptions { PropertyNameCaseInsensitive = true };

        ServerConfig config = IniSerializer.Deserialize<ServerConfig>("NAME=app\n[DATABASE]\nHOST=x\n", options);

        Assert.AreEqual("app", config.Name);
        Assert.IsNotNull(config.Database);
        Assert.AreEqual("x", config.Database.Host);
    }

    /// <summary>
    /// Verifies that a scalar target type throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenScalarTarget_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Deserialize<int>("a=1\n");
        });
    }

    /// <summary>
    /// Verifies that the strict defaults reject a duplicate section with <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDuplicateSectionUnderStrictDefaults_ShouldThrowIniFormatException()
    {
        var options = new IniSerializerOptions(IniSerializerDefaults.Strict);

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                "[db]\nhost=x\n[db]\nport=5\n", options);
        });
    }
}
