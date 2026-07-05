// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.LoadFromPath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Loader;
using System.Security.Cryptography;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Verifies that the file-path overload loads a plugin assembly into a collectible
    /// <see cref="AssemblyLoadContext" />, so a plugin can be unloaded rather than pinned for the life of the process.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenGivenAssemblyPath_ShouldLoadIntoCollectibleContext()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly.Location, new AllowAllPluginTrustPolicy());

        AssemblyLoadContext? context = AssemblyLoadContext.GetLoadContext(plugin.GetType().Assembly);

        Assert.IsNotNull(context);
        Assert.IsTrue(context!.IsCollectible, "The plugin assembly should be loaded into a collectible context.");
    }

    /// <summary>
    /// Verifies that pinning the SHA-256 digest of the bytes actually on disk admits the plugin through the file-path
    /// overload, confirming the digest the trust policy verifies is the digest of the loaded image.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenGivenAssemblyPathWithMatchingFileHash_ShouldActivateThePlugin()
    {
        string path = TestAssembly.Location;
        byte[] digest = SHA256.HashData(File.ReadAllBytes(path));
        var policy = new FileHashPluginTrustPolicy(new Dictionary<string, byte[]>
        {
            [TestAssembly.GetName().Name!] = digest,
        });

        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(path, policy);

        Assert.AreEqual("Test Plugin", plugin.Name);
    }

    /// <summary>
    /// Verifies that a pinned digest that does not match the bytes on disk rejects the plugin through the file-path
    /// overload with a <see cref="PluginNotTrustedException" />.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenGivenAssemblyPathWithMismatchedFileHash_ShouldThrowPluginNotTrusted()
    {
        var wrongDigest = new byte[32];
        var policy = new FileHashPluginTrustPolicy(new Dictionary<string, byte[]>
        {
            [TestAssembly.GetName().Name!] = wrongDigest,
        });

        Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly.Location, policy);
        });
    }
}
