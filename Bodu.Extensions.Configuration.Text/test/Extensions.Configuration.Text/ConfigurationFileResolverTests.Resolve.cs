// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationFileResolverTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

public sealed partial class ConfigurationFileResolverTests
{
    /// <summary>
    /// Verifies that a rooted path whose directory exists is served by a physical file provider at that directory,
    /// under the file's name alone.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathIsRootedAndItsDirectoryExists_ShouldServeTheFileFromThatDirectory()
    {
        using TempDirectoryScope scope = new();
        string path = scope.WriteFile("settings.toml", Content);

        (IFileProvider fileProvider, string relativePath) = ConfigurationFileResolver.Resolve(new ConfigurationBuilder(), path);

        using var physical = (PhysicalFileProvider)fileProvider;
        Assert.AreEqual(Normalize(scope.Path), Normalize(physical.Root));
        Assert.AreEqual("settings.toml", relativePath);
        Assert.IsTrue(physical.GetFileInfo(relativePath).Exists);
    }

    /// <summary>
    /// Verifies that a rooted path whose directory does not exist yet is served by a physical file provider at the
    /// deepest directory on the path that does exist, under the remaining directories and the file's name, so the
    /// file is found once its directory is created.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRootedDirectoryDoesNotExist_ShouldWalkUpToTheDeepestExistingDirectory()
    {
        using TempDirectoryScope scope = new();
        string path = Path.Combine(scope.Path, "later", "nested", "settings.toml");

        (IFileProvider fileProvider, string relativePath) = ConfigurationFileResolver.Resolve(new ConfigurationBuilder(), path);

        using var physical = (PhysicalFileProvider)fileProvider;
        Assert.AreEqual(Normalize(scope.Path), Normalize(physical.Root));
        Assert.AreEqual(Path.Combine("later", "nested", "settings.toml"), relativePath);
        Assert.IsFalse(physical.GetFileInfo(relativePath).Exists);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Content);

        IFileInfo file = physical.GetFileInfo(relativePath);
        Assert.IsTrue(file.Exists);
        Assert.AreEqual(Path.GetFullPath(path), file.PhysicalPath);
    }

    /// <summary>
    /// Verifies that a path that is not rooted is served by the builder's own file provider, unchanged.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPathIsNotRooted_ShouldReturnTheBuilderFileProviderAndThePath()
    {
        using TempDirectoryScope scope = new();
        using var builderProvider = new PhysicalFileProvider(scope.Path);
        IConfigurationBuilder builder = new ConfigurationBuilder().SetFileProvider(builderProvider);

        (IFileProvider fileProvider, string relativePath) = ConfigurationFileResolver.Resolve(builder, "settings.toml");

        Assert.AreSame(builderProvider, fileProvider);
        Assert.AreEqual("settings.toml", relativePath);
    }
}
