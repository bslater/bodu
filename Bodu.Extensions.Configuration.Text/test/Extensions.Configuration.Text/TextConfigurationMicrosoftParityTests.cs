// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationMicrosoftParityTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Side-by-side parity tests that compare the Bodu Text Configuration provider against Microsoft's INI and
/// JSON providers on contracts shared by all <see cref="FileConfigurationProvider" /> implementations. These
/// tests pin that the Bodu bridge behaves identically to its peers on the surface that
/// <see cref="IConfiguration" /> consumers depend on.
/// </summary>
[TestClass]
public class TextConfigurationMicrosoftParityTests
{
    /// <summary>
    /// Verifies that key lookup is case-insensitive for all three providers — Bodu, INI, and JSON all use
    /// <see cref="StringComparer.OrdinalIgnoreCase" /> on the configuration root.
    /// </summary>
    [TestMethod]
    public void KeyLookup_ShouldBeCaseInsensitive_AcrossAllProviders()
    {
        const string boduSource = "Service.Name = Bodu\n";
        const string iniSource = "[Service]\nName = Ini\n";
        const string jsonSource = """{ "Service": { "Name": "Json" } }""";

        using MemoryStream boduStream = new(Encoding.UTF8.GetBytes(boduSource));
        using MemoryStream iniStream = new(Encoding.UTF8.GetBytes(iniSource));
        using MemoryStream jsonStream = new(Encoding.UTF8.GetBytes(jsonSource));

        IConfiguration bodu = new ConfigurationBuilder().AddTextConfigurationStream(boduStream).Build();
        IConfiguration ini = new ConfigurationBuilder().AddIniStream(iniStream).Build();
        IConfiguration json = new ConfigurationBuilder().AddJsonStream(jsonStream).Build();

        Assert.AreEqual("Bodu", bodu["service:NAME"]);
        Assert.AreEqual("Ini", ini["SERVICE:name"]);
        Assert.AreEqual("Json", json["Service:Name"]);
    }

    /// <summary>
    /// Verifies that when the same key is supplied by multiple providers, the last-added provider wins for
    /// all three. This is the Microsoft-wide rule and Bodu must follow it to compose with JSON and INI in a
    /// builder chain.
    /// </summary>
    [TestMethod]
    public void LastAddedProvider_ShouldWin_ForAllThreeProviders()
    {
        // Each builder gets its own pair of streams — stream sources are one-shot, so a stream cannot be
        // shared across two .Build() calls.
        using (MemoryStream iniA = new(Encoding.UTF8.GetBytes("value = ini\n")))
        using (MemoryStream bodu = new(Encoding.UTF8.GetBytes("value = bodu\n")))
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddIniStream(iniA)
                .AddTextConfigurationStream(bodu)
                .Build();
            Assert.AreEqual("bodu", config["value"]);
        }

