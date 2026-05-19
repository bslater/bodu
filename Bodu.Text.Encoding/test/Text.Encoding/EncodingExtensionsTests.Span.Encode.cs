// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Span.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetEncodedByteCount(ReadOnlySpan{char}, System.Text.Encoding)" />
    /// returns the same value as <see cref="System.Text.Encoding.GetByteCount(string)" /> for every canonical
    /// encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [DataTestMethod]
    [DynamicData(nameof(CanonicalEncodings), DynamicDataSourceType.Method)]
    public void GetEncodedByteCount_WhenCalledOnSpan_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        int expected = encoding.GetByteCount(MultiByteText);

        int actual = MultiByteText.AsSpan().GetEncodedByteCount(encoding);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetEncodedByteCount(ReadOnlySpan{char}, System.Text.Encoding)" />
    /// returns zero for an empty span.
    /// </summary>
    [TestMethod]
    public void GetEncodedByteCount_WhenSpanIsEmpty_ShouldReturnZero()
    {
        int actual = ReadOnlySpan<char>.Empty.GetEncodedByteCount(System.Text.Encoding.UTF8);

        Assert.AreEqual(0, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetEncodedByteCount(ReadOnlySpan{char}, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedByteCount_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.AsSpan().GetEncodedByteCount(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToBytes(ReadOnlySpan{char}, System.Text.Encoding)" /> produces
    /// the same bytes as the BCL <see cref="System.Text.Encoding.GetBytes(string)" /> method for every canonical
    /// encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [DataTestMethod]
    [DynamicData(nameof(CanonicalEncodings), DynamicDataSourceType.Method)]
    public void ToBytes_WhenCalledOnSpan_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        byte[] expected = encoding.GetBytes(SampleText);

        byte[] actual = SampleText.AsSpan().ToBytes(encoding);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToBytes(ReadOnlySpan{char}, System.Text.Encoding)" /> returns
    /// the same singleton empty array as <see cref="Array.Empty{T}" /> when the input is empty.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenSpanIsEmpty_ShouldReturnEmptyArray()
    {
        byte[] result = ReadOnlySpan<char>.Empty.ToBytes(System.Text.Encoding.UTF8);

        Assert.AreEqual(0, result.Length);
        Assert.AreSame(Array.Empty<byte>(), result);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToBytes(ReadOnlySpan{char}, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.AsSpan().ToBytes(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// writes the encoded bytes and returns the correct count when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        int written = MultiByteText.AsSpan().EncodeTo(System.Text.Encoding.UTF8, destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(
            System.Text.Encoding.UTF8.GetBytes(MultiByteText),
            destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenDestinationIsOneByteTooSmall_ShouldThrowArgumentException()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        byte[] backing = new byte[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = MultiByteText.AsSpan().EncodeTo(System.Text.Encoding.UTF8, backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        byte[] backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.AsSpan().EncodeTo(null!, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeExactlyTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// succeeds when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void EncodeExactlyTo_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        int written = MultiByteText.AsSpan().EncodeExactlyTo(System.Text.Encoding.UTF8, destination);

        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeExactlyTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is larger than the required size.
    /// </summary>
    [TestMethod]
    public void EncodeExactlyTo_WhenDestinationIsLargerThanRequired_ShouldThrowArgumentException()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(SampleText);
        byte[] backing = new byte[required + 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = SampleText.AsSpan().EncodeExactlyTo(System.Text.Encoding.UTF8, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeExactlyTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is smaller than the required size.
    /// </summary>
    [TestMethod]
    public void EncodeExactlyTo_WhenDestinationIsSmallerThanRequired_ShouldThrowArgumentException()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(SampleText);
        byte[] backing = new byte[required - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = SampleText.AsSpan().EncodeExactlyTo(System.Text.Encoding.UTF8, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.EncodeExactlyTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodeExactlyTo_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        byte[] backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.AsSpan().EncodeExactlyTo(null!, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryEncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="true" /> and reports the byte count when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void TryEncodeTo_WhenDestinationFits_ShouldReturnTrueAndReportCount()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        bool ok = MultiByteText.AsSpan().TryEncodeTo(System.Text.Encoding.UTF8, destination, out int written);

        Assert.IsTrue(ok);
        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryEncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="false" /> and reports zero bytes written when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeTo_WhenDestinationIsOneByteTooSmall_ShouldReturnFalseAndZeroBytesWritten()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        byte[] backing = new byte[required - 1];

        bool ok = MultiByteText.AsSpan().TryEncodeTo(System.Text.Encoding.UTF8, backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryEncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeTo_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        byte[] backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.AsSpan().TryEncodeTo(null!, backing, out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToOwnedBytes(ReadOnlySpan{char}, System.Text.Encoding, MemoryPool{byte})" />
    /// returns an owner whose memory length and contents match the BCL <see cref="System.Text.Encoding.GetBytes(string)" />
    /// output.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [DataTestMethod]
    [DynamicData(nameof(CanonicalEncodings), DynamicDataSourceType.Method)]
    public void ToOwnedBytes_WhenInvoked_ShouldReturnExactSizedOwnerWithEncodedContent(System.Text.Encoding encoding)
    {
        byte[] expected = encoding.GetBytes(MultiByteText);

        using IMemoryOwner<byte> owner = MultiByteText.AsSpan().ToOwnedBytes(encoding);

        Assert.AreEqual(expected.Length, owner.Memory.Length);
        CollectionAssert.AreEqual(expected, owner.Memory.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToOwnedBytes(ReadOnlySpan{char}, System.Text.Encoding, MemoryPool{byte})" />
    /// honours a caller-supplied <see cref="MemoryPool{T}" />.
    /// </summary>
    [TestMethod]
    public void ToOwnedBytes_WhenCustomPoolSupplied_ShouldRentFromIt()
    {
        TrackingMemoryPool pool = new TrackingMemoryPool();

        using (MultiByteText.AsSpan().ToOwnedBytes(System.Text.Encoding.UTF8, pool))
        {
        }

        Assert.AreEqual(1, pool.RentCount);
        Assert.AreEqual(1, pool.DisposeCount);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToOwnedBytes(ReadOnlySpan{char}, System.Text.Encoding, MemoryPool{byte})" />
    /// returns a zero-length owner when the input span is empty.
    /// </summary>
    [TestMethod]
    public void ToOwnedBytes_WhenSpanIsEmpty_ShouldReturnZeroLengthOwner()
    {
        using IMemoryOwner<byte> owner = ReadOnlySpan<char>.Empty.ToOwnedBytes(System.Text.Encoding.UTF8);

        Assert.AreEqual(0, owner.Memory.Length);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.ToOwnedBytes(ReadOnlySpan{char}, System.Text.Encoding, MemoryPool{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToOwnedBytes_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.AsSpan().ToOwnedBytes(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Tracks calls to <see cref="MemoryPool{T}.Rent(int)" /> and <see cref="IMemoryOwner{T}.Dispose" /> so that
    /// tests can assert custom pool integration.
    /// </summary>
    private sealed class TrackingMemoryPool : MemoryPool<byte>
    {
        public int RentCount { get; private set; }

        public int DisposeCount { get; private set; }

        public override int MaxBufferSize => int.MaxValue;

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            RentCount++;
            return new TrackingOwner(this, MemoryPool<byte>.Shared.Rent(minBufferSize));
        }

        protected override void Dispose(bool disposing)
        {
        }

        private sealed class TrackingOwner : IMemoryOwner<byte>
        {
            private readonly TrackingMemoryPool _owner;
            private IMemoryOwner<byte>? _inner;

            public TrackingOwner(TrackingMemoryPool owner, IMemoryOwner<byte> inner)
            {
                _owner = owner;
                _inner = inner;
            }

            public Memory<byte> Memory => _inner?.Memory ?? throw new ObjectDisposedException(nameof(TrackingOwner));

            public void Dispose()
            {
                if (_inner is null) return;
                _inner.Dispose();
                _inner = null;
                _owner.DisposeCount++;
            }
        }
    }
}
