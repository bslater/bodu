// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.TryVerifyHashAsyncCatchPath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.IO;

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
        using var stream = new ThrowOnReadStream(static () => new IOException("Forced read failure."));

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
        using var stream = new ThrowOnReadStream(static () => new IOException("Forced read failure."));

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
}
