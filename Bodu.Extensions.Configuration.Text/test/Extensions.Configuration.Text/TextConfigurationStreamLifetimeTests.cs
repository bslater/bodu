// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationStreamLifetimeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Tests;

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
                .AddConfiguration(stream)
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
            .AddConfiguration(stream)
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
            .AddConfiguration(stream)
            .Build();

        Assert.AreEqual("value", configuration["key"]);
    }

    /// <summary>
    /// A read-only, forward-only wrapper around a byte buffer that explicitly disables seeking and length
    /// queries. Used to ensure the provider does not depend on either.
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableStream(byte[] bytes)
        {
            _inner = new MemoryStream(bytes);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
