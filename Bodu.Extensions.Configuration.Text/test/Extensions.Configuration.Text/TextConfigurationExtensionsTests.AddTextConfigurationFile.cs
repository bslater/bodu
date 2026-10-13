// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationExtensionsTests.AddTextConfigurationFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class TextConfigurationExtensionsTests
{
    /// <summary>
    /// Verifies that an absolute path loads the file it names, as it does for <c>AddJsonFile</c>, rather than being
    /// looked up under the application's base directory.
    /// </summary>
    [TestMethod]
    public void AddTextConfigurationFile_WhenPathIsAbsolute_ShouldLoadTheFile()
    {
        using TempFileScope scope = new(Sample, "boduconfig");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddTextConfigurationFile(scope.Path)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that an absolute path passed with a <see langword="null" /> file provider loads the file it names.
    /// </summary>
    [TestMethod]
    public void AddTextConfigurationFile_WhenPathIsAbsolute_ForNullFileProvider_ShouldLoadTheFile()
    {
        using TempFileScope scope = new(Sample, "boduconfig");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddTextConfigurationFile(provider: null, scope.Path)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that an absolute path set through the configure-source callback loads the file it names, as the TOML and
    /// Bencode sources load one.
    /// </summary>
    [TestMethod]
    public void AddTextConfigurationFile_WhenPathIsAbsolute_ForConfigureSource_ShouldLoadTheFile()
    {
        using TempFileScope scope = new(Sample, "boduconfig");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddTextConfigurationFile(source => source.Path = scope.Path)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);
    }

    /// <summary>
    /// Verifies that a required file at a missing absolute path throws <see cref="FileNotFoundException" /> for that
    /// path, rather than for a path under the application's base directory.
    /// </summary>
    [TestMethod]
    public void AddTextConfigurationFile_WhenAbsolutePathIsMissing_ShouldThrowFileNotFoundExceptionForThatPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "missing.boduconfig");
        IConfigurationBuilder builder = new ConfigurationBuilder().AddTextConfigurationFile(path);

        FileNotFoundException ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = builder.Build();
        });

        Assert.Contains($"'{path}'", ex.Message);
        Assert.DoesNotContain(AppContext.BaseDirectory, ex.Message);
    }
}
