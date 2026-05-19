// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationReloadTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies that <c>reloadOnChange</c> behaviour propagates through the bridge, including reload-token signal
/// delivery when the underlying configuration file is replaced on disk.
/// </summary>
[TestClass]
public class TextConfigurationReloadTests
{
    private const string InitialContent = """
logging.level.default = Information
""";

    private const string UpdatedContent = """
logging.level.default = Debug
""";

    /// <summary>
    /// Verifies that when the underlying file changes and <c>reloadOnChange</c> is enabled, the reload token
    /// fires and the new value becomes observable.
    /// </summary>
    [TestMethod]
    public void Reload_WhenFileChangesOnDisk_ShouldFireToken()
    {
        string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "reload.boduconfig");

        try
        {
            File.WriteAllText(path, InitialContent);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(-30));

            PhysicalFileProvider fileProvider = new(directory)
            {
                UseActivePolling = true,
                UsePollingFileWatcher = true,
            };

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddConfiguration(fileProvider, "reload.boduconfig", targetPath: null, optional: false, reloadOnChange: true)
                .Build();

            Assert.AreEqual("Information", configuration["logging:level:default"]);

            using ManualResetEventSlim signal = new(initialState: false);
            using IDisposable registration = Microsoft.Extensions.Primitives.ChangeToken.OnChange(
                configuration.GetReloadToken,
                () => signal.Set());

            // Ensure the next write has a clearly newer mtime than the original.
            Thread.Sleep(50);
            File.WriteAllText(path, UpdatedContent);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);

            bool reloaded = signal.Wait(TimeSpan.FromSeconds(15));

            Assert.IsTrue(reloaded, "Expected the reload token to fire after the file changed on disk.");
            Assert.AreEqual("Debug", configuration["logging:level:default"]);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }
}
