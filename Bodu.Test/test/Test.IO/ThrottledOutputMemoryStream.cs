// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrottledOutputMemoryStream.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A memory stream that simulates slow output writes by sleeping on each write call.
/// </summary>
public sealed class ThrottledOutputMemoryStream
    : System.IO.MemoryStream
{
    private readonly int delayMilliseconds;

    /// <summary>
    /// Initializes a new instance with optional write delay.
    /// </summary>
    /// <param name="delayMilliseconds">The delay in milliseconds to simulate on each write.</param>
    public ThrottledOutputMemoryStream(int delayMilliseconds = 250)
    {
        this.delayMilliseconds = delayMilliseconds;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        Thread.Sleep(delayMilliseconds);
        base.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Thread.Sleep(delayMilliseconds);
        base.Write(buffer);
    }

    /// <inheritdoc />
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await Task.Delay(delayMilliseconds, cancellationToken);
        await base.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delayMilliseconds, cancellationToken);
        await base.WriteAsync(buffer, cancellationToken);
    }
}