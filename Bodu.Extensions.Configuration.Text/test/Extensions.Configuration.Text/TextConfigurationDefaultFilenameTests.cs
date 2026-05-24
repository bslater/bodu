// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationDefaultFilenameTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies the parameterless default-filename overload of <c>AddConfiguration</c>, which probes for
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
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, ".boduconfig"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory, ExclusionFilters.None));

            IConfiguration configuration = builder.AddConfiguration().Build();

            Assert.AreEqual("yes", configuration["default:filename:loaded"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that when the dot-prefixed name is absent, the default overload falls back to
    /// <c>bodu.config</c>.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndPlainConfigPresent_ShouldLoadFallback()
    {
        var directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "bodu.config"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory));

            IConfiguration configuration = builder.AddConfiguration().Build();

            Assert.AreEqual("yes", configuration["default:filename:loaded"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that when neither conventional file is present and the call is optional, the builder
    /// produces an empty configuration view without throwing.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndAllMissing_ShouldNotThrow()
    {
        var directory = CreateTempDirectory();
        try
        {
            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory));

            IConfiguration configuration = builder.AddConfiguration(optional: true).Build();

            Assert.IsNull(configuration["default:filename:loaded"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that when neither conventional file is present and the call is required, the builder throws a
    /// <see cref="FileNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WhenNoArgsAndRequiredAndAllMissing_ShouldThrowExactly()
    {
        var directory = CreateTempDirectory();
        try
        {
            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory));

            Assert.ThrowsExactly<FileNotFoundException>(() =>
            {
                _ = builder.AddConfiguration(optional: false).Build();
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
            _ = builder.AddConfiguration();
        });
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        return directory;
    }
}
