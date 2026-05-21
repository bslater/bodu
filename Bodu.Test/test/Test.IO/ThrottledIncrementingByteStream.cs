// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrottledIncrementingByteStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A test stream that simulates slow I/O by sleeping on each read, to trigger cancellation scenarios.
/// </summary>
public sealed class ThrottledIncrementingByteStream
    : IncrementingByteStream
{
    private readonly int throttleDelayMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrottledIncrementingByteStream" /> class with the specified total number of bytes.
    /// </summary>
    /// <param name="size">The total number of bytes the stream will return.</param>
    public ThrottledIncrementingByteStream(int size, int readDelay = 1000)
        : base(size)
    {
        this.throttleDelayMs = readDelay;
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        Thread.Sleep(this.throttleDelayMs); // simulate delay
        return base.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(this.throttleDelayMs, cancellationToken); // simulate delay

        // explicitly handle cancellation
        cancellationToken.ThrowIfCancellationRequested();

        return await base.ReadAsync(buffer, cancellationToken);
    }
}