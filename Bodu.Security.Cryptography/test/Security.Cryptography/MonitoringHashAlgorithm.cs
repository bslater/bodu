// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// A test-oriented hash algorithm that computes the sum of all input bytes as a 32-bit unsigned integer, while monitoring usage. This
/// implementation is non-cryptographic and is designed for verifying calls to <see cref="HashAlgorithm" /> methods during testing.
/// </summary>
public class MonitoringHashAlgorithm : HashAlgorithm
{
    private uint hashValue;
    private bool disposed;
    private long bytesProcessed;
    private uint seedValue;

    // Instrumentation counters and events
    public int InitializeCallCount { get; private set; }

    public int HashCoreCallCount { get; private set; }

    public int HashFinalCallCount { get; private set; }

    public int DisposeCallCount { get; private set; }

    public int TryHashFinalCallCount { get; private set; }

    public int HashSizeAccessCount { get; private set; }

    public int HashAccessCount { get; private set; }

    public int HashCoreSpanCallCount { get; private set; }

    public event EventHandler? InitializeCalled;

    public event EventHandler? HashCoreCalled;

    public event EventHandler? HashFinalCalled;

    public event EventHandler? DisposeCalled;

    public event EventHandler? TryHashFinalCalled;

    public event EventHandler? HashSizeAccessed;

    public event EventHandler? HashAccessed;

    public event EventHandler? HashCoreSpanCalled;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringHashAlgorithm" /> class.
    /// </summary>
    public MonitoringHashAlgorithm()
    {
        HashSizeValue = sizeof(uint) * 8;
        bytesProcessed = seedValue = hashValue = 0;
        Initialize();
    }

    /// <summary>
    /// Gets the total number of bytes processed by the algorithm.
    /// </summary>
    public long BytesProcessed => bytesProcessed;

    /// <inheritdoc />
    public override int HashSize
    {
        get
        {
            HashSizeAccessCount++;
            HashSizeAccessed?.Invoke(this, EventArgs.Empty);
            return base.HashSize;
        }
    }

    /// <inheritdoc />
    public override byte[]? Hash
    {
        get
        {
            HashAccessCount++;
            HashAccessed?.Invoke(this, EventArgs.Empty);

            return base.Hash;
        }
    }

    public uint Seed
    {
        get
        {
            ThrowIfDisposed();
            return seedValue;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            seedValue = value;
            Initialize();
        }
    }

    /// <summary>
    /// Initializes or resets the hash algorithm to its initial state.
    /// </summary>
    public override void Initialize()
    {
        ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        this.State = 0;
        this.finalized = false;
#endif
        bytesProcessed = 0;
        hashValue = seedValue;
        InitializeCallCount++;
        InitializeCalled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Processes a block of data and updates the internal hash state.
    /// </summary>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowIfDisposed();

        for (int i = ibStart; i < ibStart + cbSize; i++)
            hashValue += array[i];

        bytesProcessed += cbSize;
        HashCoreCallCount++;
        HashCoreCalled?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();
        HashCoreSpanCallCount++;
        HashCoreSpanCalled?.Invoke(this, EventArgs.Empty);

        foreach (byte b in source)
            hashValue += b;

        bytesProcessed += source.Length;
    }

    /// <summary>
    /// Finalizes the hash computation and returns the computed hash value.
    /// </summary>
    protected override byte[] HashFinal()
    {
        ThrowIfDisposed();
        HashFinalCallCount++;
        HashFinalCalled?.Invoke(this, EventArgs.Empty);
        return BitConverter.GetBytes(hashValue);
    }

    /// <inheritdoc />
    protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
    {
        TryHashFinalCallCount++;
        TryHashFinalCalled?.Invoke(this, EventArgs.Empty);

        if (destination.Length >= sizeof(uint))
        {
            BitConverter.TryWriteBytes(destination, hashValue);
            bytesWritten = sizeof(uint);
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            CryptographicOperations.ZeroMemory(HashValue);
            HashValue = null;
            hashValue = 0;
            HashSizeValue = 0;
            bytesProcessed = 0;
            InitializeCallCount = HashCoreCallCount = HashAccessCount = HashCoreSpanCallCount =
                HashFinalCallCount = HashSizeAccessCount = TryHashFinalCallCount = 0;
        }

        disposed = true;
        DisposeCallCount++;
        DisposeCalled?.Invoke(this, EventArgs.Empty);
        base.Dispose(disposing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (State != 0)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (this.disposed)
            throw new ObjectDisposedException(nameof(MonitoringHashAlgorithm));
#endif
    }
}
