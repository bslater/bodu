// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationDefaultFilenameTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies the parameterless default-filename overload of <c>AddTextConfiguration</c>, which probes for
/// <c>.boduconfig</c> and falls back to <c>bodu.config</c>.
/// </summary>
[TestClass]
public class TextConfigurationDefaultFilenameTests
{
    private const string Sample = """
default.filename.loaded = yes
""";

    /// <summary>
    /// Verifies that when only the dot-prefixed <c>.boduconfig</c> file exists, the default overload loads it.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndDotConfigPresent_ShouldLoad()
    {
        using TempDirectoryScope scope = new();
        scope.WriteFile(".boduconfig", Sample);

        ConfigurationBuilder builder = new();
        builder.SetFileProvider(new PhysicalFileProvider(scope.Path, ExclusionFilters.None));

        IConfiguration configuration = builder.AddTextConfiguration().Build();

        Assert.AreEqual("yes", configuration["default:filename:loaded"]);
    }

    /// <summary>
    /// Verifies that when the dot-prefixed name is absent, the default overload falls back to
    /// <c>bodu.config</c>.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndPlainConfigPresent_ShouldLoadFallback()
    {
        using TempDirectoryScope scope = new();
        scope.WriteFile("bodu.config", Sample);

        ConfigurationBuilder builder = new();
        builder.SetFileProvider(new PhysicalFileProvider(scope.Path));

        IConfiguration configuration = builder.AddTextConfiguration().Build();

        Assert.AreEqual("yes", configuration["default:filename:loaded"]);
    }

    /// <summary>
    /// Verifies that when neither conventional file is present and the call is optional, the builder
    /// produces an empty configuration view without throwing.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndAllMissing_ShouldNotThrow()
    {
        using TempDirectoryScope scope = new();
        ConfigurationBuilder builder = new();
        builder.SetFileProvider(new PhysicalFileProvider(scope.Path));

        IConfiguration configuration = builder.AddTextConfiguration(optional: true).Build();

        Assert.IsNull(configuration["default:filename:loaded"]);
    }

    /// <summary>
    /// Verifies that when neither conventional file is present and the call is required, the builder throws a
    /// <see cref="FileNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndRequiredAndAllMissing_ShouldThrowExactly()
    {
        using TempDirectoryScope scope = new();
        ConfigurationBuilder builder = new();
        builder.SetFileProvider(new PhysicalFileProvider(scope.Path));

        Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = builder.AddTextConfiguration(optional: false).Build();
        });
    }

    /// <summary>
    /// Verifies that the default-filename overload rejects a <see langword="null" /> builder.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenBuilderIsNull_ShouldThrowExactly()
    {
        IConfigurationBuilder builder = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddTextConfiguration();
        });
    }
}
