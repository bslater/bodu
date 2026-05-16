// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationDefaultFilenameTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies the parameterless default-filename overload of <c>AddBoduConfiguration</c>, which probes for
/// <c>.boduconfig</c> and falls back to <c>bodu.config</c>.
/// </summary>
[TestClass]
public class BoduTextConfigurationDefaultFilenameTests
{
    private const string Sample = """
default.filename.loaded = yes
""";

    /// <summary>
    /// Verifies that when only the dot-prefixed <c>.boduconfig</c> file exists, the default overload loads it.
    /// </summary>
    [TestMethod]
    public void AddBoduConfiguration_WhenNoArgsAndDotConfigPresent_ShouldLoad()
    {
        string directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, ".boduconfig"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory, ExclusionFilters.None));

            IConfiguration configuration = builder.AddBoduConfiguration().Build();

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
    public void AddBoduConfiguration_WhenNoArgsAndPlainConfigPresent_ShouldLoadFallback()
    {
        string directory = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "bodu.config"), Sample);

            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory));

            IConfiguration configuration = builder.AddBoduConfiguration().Build();

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
    public void AddBoduConfiguration_WhenNoArgsAndAllMissing_ShouldNotThrow()
    {
        string directory = CreateTempDirectory();
        try
        {
            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory));

            IConfiguration configuration = builder.AddBoduConfiguration(optional: true).Build();

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
    public void AddBoduConfiguration_WhenNoArgsAndRequiredAndAllMissing_ShouldThrowFileNotFoundException()
    {
        string directory = CreateTempDirectory();
        try
        {
            ConfigurationBuilder builder = new();
            builder.SetFileProvider(new PhysicalFileProvider(directory));

            Assert.ThrowsExactly<FileNotFoundException>(() =>
            {
                _ = builder.AddBoduConfiguration(optional: false).Build();
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
    public void AddBoduConfiguration_WhenBuilderIsNull_ShouldThrowArgumentNullException()
    {
        IConfigurationBuilder builder = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddBoduConfiguration();
        });
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        return directory;
    }
}
