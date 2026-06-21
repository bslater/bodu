// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationStreamLifetimeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Pins the lifetime contract of the stream-backed provider: caller-owned streams must remain open and
/// re-readable after the configuration is built, mirroring the conventions of
/// <c>Microsoft.Extensions.Configuration.Json</c>'s <c>AddJsonStream</c>.
/// </summary>
[TestClass]
public class TextConfigurationStreamLifetimeTests
{
    private const string Sample = """
key = value
""";

    /// <summary>
    /// Verifies that the stream provider does not close the caller-supplied stream after building. The
    /// caller retains ownership and is responsible for disposing the stream when the wider scope ends.
    /// </summary>
    [TestMethod]
    public void Build_WhenStreamProvided_ShouldNotCloseStreamAfterLoad()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddTextConfigurationStream(stream)
                .Build();

            // Reading any property forces the lazy load. Then the stream must still be open.
            Assert.AreEqual("value", configuration["key"]);
            Assert.IsTrue(stream.CanRead, "Expected the caller-owned stream to remain open after Build.");
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the stream provider reads from the stream's current position. A stream pre-positioned
    /// past its content yields an empty configuration view (since the remaining bytes parse as the empty
    /// document) rather than rewinding silently.
    /// </summary>
    [TestMethod]
    public void Build_WhenStreamPositionIsAtEnd_ShouldYieldEmptyConfigurationWithoutRewinding()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Sample));
        stream.Position = stream.Length;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationStream(stream)
            .Build();

        Assert.IsNull(configuration["key"]);
    }

    /// <summary>
    /// Verifies that a non-seekable stream still loads correctly. The provider must not assume seek
    /// capability — many real-world streams (network streams, pipes) are forward-only.
    /// </summary>
    [TestMethod]
    public void Build_WhenStreamIsNonSeekable_ShouldStillLoad()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(Sample);
        using NonSeekableStream stream = new(bytes);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTextConfigurationStream(stream)
            .Build();

        Assert.AreEqual("value", configuration["key"]);
    }
}