        using (MemoryStream iniB = new(Encoding.UTF8.GetBytes("value = ini\n")))
        using (MemoryStream json = new(Encoding.UTF8.GetBytes("""{ "value": "json" }""")))
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddIniStream(iniB)
                .AddJsonStream(json)
                .Build();
            Assert.AreEqual("json", config["value"]);
        }
    }

    /// <summary>
    /// Verifies that a missing required file throws <see cref="FileNotFoundException" /> for all three
    /// file-backed providers. The optional flag has the same meaning across the board.
    /// </summary>
    [TestMethod]
    public void Build_WhenMissingRequiredFile_ShouldThrow_ForAllThreeProviders()
    {
        using TempDirectoryScope scope = new();
        Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = new ConfigurationBuilder()
                .SetBasePath(scope.Path)
                .AddTextConfigurationFile("missing.boduconfig", targetPath: null, optional: false, reloadOnChange: false)
                .Build();
        });

        Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = new ConfigurationBuilder()
                .SetBasePath(scope.Path)
                .AddIniFile("missing.ini", optional: false)
                .Build();
        });

        Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = new ConfigurationBuilder()
                .SetBasePath(scope.Path)
                .AddJsonFile("missing.json", optional: false)
                .Build();
        });
    }

    /// <summary>
    /// Verifies that a missing optional file yields an empty (key-absent) configuration view without
    /// throwing for all three providers.
    /// </summary>
    [TestMethod]
    public void Build_WhenMissingOptionalFile_ShouldYieldEmpty_ForAllThreeProviders()
    {
        using TempDirectoryScope scope = new();
        IConfiguration bodu = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddTextConfigurationFile("missing.boduconfig", targetPath: null, optional: true, reloadOnChange: false)
            .Build();

        IConfiguration ini = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddIniFile("missing.ini", optional: true)
            .Build();

        IConfiguration json = new ConfigurationBuilder()
            .SetBasePath(scope.Path)
            .AddJsonFile("missing.json", optional: true)
            .Build();

        Assert.IsNull(bodu["anything"]);
        Assert.IsNull(ini["anything"]);
        Assert.IsNull(json["anything"]);
    }

    /// <summary>
    /// Verifies that a malformed required file throws <see cref="InvalidDataException" /> for both Bodu and
    /// JSON. INI accepts loose syntax so it is not a useful parity target for malformed-file behaviour.
    /// </summary>
    [TestMethod]
    public void Build_WhenMalformedRequiredFile_ShouldThrowInvalidDataException_ForBoduAndJson()
    {
        using TempDirectoryScope scope = new();
        scope.WriteFile("bad.boduconfig", "[unterminated\nkey = value\n");
        scope.WriteFile("bad.json", "{ not valid json");

        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            _ = new ConfigurationBuilder()
                .SetBasePath(scope.Path)
                .AddTextConfigurationFile("bad.boduconfig", targetPath: null, optional: false, reloadOnChange: false)
                .Build();
        });

        Assert.ThrowsExactly<InvalidDataException>(() =>
        {
            _ = new ConfigurationBuilder()
                .SetBasePath(scope.Path)
                .AddJsonFile("bad.json", optional: false)
                .Build();
        });
    }

    /// <summary>
    /// Verifies that array binding via numeric segments works for both Bodu and JSON. JSON expresses this
    /// natively via arrays; Bodu does it via numeric key segments (<c>items.0 = first</c>).
    /// </summary>
    [TestMethod]
    public void ArrayBinding_ShouldWork_ForBoduAndJson()
    {
        const string boduSource = """
items.0 = first
items.1 = second
items.2 = third
""";
        const string jsonSource = """{ "items": [ "first", "second", "third" ] }""";

        using MemoryStream b = new(Encoding.UTF8.GetBytes(boduSource));
        using MemoryStream j = new(Encoding.UTF8.GetBytes(jsonSource));

        IConfiguration bodu = new ConfigurationBuilder().AddTextConfigurationStream(b).Build();
        IConfiguration json = new ConfigurationBuilder().AddJsonStream(j).Build();

        string[]? boduItems = bodu.GetSection("items").Get<string[]>();
        string[]? jsonItems = json.GetSection("items").Get<string[]>();

        CollectionAssert.AreEqual(jsonItems, boduItems);
    }

    /// <summary>
    /// Verifies that a stream source on the Bodu provider exposes the same one-shot, non-reloadable surface
    /// as the JSON stream source — both inherit from <see cref="StreamConfigurationProvider" />.
    /// </summary>
    [TestMethod]
    public void StreamSource_ShouldNotInheritFromFileConfigurationProvider_ForBoduAndJson()
    {
        using MemoryStream b = new(Encoding.UTF8.GetBytes("key = value\n"));
        using MemoryStream j = new(Encoding.UTF8.GetBytes("""{ "key": "value" }"""));

        IConfigurationRoot bodu = new ConfigurationBuilder().AddTextConfigurationStream(b).Build();
        IConfigurationRoot json = new ConfigurationBuilder().AddJsonStream(j).Build();

        // Both providers should derive from StreamConfigurationProvider, not FileConfigurationProvider.
        IConfigurationProvider boduProvider = bodu.Providers.First();
        IConfigurationProvider jsonProvider = json.Providers.First();

        Assert.IsFalse(typeof(FileConfigurationProvider).IsAssignableFrom(boduProvider.GetType()));
        Assert.IsFalse(typeof(FileConfigurationProvider).IsAssignableFrom(jsonProvider.GetType()));
    }
}
