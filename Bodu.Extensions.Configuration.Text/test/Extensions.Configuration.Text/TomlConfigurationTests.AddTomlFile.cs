// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationTests.AddTomlFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

using Microsoft.Extensions.Configuration;

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
}
