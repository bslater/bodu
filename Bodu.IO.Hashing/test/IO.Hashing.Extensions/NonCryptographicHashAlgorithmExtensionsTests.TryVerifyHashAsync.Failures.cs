// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.TryVerifyHashAsync.Failures.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Additional <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHashAsync" /> tests that focus on the
/// non-matching, exception-safe, and cancellation false-return branches across every async overload.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{

    // ─── Algorithm-thrown exceptions reach the outer catch and resolve to false ─────────────────

    /// <summary>
    /// Verifies that the async byte-array + byte-array overload returns <see langword="false" /> when the
    /// algorithm itself raises an exception, exercising the <c>catch { return false; }</c> branch around the inner
    /// <c>MemoryStream</c>-backed call.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrows_ForByteArrayByteArrayOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, s_sampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async byte-array + hex overload returns <see langword="false" /> when the algorithm
    /// itself raises an exception, exercising the <c>catch { return false; }</c> branch around the inner
    /// <c>MemoryStream</c>-backed call.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrows_ForByteArrayHexOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, s_sampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + byte-array overload returns <see langword="false" /> when the algorithm
    /// itself raises an exception during <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrows_ForStreamByteArrayOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + hex overload returns <see langword="false" /> when the algorithm itself
    /// raises an exception during <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrows_ForStreamHexOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();
        using MemoryStream stream = new(s_sampleData);

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + <see cref="ReadOnlyMemory{T}" /> overload returns <see langword="false" />
    /// when the algorithm itself raises an exception during
    /// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrows_ForStreamMemoryOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();
        using MemoryStream stream = new(s_sampleData);
        ReadOnlyMemory<byte> expected = s_sampleHash;

        var result = await algorithm.TryVerifyHashAsync(stream, expected);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async string + encoding overload returns <see langword="false" /> when the algorithm
    /// itself raises an exception, exercising the <c>catch { return false; }</c> branch after the encoding step
    /// succeeds.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrows_ForStringEncodedOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        var result = await algorithm.TryVerifyHashAsync(s_sampleString, s_sampleEncoding, s_sampleStringHash);

        Assert.IsFalse(result);
    }
    /// <summary>
    /// Verifies that the async byte-array + byte-array overload returns <see langword="false" /> when the input
    /// hashes to a value different from the expected hash.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayDoesNotMatchByteArray_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, wrong);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async byte-array + hex overload returns <see langword="false" /> when the input hashes to
    /// a value different from the expected hex value.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayDoesNotMatchHex_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, "00000000");

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async byte-array + hex overload returns <see langword="false" /> rather than throwing
    /// when the expected hex is malformed.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenByteArrayHexIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        var result = await algorithm.TryVerifyHashAsync(s_sampleData, "NOT-HEX!!");

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream overload returns <see langword="false" /> when an already-cancelled token is
    /// supplied — cancellation is caught and surfaced as a non-match.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenCancellationIsRequested_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        using CancellationTokenSource cts = new();
        cts.Cancel();

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHash, cts.Token);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + memory overload returns <see langword="false" /> when the stream produces
    /// a hash that does not match the expected memory contents.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamDoesNotMatchMemory_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        ReadOnlyMemory<byte> wrong = BitConverter.GetBytes((uint)999);

        var result = await algorithm.TryVerifyHashAsync(stream, wrong);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + hex overload returns <see langword="false" /> rather than throwing when
    /// reading the stream raises an exception.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamThrowsDuringRead_ForHexOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using AsyncThrowingStream stream = new();

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + memory overload returns <see langword="false" /> rather than throwing when
    /// reading the stream raises an exception.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamThrowsDuringRead_ForMemoryOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using AsyncThrowingStream stream = new();
        ReadOnlyMemory<byte> expected = s_sampleHash;

        var result = await algorithm.TryVerifyHashAsync(stream, expected);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the async stream + byte-array overload returns <see langword="false" /> rather than throwing
    /// when reading the stream raises an exception.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamThrowsDuringRead_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using AsyncThrowingStream stream = new();

        var result = await algorithm.TryVerifyHashAsync(stream, s_sampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// A readable async <see cref="Stream" /> whose every read raises <see cref="IOException" />, used to drive
    /// the exception-safe false-return branch of stream-based async <c>TryVerifyHashAsync</c> overloads.
    /// </summary>
    private sealed class AsyncThrowingStream
        : Stream
    {

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new IOException("Simulated read failure.");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(new IOException("Simulated async read failure."));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromException<int>(new IOException("Simulated async read failure."));

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

    }

}
