// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.AddTomlFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TomlConfigurationTests
{
    /// <summary>
    /// Verifies that loading an existing TOML file flattens its values into the colon-delimited key model.
    /// </summary>
    [TestMethod]
    public void AddTomlFile_WhenFileExists_ShouldLoadValues()
    {
        using TempFileScope scope = new("name = \"from-file\"\n[db]\nport = 5432\n");
        IConfigurationRoot config = new ConfigurationBuilder().AddTomlFile(scope.Path).Build();

        Assert.AreEqual("from-file", config["name"]);
        Assert.AreEqual("5432", config["db:port"]);
    }

    /// <summary>
    /// Verifies that an optional missing TOML file builds an empty configuration without throwing.
    /// </summary>
    [TestMethod]
    public void AddTomlFile_WhenOptionalAndMissing_ShouldBuildEmpty()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddTomlFile("does-not-exist.toml", optional: true).Build();

        Assert.IsNull(config["anything"]);
    }

    /// <summary>
    /// Verifies that a required missing TOML file throws <see cref="FileNotFoundException" /> when built.
    /// </summary>
    [TestMethod]
    public void AddTomlFile_WhenRequiredAndMissing_ShouldThrowFileNotFound()
    {
        Assert.ThrowsExactly<FileNotFoundException>(() =>
            new ConfigurationBuilder().AddTomlFile("does-not-exist.toml", optional: false).Build());
    }

    /// <summary>
    /// Verifies that a relative path resolves against the base path the builder sets with <c>SetBasePath</c>, as it does
    /// for <c>AddJsonFile</c>, rather than against the current directory.
    /// </summary>
    [TestMethod]
    public void AddTomlFile_WhenTheBuilderHasABasePath_ShouldResolveTheRelativePathAgainstIt()
    {
        using TempFileScope scope = new("name = \"from-file\"\n[db]\nport = 5432\n", "toml");
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(scope.Directory)
            .AddTomlFile(Path.GetFileName(scope.Path))
            .Build();

        Assert.AreEqual("from-file", config["name"]);
        Assert.AreEqual("5432", config["db:port"]);
    }

    /// <summary>
    /// Verifies that a relative path resolves through the file provider the builder sets with <c>SetFileProvider</c>,
    /// as it does for <c>AddJsonFile</c>.
    /// </summary>
    [TestMethod]
    public void AddTomlFile_WhenTheBuilderHasAFileProvider_ShouldReadThroughIt()
    {
        using TempFileScope scope = new("name = \"from-file\"\n[db]\nport = 5432\n", "toml");
        using var fileProvider = new PhysicalFileProvider(scope.Directory);
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetFileProvider(fileProvider)
            .AddTomlFile(Path.GetFileName(scope.Path))
            .Build();

        Assert.AreEqual("from-file", config["name"]);
    }
}
