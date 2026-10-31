// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationFileResolverTests.OpenRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class ConfigurationFileResolverTests
{
    /// <summary>
    /// Verifies that, without a file provider, an existing file is opened through the file system.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFileProviderIsNullAndTheFileExists_ShouldReadItThroughTheFileSystem()
    {
        using TempFileScope scope = new(Content, "toml");

        using Stream? stream = ConfigurationFileResolver.OpenRead(null, scope.Path);

        Assert.IsNotNull(stream);
        using var reader = new StreamReader(stream);
        Assert.AreEqual(Content, reader.ReadToEnd());
    }

    /// <summary>
    /// Verifies that, without a file provider, a missing file is reported as <see langword="null" /> rather than
    /// thrown.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFileProviderIsNullAndTheFileIsMissing_ShouldReturnNull()
    {
        using TempDirectoryScope scope = new();

        Stream? stream = ConfigurationFileResolver.OpenRead(null, scope.Combine("missing.toml"));

        Assert.IsNull(stream);
    }

    /// <summary>
    /// Verifies that a file the file provider serves is opened through that provider.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFileProviderServesTheFile_ShouldReturnItsStream()
    {
        using TempDirectoryScope scope = new();
        _ = scope.WriteFile("settings.toml", Content);
        using var fileProvider = new PhysicalFileProvider(scope.Path);

        using Stream? stream = ConfigurationFileResolver.OpenRead(fileProvider, "settings.toml");

        Assert.IsNotNull(stream);
        using var reader = new StreamReader(stream);
        Assert.AreEqual(Content, reader.ReadToEnd());
    }

    /// <summary>
    /// Verifies that a file the file provider does not serve is reported as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFileProviderDoesNotServeTheFile_ShouldReturnNull()
    {
        using TempDirectoryScope scope = new();
        using var fileProvider = new PhysicalFileProvider(scope.Path);

        Stream? stream = ConfigurationFileResolver.OpenRead(fileProvider, "missing.toml");

        Assert.IsNull(stream);
    }
}
