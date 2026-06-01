// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// A test-oriented hash algorithm that computes the sum of all input bytes as a 32-bit unsigned integer, while
/// monitoring usage. This implementation is non-cryptographic and is designed for verifying calls to
/// <see cref="HashAlgorithm" /> methods during testing.
/// </summary>
public class MonitoringHashAlgorithm
    : HashAlgorithm
{
    private uint _hashValue;
    private bool _disposed;
    private long _bytesProcessed;
    private uint _seedValue;

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
    /// Initialises a new instance of the <see cref="MonitoringHashAlgorithm" /> class.
    /// </summary>
    public MonitoringHashAlgorithm()
    {
        HashSizeValue = sizeof(uint) * 8;
        _bytesProcessed = _seedValue = _hashValue = 0;
        Initialize();
    }

    /// <summary>
    /// Gets the total number of bytes processed by the algorithm.
    /// </summary>
    public long BytesProcessed => _bytesProcessed;

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
            return _seedValue;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            _seedValue = value;
            Initialize();
        }
    }

    /// <summary>
    /// Initialises or resets the hash algorithm to its initial state.
    /// </summary>
    public override void Initialize()
    {
        ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        this.State = 0;
        this.finalized = false;
#endif
        _bytesProcessed = 0;
        _hashValue = _seedValue;
        InitializeCallCount++;
        InitializeCalled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Processes a block of data and updates the internal hash state.
    /// </summary>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowIfDisposed();

        for (var i = ibStart; i < ibStart + cbSize; i++)
            _hashValue += array[i];

        _bytesProcessed += cbSize;
        HashCoreCallCount++;
        HashCoreCalled?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();
        HashCoreSpanCallCount++;
        HashCoreSpanCalled?.Invoke(this, EventArgs.Empty);

        foreach (var b in source)
            _hashValue += b;

        _bytesProcessed += source.Length;
    }

    /// <summary>
    /// Finalizes the hash computation and returns the computed hash value.
    /// </summary>
    protected override byte[] HashFinal()
    {
        ThrowIfDisposed();
        HashFinalCallCount++;
        HashFinalCalled?.Invoke(this, EventArgs.Empty);
        return BitConverter.GetBytes(_hashValue);
    }

    /// <inheritdoc />
    protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
    {
        TryHashFinalCallCount++;
        TryHashFinalCalled?.Invoke(this, EventArgs.Empty);

        if (destination.Length >= sizeof(uint))
        {
            BitConverter.TryWriteBytes(destination, _hashValue);
            bytesWritten = sizeof(uint);
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            CryptographicOperations.ZeroMemory(HashValue);
            HashValue = null;
            _hashValue = 0;
            HashSizeValue = 0;
            _bytesProcessed = 0;
            InitializeCallCount = HashCoreCallCount = HashAccessCount = HashCoreSpanCallCount =
                HashFinalCallCount = HashSizeAccessCount = TryHashFinalCallCount = 0;
        }

        _disposed = true;
        DisposeCallCount++;
        DisposeCalled?.Invoke(this, EventArgs.Empty);
        base.Dispose(disposing);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (State != 0)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.Crypt_Invalid_ReconfigurationNotAllowed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (this.disposed)
            throw new ObjectDisposedException(nameof(MonitoringHashAlgorithm));
#endif

}
