// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmHelperTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="HashAlgorithmHelper" /> static helpers covering one-shot
/// hashing through an <see cref="IHashAlgorithmFactory{T}" />.
/// </summary>
[TestClass]
public sealed class HashAlgorithmHelperTests
{
    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, System.ReadOnlySpan{byte})" />
    /// with a <see langword="null" /> factory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void HashData_Span_WhenFactoryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = HashAlgorithmHelper.HashData<MD5>(null!, []);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, Stream)" />
    /// with a <see langword="null" /> factory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void HashData_Stream_WhenFactoryIsNull_ShouldThrowExactly()
    {
        using var stream = new MemoryStream();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = HashAlgorithmHelper.HashData<MD5>(null!, stream);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, Stream)" />
    /// with a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void HashData_Stream_WhenStreamIsNull_ShouldThrowExactly()
    {
        DelegateHashAlgorithmFactory<MD5> factory = HashAlgorithmFactory.From<MD5>(MD5.Create);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = HashAlgorithmHelper.HashData(factory, (Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashDataAsync{T}" /> with a
    /// <see langword="null" /> factory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task HashDataAsync_WhenFactoryIsNull_ShouldThrowExactly()
    {
        using var stream = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await HashAlgorithmHelper.HashDataAsync<MD5>(null!, stream);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashDataAsync{T}" /> with a
    /// <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public async Task HashDataAsync_WhenStreamIsNull_ShouldThrowExactly()
    {
        DelegateHashAlgorithmFactory<MD5> factory = HashAlgorithmFactory.From<MD5>(MD5.Create);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await HashAlgorithmHelper.HashDataAsync(factory, (Stream)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.TryHashData{T}" /> with a <see langword="null" />
    /// factory throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryHashData_WhenFactoryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = HashAlgorithmHelper.TryHashData<MD5>(null!, [], [], out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, System.ReadOnlySpan{byte})" />
    /// produces the same digest as a directly-constructed algorithm for the same input — guards
    /// against the helper accidentally truncating, mis-copying, or zeroing the result.
    /// </summary>
    [TestMethod]
    public void HashData_Span_ShouldMatchDirectAlgorithmOutput()
    {
        var input = Enumerable.Range(0, 96).Select(i => (byte)i).ToArray();

        var viaHelper = HashAlgorithmHelper.HashData(
            HashAlgorithmFactory.From<MD5>(MD5.Create),
            input);

        var direct = MD5.HashData(input);

        CollectionAssert.AreEqual(direct, viaHelper);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, Stream)" />
    /// produces the same digest as the span overload for the same content — confirms that the
    /// internal stream pump uses the buffer pool correctly and finalises with an empty
    /// <c>TransformFinalBlock</c>.
    /// </summary>
    [TestMethod]
    public void HashData_Stream_ShouldMatchSpanOverloadForSameContent()
    {
        var input = Enumerable.Range(0, 32 * 1024).Select(i => (byte)(i & 0xFF)).ToArray();

        var viaSpan = HashAlgorithmHelper.HashData(
            HashAlgorithmFactory.From<SHA256>(SHA256.Create),
            input);

        using var stream = new MemoryStream(input);
        var viaStream = HashAlgorithmHelper.HashData(
            HashAlgorithmFactory.From<SHA256>(SHA256.Create),
            stream);

        CollectionAssert.AreEqual(viaSpan, viaStream);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmHelper.HashData{T}(IHashAlgorithmFactory{T}, System.ReadOnlySpan{byte})" />
    /// disposes the algorithm produced by the factory after computing the hash. Regression guard
    /// for resource leaks introduced if the helper ever stops using <c>using</c>.
    /// </summary>
    [TestMethod]
    public void HashData_Span_ShouldDisposeAlgorithmAfterUse()
    {
        var probe = new DisposalProbe();
        DelegateHashAlgorithmFactory<DisposalProbe> factory = HashAlgorithmFactory.From(() => probe);

        _ = HashAlgorithmHelper.HashData(factory, []);

        Assert.IsTrue(probe.DisposedCount > 0,
            "HashAlgorithmHelper.HashData should dispose the algorithm it constructs.");
    }

    /// <summary>
    /// A <see cref="HashAlgorithm" /> probe that counts how many times <see cref="HashAlgorithm.Dispose()" />
    /// is invoked. Used to verify <see cref="HashAlgorithmHelper" /> deterministically disposes
    /// instances it constructs.
    /// </summary>
    private sealed class DisposalProbe
        : HashAlgorithm
    {
        // HashSizeValue is in bits and must match the byte length returned by HashFinal —
        // the framework's TryHashFinal validates this and throws InvalidOperationException
        // ("The algorithm's implementation is incorrect.") on mismatch.
        private const int HashSizeBits = 256;
        private const int HashSizeBytes = HashSizeBits / 8;

        public int DisposedCount { get; private set; }

        public DisposalProbe()
        {
            this.HashSizeValue = HashSizeBits;
        }

        public override void Initialize() { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }

        protected override byte[] HashFinal() => new byte[HashSizeBytes];

        protected override void Dispose(bool disposing)
        {
            this.DisposedCount++;
            base.Dispose(disposing);
        }
    }
}
