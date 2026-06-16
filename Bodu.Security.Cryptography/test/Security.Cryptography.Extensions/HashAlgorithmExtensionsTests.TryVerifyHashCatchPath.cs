// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.TryVerifyHashCatchPath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Bodu.Test.IO;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Exercises the catch paths of the <see cref="HashAlgorithmExtensions.TryVerifyHash" /> family. Each test
/// triggers an exception inside the underlying <see cref="HashAlgorithmExtensions.VerifyHash" /> call so the
/// catch handler in the corresponding try-pattern overload runs and is recorded as covered.
/// </summary>
public partial class HashAlgorithmExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, byte[], byte[])" /> swallows
    /// the underlying <see cref="ObjectDisposedException" /> raised by a disposed algorithm and returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_ByteArrayBytes_WhenAlgorithmIsDisposed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, SampleHash));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, byte[], string)" /> swallows
    /// the underlying <see cref="ObjectDisposedException" /> raised by a disposed algorithm and returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_ByteArrayHex_WhenAlgorithmIsDisposed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, SampleHex));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, string, Encoding, byte[])" />
    /// swallows the underlying <see cref="ObjectDisposedException" /> raised by a disposed algorithm and returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_StringEncoded_WhenAlgorithmIsDisposed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.IsFalse(algorithm.TryVerifyHash(SampleString, SampleEncoding, SampleStringHash));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// swallows the underlying <see cref="ObjectDisposedException" /> raised by a disposed algorithm and returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_Span_WhenAlgorithmIsDisposed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        ReadOnlySpan<byte> input = SampleData;
        ReadOnlySpan<byte> expected = SampleHash;

        Assert.IsFalse(algorithm.TryVerifyHash(input, expected));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, ReadOnlyMemory{byte}, byte[])" />
    /// swallows the underlying <see cref="ObjectDisposedException" /> raised by a disposed algorithm and returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_Memory_WhenAlgorithmIsDisposed_ShouldReturnFalse()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        ReadOnlyMemory<byte> memory = SampleData;

        Assert.IsFalse(algorithm.TryVerifyHash(memory, SampleHash));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, Stream, byte[])" /> swallows
    /// the underlying exception raised when reading from a faulted stream and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_StreamBytes_WhenStreamThrows_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new ThrowOnReadStream(static () => new IOException("Forced read failure."));

        Assert.IsFalse(algorithm.TryVerifyHash(stream, SampleHash));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, Stream, string)" /> swallows
    /// the underlying exception raised when reading from a faulted stream and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_StreamHex_WhenStreamThrows_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new ThrowOnReadStream(static () => new IOException("Forced read failure."));

        Assert.IsFalse(algorithm.TryVerifyHash(stream, SampleHex));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, byte[], byte[])" /> returns
    /// <see langword="false" /> when the supplied input and the expected hash mismatch, exercising the non-throwing
    /// failure path.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_ByteArrayBytes_WhenHashMismatches_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        byte[] mismatched = BitConverter.GetBytes((uint)123);

        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, mismatched));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, ReadOnlyMemory{byte}, byte[])" />
    /// returns <see langword="false" /> when the computed hash does not match the expected value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_Memory_WhenHashMismatches_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        ReadOnlyMemory<byte> memory = SampleData;
        byte[] mismatched = BitConverter.GetBytes((uint)321);

        Assert.IsFalse(algorithm.TryVerifyHash(memory, mismatched));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, Stream, byte[])" /> returns
    /// <see langword="false" /> when the stream contents do not match the expected hash value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_StreamBytes_WhenHashMismatches_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);
        byte[] mismatched = BitConverter.GetBytes((uint)999);

        Assert.IsFalse(algorithm.TryVerifyHash(stream, mismatched));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, Stream, string)" /> returns
    /// <see langword="false" /> when the stream contents do not match the expected hex value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_StreamHex_WhenHashMismatches_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        using var stream = new MemoryStream(SampleData);

        Assert.IsFalse(algorithm.TryVerifyHash(stream, "00112233"));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, string, Encoding, byte[])" />
    /// returns <see langword="false" /> when the encoded string's hash does not match the expected hash value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_StringEncoded_WhenHashMismatches_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        byte[] mismatched = BitConverter.GetBytes((uint)9999);

        Assert.IsFalse(algorithm.TryVerifyHash(SampleString, SampleEncoding, mismatched));
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.TryVerifyHash(HashAlgorithm, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// returns <see langword="false" /> when the span hash does not match the expected span value.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_Span_WhenHashMismatches_ShouldReturnFalse()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        ReadOnlySpan<byte> input = SampleData;
        ReadOnlySpan<byte> mismatched = BitConverter.GetBytes((uint)8888);

        Assert.IsFalse(algorithm.TryVerifyHash(input, mismatched));
    }
}
