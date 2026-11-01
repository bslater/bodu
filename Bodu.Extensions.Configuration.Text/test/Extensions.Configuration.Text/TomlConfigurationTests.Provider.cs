// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.Provider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Bodu.Test.IO;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TomlConfigurationTests
{
    /// <summary>
    /// Verifies that a source with neither a stream nor a path loads an empty configuration map.
    /// </summary>
    [TestMethod]
    public void Provider_WhenSourceHasNeitherStreamNorPath_ShouldLoadEmpty()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource());

        provider.Load();

        Assert.IsFalse(provider.TryGet("anything", out _));
        Assert.AreEqual(0, provider.GetChildKeys([], null).Count());
    }

    /// <summary>
    /// Verifies that a provider constructed directly from a file-backed source reads the file through the file system,
    /// since no configuration builder supplied a file provider.
    /// </summary>
    [TestMethod]
    public void Provider_WhenConstructedDirectlyWithAnExistingPath_ShouldReadTheFileThroughTheFileSystem()
    {
        using TempFileScope scope = new("name = \"from-file\"\n[db]\nport = 5432\n", "toml");
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Path = scope.Path });

        provider.Load();

        Assert.IsTrue(provider.TryGet("name", out string? name));
        Assert.AreEqual("from-file", name);
        Assert.IsTrue(provider.TryGet("db:port", out string? port));
        Assert.AreEqual("5432", port);
    }

    /// <summary>
    /// Verifies that a provider constructed directly from an optional file-backed source whose file is missing loads
    /// an empty configuration map.
    /// </summary>
    [TestMethod]
    public void Provider_WhenConstructedDirectlyWithAMissingOptionalPath_ShouldLoadEmpty()
    {
        using TempDirectoryScope scope = new();
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Path = scope.Combine("missing.toml"), Optional = true });

        provider.Load();

        Assert.IsFalse(provider.TryGet("anything", out _));
    }

    /// <summary>
    /// Verifies that a provider constructed directly from a required file-backed source whose file is missing throws
    /// <see cref="FileNotFoundException" /> naming the file.
    /// </summary>
    [TestMethod]
    public void Provider_WhenConstructedDirectlyWithAMissingRequiredPath_ShouldThrowFileNotFoundException()
    {
        using TempDirectoryScope scope = new();
        string path = scope.Combine("missing.toml");
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Path = path });

        var ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            provider.Load();
        });

        Assert.AreEqual(path, ex.FileName);
        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.IO_FileNotFound_TomlFile, path), ex.Message);
    }

    /// <summary>
    /// Verifies that the root child keys are the first segment of every loaded key, one per key and so repeated for
    /// each key under a table, merged with the earlier providers' keys and sorted by the configuration key comparer.
    /// </summary>
    [TestMethod]
    public void GetChildKeys_WhenParentPathIsNull_ShouldReturnTopLevelSegmentsMergedWithEarlierKeys()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Stream = new MemoryStream(Encoding.UTF8.GetBytes(Sample)) });
        provider.Load();

        string[] keys = provider.GetChildKeys(["zeta"], null).ToArray();

        CollectionAssert.AreEqual(new[] { "logging", "logging", "server", "server", "server", "title", "zeta" }, keys);
    }

    /// <summary>
    /// Verifies that the read-only TOML provider rejects mutation via <c>Set</c> with
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Provider_WhenSet_ShouldRejectMutation()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource { Stream = new MemoryStream(Encoding.UTF8.GetBytes("a = 1")) });
        provider.Load();

        Assert.ThrowsExactly<NotSupportedException>(() => provider.Set("a", "2"));
    }

    /// <summary>
    /// Verifies that the TOML provider implements <see cref="IConfigurationProvider" />.
    /// </summary>
    [TestMethod]
    public void Provider_ShouldImplementConfigurationProvider()
    {
        var provider = new TomlConfigurationProvider(new TomlConfigurationSource());

        Assert.IsInstanceOfType<IConfigurationProvider>(provider);
    }
}
