// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.LoadFromFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Loads the valid fixture plugin through <see cref="NotableDatePluginLoader.LoadFromFile" />, disposes the handle,
    /// and returns a weak reference to the plugin's load context. Kept non-inlined so no JIT-extended local roots the
    /// context past the call.
    /// </summary>
    /// <returns>A weak reference to the disposed plugin's load context.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndDisposeFixturePlugin()
    {
        NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
            new AllowAllPluginTrustPolicy());

        WeakReference reference = new(AssemblyLoadContext.GetLoadContext(handle.Plugin.GetType().Assembly));
        handle.Dispose();

        return reference;
    }

    /// <summary>
    /// Verifies that <see cref="NotableDatePluginLoader.LoadFromFile" /> activates the plugin and hands back an
    /// unloadable handle.
    /// </summary>
    [TestMethod]
    public void LoadFromFile_WhenGivenFixtureAssemblyPath_ShouldReturnUnloadableHandle()
    {
        using NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
            new AllowAllPluginTrustPolicy());

        Assert.AreEqual("Fixture Plugin", handle.Plugin.Name);
        Assert.IsTrue(handle.IsUnloadable);
    }

    /// <summary>
    /// Verifies that disposing the handle unloads the plugin's collectible load context once nothing references the
    /// plugin's types — closing the gap where a successfully loaded plugin was pinned for the life of the process.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenNothingReferencesThePlugin_ShouldUnloadTheLoadContext()
    {
        WeakReference context = LoadAndDisposeFixturePlugin();

        for (var attempt = 0; context.IsAlive && attempt < 10; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.IsFalse(context.IsAlive, "The plugin load context should be reclaimed after the handle is disposed.");
    }

    /// <summary>
    /// Verifies that disposal is idempotent and flips <see cref="NotableDatePluginHandle.IsUnloadable" /> off.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldBeIdempotent()
    {
        NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
            new AllowAllPluginTrustPolicy());

        handle.Dispose();
        Assert.IsFalse(handle.IsUnloadable);

        handle.Dispose();
        Assert.IsFalse(handle.IsUnloadable);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> or empty path and a <see langword="null" /> policy are rejected.
    /// </summary>
    [TestMethod]
    public void LoadFromFile_WhenArgumentsAreInvalid_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFromFile(null!, new AllowAllPluginTrustPolicy());
        });
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFromFile(string.Empty, new AllowAllPluginTrustPolicy());
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFromFile(
                FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
                null!);
        });
    }
}
