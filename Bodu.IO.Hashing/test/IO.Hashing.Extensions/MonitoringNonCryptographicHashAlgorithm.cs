// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringNonCryptographicHashAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// A test-oriented non-cryptographic hash algorithm that computes the sum of all input bytes as a 32-bit unsigned
/// integer, while monitoring usage. This implementation is designed for verifying calls to
/// <see cref="NonCryptographicHashAlgorithm" /> extension methods during testing.
/// </summary>
/// <remarks>
/// The hash output is <c>BitConverter.GetBytes((uint)sum)</c> in the platform byte order, making the expected hash of
/// any input trivially computable without reference vectors.
/// </remarks>
public sealed class MonitoringNonCryptographicHashAlgorithm
    : NonCryptographicHashAlgorithm
{

    private uint _sum;

    /// <summary>
    /// Initialises a new instance of <see cref="MonitoringNonCryptographicHashAlgorithm" /> with a 4-byte output.
    /// </summary>
    public MonitoringNonCryptographicHashAlgorithm()
        : base(hashLengthInBytes: sizeof(uint))
    {
    }

    /// <summary>
    /// Gets the number of times <see cref="Append(ReadOnlySpan{byte})" /> has been called on this instance.
    /// </summary>
    public int AppendCallCount { get; private set; }

    /// <summary>
    /// Gets the total number of bytes fed into this instance across all <see cref="Append(ReadOnlySpan{byte})" />
    /// calls.
    /// </summary>
    public long BytesAppended { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="GetCurrentHashCore" /> has been invoked.
    /// </summary>
    public int GetCurrentHashCoreCallCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="Reset()" /> has been called on this instance.
    /// </summary>
    public int ResetCallCount { get; private set; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        foreach (var b in source)
            _sum += b;

        BytesAppended += source.Length;
        AppendCallCount++;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _sum = 0;
        BytesAppended = 0;
        ResetCallCount++;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        BitConverter.TryWriteBytes(destination, _sum);
        GetCurrentHashCoreCallCount++;
    }

}
