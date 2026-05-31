// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingStream.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A <see cref="MemoryStream" /> wrapper that intermittently introduces an async yield on <see cref="ReadAsync" />
/// calls to simulate a stream that occasionally stalls before delivering data — for example, a network source waiting
/// for the next packet.
/// </summary>
/// <remarks>
/// <para>
/// Every <paramref name="stallEveryN" />-th call awaits <see cref="Task.Yield" /> before performing the read, causing
/// the caller to briefly relinquish the thread and continue asynchronously. Real bytes are always returned — zero is
/// never returned unless the stream has genuinely reached end-of-file.
/// </para>
/// <para>
/// This correctly models bursty I/O without violating the <see cref="Stream" /> contract:
/// <see cref="Stream.ReadAsync(Memory{byte},CancellationToken)" /> returning zero bytes is the end-of-stream signal,
/// not a stall signal. Consumers that treat a zero return as EOF — which is correct per the contract — must not receive
/// zero bytes mid-stream.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use.
/// </para>
/// </remarks>
public sealed class IntermittentZeroReadStream 
    : System.IO.MemoryStream
{
    private readonly int stallEveryN;
    private int callCount;

    public const int MaxBytesPerRead = 4;

    public IntermittentZeroReadStream(byte[] data, int stallEveryN = 3)
        : base(data ?? throw new ArgumentNullException(nameof(data)))
    {
        if (stallEveryN < 2)
            throw new ArgumentOutOfRangeException(nameof(stallEveryN), "stallEveryN must be at least 2.");

        this.stallEveryN = stallEveryN;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Synchronous reads are not stalled — stalling only applies to <see cref="ReadAsync" />.
    /// </remarks>
    public override int Read(byte[] buffer, int offset, int count)
        => base.Read(buffer, offset, Math.Min(count, MaxBytesPerRead));

    /// <inheritdoc />
    /// <remarks>
    /// Every <c>stallEveryN</c>-th call yields before reading to simulate a bursty async source. Real bytes are always
    /// returned — zero is never returned before EOF.
    /// </remarks>
    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (++this.callCount % this.stallEveryN == 0)
            await Task.Yield(); // simulate momentary stall without falsely signalling EOF

        return await base.ReadAsync(
            buffer[..Math.Min(buffer.Length, MaxBytesPerRead)],
            cancellationToken);
    }
}