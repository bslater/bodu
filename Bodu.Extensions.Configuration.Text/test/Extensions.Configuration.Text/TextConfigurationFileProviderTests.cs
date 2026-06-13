// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationFileProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies the explicit <see cref="IFileProvider" /> overload of <c>AddBoduConfigurationFile</c>, mirroring
/// <c>AddJsonFile(IConfigurationBuilder, IFileProvider, string, bool, bool)</c>.
/// </summary>
[TestClass]
public class TextConfigurationFileProviderTests
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
        using TempFileScope scope = new(Sample, "boduconfig");
        PhysicalFileProvider fileProvider = new(scope.Directory);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddBoduConfigurationFile(fileProvider, Path.GetFileName(scope.Path))
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the file-provider overload tolerates a <see langword="null" /> provider and falls back to
    /// the builder's default file provider.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithNullFileProvider_ShouldFallBackToBuilderDefault()
    {
        using TempFileScope scope = new(Sample, "boduconfig");
        ConfigurationBuilder builder = new();
        builder.SetBasePath(scope.Directory);

        IConfiguration configuration = builder
            .AddBoduConfigurationFile(provider: null, path: Path.GetFileName(scope.Path))
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that the file-provider overload rejects a <see langword="null" /> builder.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithNullBuilder_ShouldThrowExactly()
    {
        IConfigurationBuilder builder = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = builder.AddBoduConfigurationFile(provider: null, path: "some.boduconfig");
        });
    }

    /// <summary>
    /// Verifies that the file-provider overload rejects an empty path.
    /// </summary>
    [TestMethod]
    public void AddConfiguration_WithEmptyPath_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ConfigurationBuilder().AddBoduConfigurationFile(provider: null, path: "   ");
        });
    }
}
