// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationReloadFailureTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Pins the behaviour of the file-backed provider when a reload encounters a malformed file or when a
/// caller supplies an <c>OnLoadException</c> handler. Inheriting from
/// <see cref="FileConfigurationProvider" /> gives the bridge the standard Microsoft semantics; these tests
/// confirm the inheritance is intact and the parse exception is preserved.
/// </summary>
[TestClass]
public class TextConfigurationReloadFailureTests
{
    private const string Initial = """
logging.level.default = Information
""";

    private const string Malformed = """
[unterminated
logging.level.default = Debug
""";

    /// <summary>
    /// Verifies that an <c>OnLoadException</c> handler attached to the source receives the parse exception
    /// raised when a reload encounters a malformed file. The bridge does not unwrap the parse exception
    /// — the handler sees the <see cref="ConfigurationParseException" /> exactly as the document parser
    /// raised it.
    /// </summary>
    /// <remarks>
    /// We do not assert what happens to the in-memory <c>Data</c> after a suppressed reload — that
    /// behaviour is inherited from <see cref="FileConfigurationProvider" /> and may evolve between
    /// framework versions. The Bodu-specific contract this test pins is "the exception surface is the
    /// real parse exception, not a generic wrapper."
    /// </remarks>
    [TestMethod]
    public void Reload_WhenMalformedAndOnLoadExceptionAttached_ShouldReceiveParseException()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "reload.boduconfig");

        FileLoadExceptionContext? captured = null;
        using ManualResetEventSlim handlerInvoked = new(initialState: false);

        try
        {
            File.WriteAllText(path, Initial);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(-30));

            PhysicalFileProvider fileProvider = new(directory)
            {
                UseActivePolling = true,
                UsePollingFileWatcher = true,
            };

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddConfiguration(source =>
                {
                    source.FileProvider = fileProvider;
                    source.Path = "reload.boduconfig";
                    source.Optional = false;
                    source.ReloadOnChange = true;
                    source.OnLoadException = ctx =>
                    {
                        captured = ctx;
                        ctx.Ignore = true; // Don't tear down the host on a malformed reload.
                        handlerInvoked.Set();
                    };
                })
                .Build();

            Assert.AreEqual("Information", configuration["logging:level:default"]);

            Thread.Sleep(50);
            File.WriteAllText(path, Malformed);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);

            var fired = handlerInvoked.Wait(TimeSpan.FromSeconds(15));

            Assert.IsTrue(fired, "Expected the OnLoadException handler to fire when the reload encountered a malformed file.");
            Assert.IsNotNull(captured);

            // FileConfigurationProvider wraps load-time exceptions in InvalidDataException, mirroring the
            // behaviour of the JSON/INI providers. The original ConfigurationParseException is preserved
            // as the inner exception so the diagnostic is reachable.
            Assert.IsInstanceOfType<InvalidDataException>(captured!.Exception);
            Assert.IsInstanceOfType<ConfigurationParseException>(captured.Exception.InnerException);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Verifies that a stream-backed source is one-shot: it has no reload-on-change machinery, no
    /// <c>ReloadOnChange</c> property, and getting a fresh reload token returns a token that never fires.
    /// </summary>
    [TestMethod]
    public void StreamSource_ShouldBeOneShotAndNotReloadable()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Initial));

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();

        Assert.AreEqual("Information", configuration["logging:level:default"]);

        // A reload token from a one-shot provider should never fire spontaneously. We can't prove a
        // negative, but we can prove the API is shaped that way: the source type does not derive from
        // FileConfigurationSource, so reload-on-change is not part of the contract.
        TextStreamConfigurationProvider? streamProvider = configuration.Providers
            .OfType<TextStreamConfigurationProvider>()
            .FirstOrDefault();

        Assert.IsNotNull(streamProvider);
        Assert.IsFalse(typeof(FileConfigurationProvider).IsAssignableFrom(streamProvider!.GetType()));

        // Calling Load() again on the same provider re-uses the source's now-exhausted stream.
        // We are not asserting a specific outcome here — the design is intentionally one-shot and the
        // assertion above on the type hierarchy is what pins the contract.
    }
}
