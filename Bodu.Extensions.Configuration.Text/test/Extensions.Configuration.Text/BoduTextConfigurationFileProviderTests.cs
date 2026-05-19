// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationFileProviderTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies the explicit <see cref="IFileProvider" /> overload of <c>AddConfiguration</c>, mirroring
/// <c>AddJsonFile(IConfigurationBuilder, IFileProvider, string, bool, bool)</c>.
/// </summary>
[TestClass]
public class BoduTextConfigurationFileProviderTests
{
    private const string Sample = """
logging.level.default = Information
""";

    /// <summary>
    /// Verifies that the file-provider overload loads the configuration via the supplied
    /// <see cref="IFileProvider" /> rather than the builder's default.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithExplicitFileProvider_ShouldLoadFromProvider()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".boduconfig");
        try
        {
            File.WriteAllText(path, Sample);
            PhysicalFileProvider fileProvider = new(Path.GetDirectoryName(path)!);

            IConfiguration configuration = new ConfigurationBuilder()
                .AddConfiguration(fileProvider, Path.GetFileName(path))
                .Build();

            Assert.AreEqual("Information", configuration["logging:level:default"]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that the file-provider overload tolerates a <see langword="null" /> provider and falls back to
    /// the builder's default file provider.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithNullFileProvider_ShouldFallBackToBuilderDefault()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".boduconfig");
        try
        {
            File.WriteAllText(path, Sample);

            ConfigurationBuilder builder = new();
            builder.SetBasePath(Path.GetDirectoryName(path)!);

            IConfiguration configuration = builder
                .AddConfiguration(provider: null, path: Path.GetFileName(path))
                .Build();

            Assert.AreEqual("Information", configuration["logging:level:default"]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that the file-provider overload rejects a <see langword="null" /> builder.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        IConfigurationBuilder builder = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddConfiguration(provider: null, path: "some.boduconfig");
        });
    }

    /// <summary>
    /// Verifies that the file-provider overload rejects an empty path.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithEmptyPath_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ConfigurationBuilder().AddConfiguration(provider: null, path: "   ");
        });
    }
}
