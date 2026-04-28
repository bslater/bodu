// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensionsTests.TryVerifyHash.Catch.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using System.Threading.Tasks;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Tests covering the swallow-and-return-<see langword="false" /> branch of the
/// <see cref="NonCryptographicHashAlgorithmExtensions.TryVerifyHash" /> overloads — where the underlying
/// <c>VerifyHash</c> throws and the try-pattern reports failure to the caller without propagating the exception.
/// </summary>
public partial class NonCryptographicHashAlgorithmExtensionsTests
{
    /// <summary>
    /// Verifies that <c>TryVerifyHash(Stream, byte[])</c> swallows an exception thrown during stream reads and
    /// returns <see langword="false" /> rather than propagating it.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamThrowsDuringRead_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        using ThrowingReadStream stream = new();

        Assert.IsFalse(algorithm.TryVerifyHash(stream, SampleHash));
    }

    /// <summary>
    /// Verifies that <c>TryVerifyHash(Stream, string)</c> swallows an exception thrown during stream reads
    /// when the expected value is a hex string and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenStreamThrowsDuringRead_ForHexOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        using ThrowingReadStream stream = new();

        Assert.IsFalse(algorithm.TryVerifyHash(stream, SampleHex));
    }

    /// <summary>
    /// Verifies that <c>TryVerifyHash(byte[], byte[])</c> swallows exceptions raised by the underlying
    /// algorithm during <c>Append</c> and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsDuringAppend_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, SampleHash));
    }

    /// <summary>
    /// Verifies that <c>TryVerifyHash(byte[], string)</c> swallows exceptions raised during the underlying
    /// hash computation and returns <see langword="false" />, even when the supplied hex is well-formed.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsDuringAppend_ForHexOverload_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash(SampleData, SampleHex));
    }

    /// <summary>
    /// Verifies that <c>TryVerifyHash(byte[], byte[], out bool)</c> swallows exceptions and reports the
    /// failure to the caller via the method return value (with <paramref name="result" /> set to
    /// <see langword="false" />).
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsDuringAppendWithOutBool_ShouldReturnFalseAndSetResultFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        bool succeeded = algorithm.TryVerifyHash(SampleData, SampleHash, out bool result);

        Assert.IsFalse(succeeded);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the span overload of <c>TryVerifyHash</c> also swallows exceptions raised by the
    /// algorithm and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsDuringAppend_ForSpanOverload_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash((ReadOnlySpan<byte>)SampleData, SampleHash));
    }

    /// <summary>
    /// Verifies that the memory overload of <c>TryVerifyHash</c> swallows exceptions raised by the algorithm
    /// and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsDuringAppend_ForMemoryOverload_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash((ReadOnlyMemory<byte>)SampleData, SampleHash));
    }

    /// <summary>
    /// Verifies that the string + encoding overload of <c>TryVerifyHash</c> swallows exceptions raised by the
    /// algorithm and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryVerifyHash_WhenAlgorithmThrowsDuringAppend_ForStringOverload_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        Assert.IsFalse(algorithm.TryVerifyHash(SampleString, SampleEncoding, SampleStringHash));
    }

    /// <summary>
    /// Verifies that <c>VerifyHash(Stream, string)</c> returns <see langword="false" /> when the supplied hex
    /// string is malformed, exercising the <see cref="FormatException" /> catch arm of the hex-decoding step.
    /// </summary>
    [TestMethod]
    public void VerifyHash_WhenStreamHexIsMalformed_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(SampleData);

        Assert.IsFalse(algorithm.VerifyHash(stream, "ZZZZZZZZ"));
    }

    /// <summary>
    /// Verifies that the asynchronous <c>TryVerifyHashAsync(Stream, byte[])</c> overload swallows exceptions
    /// thrown during stream reads and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamThrowsDuringRead_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using ThrowingReadStream stream = new();

        bool result = await algorithm.TryVerifyHashAsync(stream, SampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the asynchronous <c>TryVerifyHashAsync(Stream, string)</c> overload swallows exceptions
    /// thrown during stream reads and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamThrowsDuringRead_ForHexOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using ThrowingReadStream stream = new();

        bool result = await algorithm.TryVerifyHashAsync(stream, SampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the asynchronous <c>TryVerifyHashAsync(Stream, ReadOnlyMemory&lt;byte&gt;)</c> overload
    /// swallows exceptions thrown during stream reads and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenStreamThrowsDuringRead_ForMemoryOverload_ShouldReturnFalse()
    {
        MonitoringNonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using ThrowingReadStream stream = new();

        bool result = await algorithm.TryVerifyHashAsync(stream, (ReadOnlyMemory<byte>)SampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the asynchronous <c>TryVerifyHashAsync(byte[], byte[])</c> overload swallows exceptions
    /// raised by the underlying algorithm during <c>Append</c> and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrowsDuringAppend_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the asynchronous <c>TryVerifyHashAsync(byte[], string)</c> overload swallows exceptions
    /// raised by the underlying algorithm during <c>Append</c> and returns <see langword="false" />, even when
    /// the supplied hex is well-formed.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrowsDuringAppend_ForHexOverload_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        bool result = await algorithm.TryVerifyHashAsync(SampleData, SampleHex);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that the asynchronous <c>TryVerifyHashAsync(string, Encoding, byte[])</c> overload swallows
    /// exceptions raised by the underlying algorithm during <c>Append</c> and returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public async Task TryVerifyHashAsync_WhenAlgorithmThrowsDuringAppend_ForStringOverload_ShouldReturnFalse()
    {
        ThrowingHashAlgorithm algorithm = new();

        bool result = await algorithm.TryVerifyHashAsync(SampleString, SampleEncoding, SampleStringHash);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// A non-cryptographic hash algorithm whose <see cref="Append(ReadOnlySpan{byte})" /> always throws — used
    /// to drive the catch-and-return-false path of the various <c>TryVerifyHash</c> overloads.
    /// </summary>
    private sealed class ThrowingHashAlgorithm : NonCryptographicHashAlgorithm
    {
        public ThrowingHashAlgorithm()
            : base(hashLengthInBytes: 4)
        {
        }

        public override void Append(ReadOnlySpan<byte> source) =>
            throw new InvalidOperationException("Simulated Append failure for TryVerifyHash catch-path coverage.");

        public override void Reset()
        {
        }

        protected override void GetCurrentHashCore(Span<byte> destination)
        {
        }
    }

    /// <summary>
    /// A read-only <see cref="Stream" /> that always throws <see cref="IOException" /> from
    /// <see cref="Stream.Read(byte[], int, int)" />, used to drive the catch-and-return-false path of the
    /// stream <c>TryVerifyHash</c> overloads.
    /// </summary>
    private sealed class ThrowingReadStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new IOException("Simulated read failure for TryVerifyHash catch-path coverage.");

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
