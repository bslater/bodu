// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmHelperTests.Body.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Supplementary tests for <see cref="HashAlgorithmHelper" /> that exercise the body of <c>TryHashData</c>,
/// the trim path of the span overload of <c>HashData</c>, and the async stream pump.
/// </summary>
[TestClass]
public sealed class HashAlgorithmHelperBodyTests
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.TryHashData{T}(IHashAlgorithmFactory{T}, ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="true" /> and the number of bytes written when supplied with a destination of the
    /// required size.
    /// </summary>
    [TestMethod]
    public void TryHashData_WhenDestinationFits_ShouldReturnTrue()
    {
        var factory = HashAlgorithmFactory.From<SHA256>(SHA256.Create);
        Span<byte> destination = stackalloc byte[32];

        var result = HashAlgorithmHelper.TryHashData(factory, new byte[] { 1, 2, 3, 4 }, destination, out var bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(32, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.TryHashData{T}(IHashAlgorithmFactory{T}, ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// returns <see langword="false" /> and zero bytes when the destination is smaller than the algorithm's digest size.
    /// </summary>
    [TestMethod]
    public void TryHashData_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var factory = HashAlgorithmFactory.From<SHA256>(SHA256.Create);
        Span<byte> destination = stackalloc byte[8];

        var result = HashAlgorithmHelper.TryHashData(factory, new byte[] { 1, 2, 3 }, destination, out var bytesWritten);

        Assert.IsFalse(result);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, ReadOnlySpan{byte})" /> trims
    /// the returned byte array when the algorithm reports a larger <see cref="HashAlgorithm.HashSize" /> than the
    /// number of bytes it actually writes. This exercises the trim path on lines 79-81 of the helper.
    /// </summary>
    [TestMethod]
    public void HashData_Span_WhenAlgorithmWritesFewerBytesThanReported_ShouldTrimResult()
    {
        var factory = HashAlgorithmFactory.From(() => new ShortHashAlgorithm());

        var result = HashAlgorithmHelper.HashData(factory, new byte[] { 1, 2, 3 });

        Assert.AreEqual(ShortHashAlgorithm.ActualBytes, result.Length);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, ReadOnlySpan{byte})" />
    /// surfaces the <see cref="InvalidOperationException" /> raised by
    /// <see cref="System.Security.Cryptography.HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />
    /// when an algorithm's <see cref="System.Security.Cryptography.HashAlgorithm.TryHashFinal(Span{byte}, out int)" />
    /// reports failure for a destination that is already large enough — a BCL contract violation that the framework
    /// flags as a programmer error.
    /// </summary>
    [TestMethod]
    public void HashData_Span_WhenAlgorithmTryHashFinalReturnsFalseDespiteAdequateBuffer_ShouldThrowExactly()
    {
        var factory = HashAlgorithmFactory.From(() => new RefusingHashAlgorithm());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = HashAlgorithmHelper.HashData(factory, new byte[] { 1, 2, 3 });
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashDataAsync{T}(IHashAlgorithmFactory{T}, Stream, CancellationToken)" />
    /// produces the same digest as the span overload for the same input — exercising the asynchronous stream pump.
    /// </summary>
    [TestMethod]
    public async Task HashDataAsync_Stream_ShouldMatchSpanOverloadForSameContent()
    {
        var factory = HashAlgorithmFactory.From<SHA256>(SHA256.Create);
        var input = Enumerable.Range(0, 4096).Select(i => (byte)(i & 0xFF)).ToArray();

        var viaSpan = HashAlgorithmHelper.HashData(factory, input);
        using var stream = new MemoryStream(input);
        var viaAsync = await HashAlgorithmHelper.HashDataAsync(factory, stream);

        CollectionAssert.AreEqual(viaSpan, viaAsync);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashDataAsync{T}(IHashAlgorithmFactory{T}, Stream, CancellationToken)" />
    /// surfaces an <see cref="OperationCanceledException" /> when an already-cancelled token is supplied. The exact
    /// derived type (<see cref="TaskCanceledException" /> vs <see cref="OperationCanceledException" />) depends on
    /// the underlying stream implementation, so the test asserts on the base type.
    /// </summary>
    [TestMethod]
    public async Task HashDataAsync_WhenAlreadyCancelled_ShouldThrowExactly()
    {
        var factory = HashAlgorithmFactory.From<SHA256>(SHA256.Create);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        try
        {
            _ = await HashAlgorithmHelper.HashDataAsync(factory, stream, cts.Token);
            Assert.Fail("Expected OperationCanceledException was not thrown.");
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    /// <summary>
    /// A <see cref="HashAlgorithm" /> that reports <see cref="HashAlgorithm.HashSize" /> as 32 bytes
    /// (256 bits) but writes only <see cref="ActualBytes" /> bytes from
    /// <see cref="HashAlgorithm.TryHashFinal" />. The mismatch forces the helper through the trim
    /// branch when computing a hash via the span overload.
    /// </summary>
    private sealed class ShortHashAlgorithm
        : HashAlgorithm
    {
        public const int ActualBytes = 8;

        public ShortHashAlgorithm()
        {
            HashSizeValue = 256;
        }

        public override void Initialize() { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }

        protected override byte[] HashFinal() => new byte[ActualBytes];

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < ActualBytes)
            {
                bytesWritten = 0;
                return false;
            }

            destination.Slice(0, ActualBytes).Clear();
            bytesWritten = ActualBytes;
            return true;
        }
    }

    /// <summary>
    /// A <see cref="HashAlgorithm" /> whose <see cref="HashAlgorithm.TryHashFinal" /> always reports failure.
    /// Returning <see langword="false" /> from <c>TryHashFinal</c> when the destination is already at least
    /// <c>HashSize / 8</c> bytes long violates the BCL contract; the framework surfaces this misuse by raising
    /// <see cref="InvalidOperationException" /> from
    /// <see cref="HashAlgorithm.TryComputeHash(ReadOnlySpan{byte}, Span{byte}, out int)" />, and the helper
    /// propagates that exception unchanged.
    /// </summary>
    private sealed class RefusingHashAlgorithm
        : HashAlgorithm
    {
        public RefusingHashAlgorithm()
        {
            HashSizeValue = 256;
        }

        public override void Initialize() { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }

        protected override byte[] HashFinal() => Array.Empty<byte>();

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            return false;
        }
    }
}
