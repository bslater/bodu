// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains tests for the <see cref="IniSerializer.Serialize{T}(T, IniSerializerOptions?)" /> overloads.
/// </summary>
public partial class IniSerializerTests
{
    /// <summary>
    /// Verifies that a POCO with global scalar members, a section POCO, and a section dictionary serializes to the
    /// expected INI text with globals before sections.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenPocoWithGlobalsAndSections_ShouldEmitExpectedText()
    {
        var config = new ServerConfig
        {
            Name = "app",
            Retries = 3,
            Database = new DatabaseSection { Host = "localhost", Port = 5432 },
            Logging = new Dictionary<string, string> { ["level"] = "info" },
        };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Name=app\nRetries=3\n[Database]\nHost=localhost\nPort=5432\n[Logging]\nlevel=info\n", text);
    }

    /// <summary>
    /// Verifies that the configured naming policy applies to global keys, section names, and section keys.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNamingPolicySnakeCaseLower_ShouldApplyToKeysAndSections()
    {
        var config = new ServerConfig
        {
            Name = "app",
            Database = new DatabaseSection { Host = "x", Port = 1 },
        };
        var options = new IniSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

        string text = IniSerializer.Serialize(config, options);

        Assert.AreEqual("name=app\nretries=0\n[database]\nhost=x\nport=1\n", text);
    }

    /// <summary>
    /// Verifies that a nested string-keyed dictionary root serializes each entry as a section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNestedDictionaryRoot_ShouldEmitSections()
    {
        var root = new Dictionary<string, Dictionary<string, string>>
        {
            ["db"] = new() { ["host"] = "x" },
            ["log"] = new() { ["level"] = "warn" },
        };

        string text = IniSerializer.Serialize(root);

        Assert.AreEqual("[db]\nhost=x\n[log]\nlevel=warn\n", text);
    }

    /// <summary>
    /// Verifies that the entry matching <see cref="IniSerializerOptions.GlobalSectionName" /> is emitted as global
    /// keys rather than a section.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGlobalSectionNameSet_ShouldEmitReservedEntryAsGlobals()
    {
        var root = new Dictionary<string, Dictionary<string, string>>
        {
            ["global"] = new() { ["a"] = "1" },
            ["db"] = new() { ["host"] = "x" },
        };
        var options = new IniSerializerOptions { GlobalSectionName = "global" };

        string text = IniSerializer.Serialize(root, options);

        Assert.AreEqual("a=1\n[db]\nhost=x\n", text);
    }

    /// <summary>
    /// Verifies that a null section member is omitted under the default ignore condition.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenSectionMemberNull_ShouldOmitSection()
    {
        var config = new ServerConfig { Name = "app" };

        string text = IniSerializer.Serialize(config);

        Assert.AreEqual("Name=app\nRetries=0\n", text);
    }

    /// <summary>
    /// Verifies that a scalar root throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenScalarRoot_ShouldThrowIniSerializationException()
    {
        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize("just a string");
        });
    }

    /// <summary>
    /// Verifies that a member nested beyond INI's two levels throws <see cref="IniSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberNestsBeyondTwoLevels_ShouldThrowIniSerializationException()
    {
        var config = new DeepConfig { Outer = new OuterSection { Inner = new DatabaseSection { Host = "x" } } };

        Assert.ThrowsExactly<IniSerializationException>(() =>
        {
            _ = IniSerializer.Serialize(config);
        });
    }
}
