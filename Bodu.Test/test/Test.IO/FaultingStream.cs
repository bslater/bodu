// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A <see cref="MemoryStream" /> wrapper that throws <see cref="IOException" /> once a
/// configurable number of bytes have been successfully read, simulating a mid-stream
/// connection drop, disk error, or network interruption.
/// </summary>
/// <remarks>
/// <para>
/// All reads up to the <see cref="ThrowAfterBytes" /> threshold succeed normally. The next
/// read call after the threshold has been reached throws <see cref="IOException" /> on every
/// subsequent invocation, so the consumer cannot recover from the same instance.
/// </para>
/// <para>
/// Use this stream in tests that verify error propagation — confirming that a consumer
/// forwards the exception to its caller without deadlocking any internal workers or leaving
/// the consumer in an inconsistent state that prevents reuse.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class FaultingStream 
    : System.IO.MemoryStream
{
    private int _bytesRead;

    /// <summary>
    /// Initialises a new instance of <see cref="FaultingStream" />.
    /// </summary>
    /// <param name="data">The byte array to expose as a stream.</param>
    /// <param name="throwAfterBytes">
    /// The number of bytes that may be read successfully before the stream begins throwing
    /// <see cref="IOException" />. Must be non-negative. A value of zero causes every read to
    /// throw immediately.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="data" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="throwAfterBytes" /> is negative.</exception>
    public FaultingStream(byte[] data, int throwAfterBytes)
        : base(data ?? throw new ArgumentNullException(nameof(data)))
    {
        if (throwAfterBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(throwAfterBytes), "throwAfterBytes must be non-negative.");

        this.ThrowAfterBytes = throwAfterBytes;
    }

    /// <summary>
    /// Gets the number of bytes that may be read successfully before the stream faults.
    /// </summary>
    public int ThrowAfterBytes { get; }

    /// <summary>
    /// Gets the total number of bytes that have been successfully read so far.
    /// </summary>
    public int BytesRead => this._bytesRead;

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        this.ThrowIfFaulted();
        var n = base.Read(buffer, offset, count);
        this._bytesRead += n;
        return n;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        this.ThrowIfFaulted();
        var n = await base.ReadAsync(buffer, cancellationToken);
        this._bytesRead += n;
        return n;
    }

    private void ThrowIfFaulted()
    {
        if (this._bytesRead >= this.ThrowAfterBytes)
            throw new IOException(
                $"Simulated stream fault after {this.ThrowAfterBytes} bytes " +
                $"({this._bytesRead} bytes read so far).");
    }
}
