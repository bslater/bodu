// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.TryVerifyHashAsyncCatchPath.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Exercises the asynchronous catch and cancellation paths of the
/// <see cref="HashAlgorithmExtensions.TryVerifyHashAsync" /> family.
/// </summary>
public partial class HashAlgorithmExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHashAsync(HashAlgorithm, Stream, byte[], CancellationToken)" />
    /// returns <see langword="false" /> when the underlying read operation throws, exercising the catch path of the
    /// generated state machine.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_StreamBytes_WhenStreamThrows_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new ThrowingAsyncStream();

        var result = await algorithm.TryVerifyHashAsync(stream, SampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHashAsync(HashAlgorithm, Stream, string, CancellationToken)" />
    /// returns <see langword="false" /> when the underlying read operation throws.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_StreamHex_WhenStreamThrows_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new ThrowingAsyncStream();

        var result = await algorithm.TryVerifyHashAsync(stream, SampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHashAsync(HashAlgorithm, byte[], byte[], CancellationToken)" />
    /// resolves to <see langword="false" /> when the cancellation token is already cancelled, taking the early-return path
    /// without invoking the synchronous delegate.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_ByteArrayBytes_WhenAlreadyCancelled_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash, cts.Token);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHashAsync(HashAlgorithm, byte[], string, CancellationToken)" />
    /// resolves to <see langword="false" /> when the cancellation token is already cancelled, taking the early-return path
    /// without invoking the synchronous delegate.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_ByteArrayHex_WhenAlreadyCancelled_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await algorithm.TryVerifyHashAsync(SampleData, SampleHex, cts.Token);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHashAsync(HashAlgorithm, string, System.Text.Encoding, byte[], CancellationToken)" />
    /// resolves to <see langword="false" /> when the cancellation token is already cancelled.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_StringEncoded_WhenAlreadyCancelled_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, SampleStringHash, cts.Token);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// A read-only stream whose asynchronous reads always throw <see cref="IOException" />, used to drive the catch
    /// path of the async <c>TryVerifyHashAsync</c> generated state machines.
    /// </summary>
    private sealed class ThrowingAsyncStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new IOException("Forced read failure.");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromException<int>(new IOException("Forced async read failure."));

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("Forced async read failure."));

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
