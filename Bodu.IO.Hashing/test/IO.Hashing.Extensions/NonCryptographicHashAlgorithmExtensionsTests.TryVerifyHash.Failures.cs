// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.TryVerifyHash.Failures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Additional <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHash" /> tests that focus on the
/// non-matching and exception-safe false-return branches across every overload, including the byte/memory/span/
/// stream and the hex-string entry points.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{

    /// <summary>
    /// Verifies that the byte-array + hex overload returns <see langword="false" /> when the algorithm itself
    /// raises an exception during <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />,
    /// exercising the <c>catch { return false; }</c> branch.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrows_ForByteArrayHexOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleData, s_sampleHex));
    }

    // ─── Algorithm-thrown exceptions reach the outer catch and resolve to false ─────────────────

    /// <summary>
    /// Verifies that the byte-array overload returns <see langword="false" /> when the algorithm itself raises an
    /// exception during <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" />, exercising the
    /// <c>catch { return false; }</c> branch.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrows_ForByteArrayOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleData, s_sampleHash));
    }

    /// <summary>
    /// Verifies that the <see cref="ReadOnlyMemory{T}" /> overload returns <see langword="false" /> when the
    /// algorithm raises an exception, exercising the <c>catch { return false; }</c> branch.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrows_ForMemoryOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();
        ReadOnlyMemory<byte> input = s_sampleData;

        Assert.IsFalse(algorithm.TryVerifyHash(input, s_sampleHash));
    }

    /// <summary>
    /// Verifies that the <see cref="ReadOnlySpan{T}" /> overload returns <see langword="false" /> when the
    /// algorithm raises an exception, exercising the <c>catch { return false; }</c> branch.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrows_ForSpanOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();
        ReadOnlySpan<byte> input = s_sampleData;
        ReadOnlySpan<byte> expected = s_sampleHash;

        Assert.IsFalse(algorithm.TryVerifyHash(input, expected));
    }

    /// <summary>
    /// Verifies that the string + encoding overload returns <see langword="false" /> when the algorithm raises an
    /// exception, exercising the <c>catch { return false; }</c> branch after encoding succeeds.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrows_ForStringEncodedOverload_ShouldReturnFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleString, s_sampleEncoding, s_sampleStringHash));
    }

    /// <summary>
    /// Verifies that the out-bool overload returns <see langword="false" /> and sets <paramref name="result" /> to
    /// <see langword="false" /> when the algorithm raises an exception, exercising the
    /// <c>catch { return false; }</c> branch.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsWithOutBool_ShouldReturnFalseAndLeaveResultFalse()
    {
        ThrowingNonCryptographicHashAlgorithm algorithm = new();

        var succeeded = algorithm.TryVerifyHash(s_sampleData, s_sampleHash, out var result);

        Assert.IsFalse(succeeded);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the byte-array overload returns <see langword="false" /> when the expected hash byte array has
    /// the wrong length.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayExpectedHashLengthMismatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrongLength = new byte[] { 0x00, 0x00 };

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleData, wrongLength));
    }

    /// <summary>
    /// Verifies that the byte-array hex overload returns <see langword="false" /> when the expected hex decodes to
    /// a byte sequence whose length does not match the digest.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenByteArrayExpectedHexLengthMismatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleData, "AABB"));
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHash(NonCryptographicHashAlgorithm, ReadOnlyMemory{byte}, byte[])" />
    /// returns <see langword="false" /> when the memory buffer does not produce the expected hash.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenMemoryDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlyMemory<byte> memory = s_sampleData;
        var wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.TryVerifyHash(memory, wrong));
    }
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHash(NonCryptographicHashAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// returns <see langword="false" /> when the computed hash does not match the supplied span.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenSpanDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlySpan<byte> input = s_sampleData;
        ReadOnlySpan<byte> wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.TryVerifyHash(input, wrong));
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHash(NonCryptographicHashAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// returns <see langword="false" /> when the expected hash has the wrong length.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenSpanLengthMismatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlySpan<byte> input = s_sampleData;
        ReadOnlySpan<byte> wrongLength = [0x00, 0x00];

        Assert.IsFalse(algorithm.TryVerifyHash(input, wrongLength));
    }

    /// <summary>
    /// Verifies that the stream + byte-array overload returns <see langword="false" /> when the stream content
    /// produces a different hash from the supplied expected value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamDoesNotMatchByteArray_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);
        var wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.TryVerifyHash(stream, wrong));
    }

    /// <summary>
    /// Verifies that the stream + hex overload returns <see langword="false" /> when the stream content produces a
    /// different hash from the supplied hex value, and that a malformed hex string returns <see langword="false" />
    /// rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamHexDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        Assert.IsFalse(algorithm.TryVerifyHash(stream, "00000000"));
    }

    /// <summary>
    /// Verifies that the stream + hex overload swallows a malformed hex string and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamHexIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(s_sampleData);

        Assert.IsFalse(algorithm.TryVerifyHash(stream, "NOT-HEX!!"));
    }

    /// <summary>
    /// Verifies that the stream + hex overload returns <see langword="false" /> rather than throwing when reading
    /// the stream raises an exception.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamThrowsDuringRead_ForHexOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using ThrowingStream stream = new();

        Assert.IsFalse(algorithm.TryVerifyHash(stream, s_sampleHex));
    }

    /// <summary>
    /// Verifies that the stream + byte-array overload returns <see langword="false" /> rather than throwing when
    /// reading the stream raises an exception.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamThrowsDuringRead_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using ThrowingStream stream = new();

        Assert.IsFalse(algorithm.TryVerifyHash(stream, s_sampleHash));
    }

    /// <summary>
    /// Verifies that the string + encoding overload returns <see langword="false" /> when the encoded input
    /// produces a different hash from the supplied expected value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStringEncodedDoesNotMatch_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        var wrong = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.TryVerifyHash(s_sampleString, s_sampleEncoding, wrong));
    }

    /// <summary>
    /// A readable <see cref="Stream" /> whose every read raises <see cref="IOException" />, used to drive the
    /// exception-safe false-return branch of stream-based <c>TryVerifyHash</c> overloads.
    /// </summary>
    private sealed class ThrowingStream
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

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

    }

}
