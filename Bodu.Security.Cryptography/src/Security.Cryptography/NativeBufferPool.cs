// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NativeBufferPool.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Reuses the native buffers that hold the working memory of the memory-hard key-derivation functions - Argon2's memory
/// matrix and scrypt's <c>V</c> - so a steady stream of derivations neither allocates that memory per call nor pays the
/// operating system for fresh pages each time.
/// </summary>
/// <remarks>
/// <para>
/// Each renter clears what it wrote before the buffer comes back, so every buffer the pool holds is entirely zero:
/// nothing derived from a password outlives the derivation that produced it. The pool keeps at most
/// <see cref="MaxRetainedBuffers" /> buffers of at most <see cref="MaxRetainedBufferBytes" /> each, whichever function
/// used them last, and releases any buffer left unused for <see cref="IdleTimeout" />, so a process that stops deriving
/// gives the memory back.
/// </para>
/// <para>
/// Setting the <see cref="DisableReuseSwitchName" /> <see cref="AppContext" /> switch turns retention off for every
/// renter: each buffer is then freed when its derivation ends. The switch keeps the name it was introduced under, for
/// Argon2, and is read once, when the shared pool is created.
/// </para>
/// <para>
/// Allocation from native memory keeps that memory off the collected heap altogether; a freed or trimmed buffer goes
/// back to the operating system at once instead of waiting for a gen2 collection.
/// </para>
/// </remarks>
internal sealed unsafe partial class NativeBufferPool
{
    /// <summary>The name of the <see cref="AppContext" /> switch that, when enabled, frees every buffer as soon as its derivation ends.</summary>
    internal const string DisableReuseSwitchName = "Bodu.Security.Cryptography.Argon2.DisableMatrixReuse";

    /// <summary>The largest buffer the shared pool retains, in bytes: 256 MiB, four times RFC 9106's second recommended memory cost.</summary>
    internal const long DefaultMaxRetainedBufferBytes = 256L * 1024 * 1024;

    /// <summary>The alignment of every buffer, in bytes: one cache line, so no vector load straddles two.</summary>
    private const nuint Alignment = 64;

    /// <summary>The largest span <see cref="CryptographicOperations.ZeroMemory" /> is given at once, in bytes.</summary>
    private const int ClearChunkBytes = 1 << 30;

    /// <summary>
    /// Gets the time a retained buffer may go unused before the shared pool frees it.
    /// </summary>
    internal static TimeSpan DefaultIdleTimeout { get; } = TimeSpan.FromSeconds(30);

    /// <summary>The buffers currently retained, each all zero; guarded by <see cref="_gate" />.</summary>
    private readonly List<RetainedBuffer> _retained = [];

    /// <summary>The lock that guards the retained buffers and the idle timer.</summary>
    private readonly object _gate = new();

    /// <summary>The clock that stamps returned buffers and drives the idle timer.</summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>The timer that releases idle buffers, or <see langword="null" /> while the pool is empty; guarded by <see cref="_gate" />.</summary>
    private ITimer? _idleTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeBufferPool" /> class.
    /// </summary>
    /// <param name="maxRetainedBuffers">The greatest number of buffers to retain; zero retains none.</param>
    /// <param name="maxRetainedBufferBytes">The largest buffer to retain, in bytes.</param>
    /// <param name="idleTimeout">The time a retained buffer may go unused before it is freed.</param>
    /// <param name="timeProvider">The clock that stamps returned buffers and drives the idle timer.</param>
    internal NativeBufferPool(int maxRetainedBuffers, long maxRetainedBufferBytes, TimeSpan idleTimeout, TimeProvider timeProvider)
    {
        MaxRetainedBuffers = maxRetainedBuffers;
        MaxRetainedBufferBytes = maxRetainedBufferBytes;
        IdleTimeout = idleTimeout;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets the pool every derivation uses: up to one retained buffer per processor, each up to 256 MiB, released after
    /// thirty idle seconds - or none at all when the <see cref="DisableReuseSwitchName" /> switch is set.
    /// </summary>
    internal static NativeBufferPool Shared { get; } = new(
        AppContext.TryGetSwitch(DisableReuseSwitchName, out bool disabled) && disabled ? 0 : Environment.ProcessorCount,
        DefaultMaxRetainedBufferBytes,
        DefaultIdleTimeout,
        TimeProvider.System);

    /// <summary>
    /// Gets the greatest number of buffers the pool retains.
    /// </summary>
    internal int MaxRetainedBuffers { get; }

    /// <summary>
    /// Gets the largest buffer the pool retains, in bytes.
    /// </summary>
    internal long MaxRetainedBufferBytes { get; }

    /// <summary>
    /// Gets the time a retained buffer may go unused before the pool frees it.
    /// </summary>
    internal TimeSpan IdleTimeout { get; }

    /// <summary>
    /// Gets the number of buffers the pool currently retains.
    /// </summary>
    internal int RetainedCount
    {
        get
        {
            lock (_gate)
                return _retained.Count;
        }
    }

    /// <summary>
    /// Gets the total size, in bytes, of the buffers the pool currently retains.
    /// </summary>
    internal long RetainedBytes
    {
        get
        {
            lock (_gate)
            {
                long total = 0;
                foreach (RetainedBuffer buffer in _retained)
                    total += (long)buffer.Capacity;

                return total;
            }
        }
    }

    /// <summary>
    /// Takes a buffer of at least <paramref name="bytes" /> bytes: the smallest retained buffer that is large enough,
    /// or a newly allocated one.
    /// </summary>
    /// <param name="bytes">The number of bytes required.</param>
    /// <param name="capacity">The size of the buffer returned, which may exceed <paramref name="bytes" />.</param>
    /// <returns>
    /// The buffer, 64-byte aligned. A retained buffer is all zero; a newly allocated one is uninitialized.
    /// </returns>
    /// <exception cref="OutOfMemoryException">The buffer cannot be allocated.</exception>
    internal byte* Rent(nuint bytes, out nuint capacity)
    {
        lock (_gate)
        {
            int best = -1;
            for (int i = 0; i < _retained.Count; i++)
            {
                if (_retained[i].Capacity >= bytes && (best < 0 || _retained[i].Capacity < _retained[best].Capacity))
                    best = i;
            }

            if (best >= 0)
            {
                RetainedBuffer buffer = _retained[best];
                _retained.RemoveAt(best);
                capacity = buffer.Capacity;
                return (byte*)buffer.Address;
            }
        }

        capacity = bytes;
        return (byte*)NativeMemory.AlignedAlloc(bytes, Alignment);
    }

    /// <summary>
    /// Takes back a buffer whose every byte is zero, retaining it for reuse or freeing it.
    /// </summary>
    /// <param name="buffer">The buffer, as returned by <see cref="Rent" />.</param>
    /// <param name="capacity">The buffer's size, as reported by <see cref="Rent" />.</param>
    /// <remarks>
    /// The caller clears what it wrote before returning the buffer; the pool relies on that and never reads the
    /// contents.
    /// </remarks>
    internal void Return(byte* buffer, nuint capacity)
    {
        lock (_gate)
        {
            if (_retained.Count < MaxRetainedBuffers && capacity <= (nuint)MaxRetainedBufferBytes)
            {
                _retained.Add(new RetainedBuffer((nint)buffer, capacity, _timeProvider.GetTimestamp()));
                EnsureIdleTimer();
                return;
            }
        }

        NativeMemory.AlignedFree(buffer);
    }

    /// <summary>
    /// Frees every retained buffer that has gone unused for at least <see cref="IdleTimeout" />.
    /// </summary>
    /// <remarks>
    /// The idle timer calls this; tests call it directly with a controlled clock.
    /// </remarks>
    internal void TrimIdle()
    {
        long now = _timeProvider.GetTimestamp();

        lock (_gate)
        {
            for (int i = _retained.Count - 1; i >= 0; i--)
            {
                if (_timeProvider.GetElapsedTime(_retained[i].ReturnedAt, now) < IdleTimeout)
                    continue;

                NativeMemory.AlignedFree((void*)_retained[i].Address);
                _retained.RemoveAt(i);
            }

            if (_retained.Count == 0 && _idleTimer is not null)
            {
                _idleTimer.Dispose();
                _idleTimer = null;
            }
        }
    }

    /// <summary>
    /// Clears a region of native memory in a way the JIT cannot elide.
    /// </summary>
    /// <param name="buffer">The start of the region.</param>
    /// <param name="bytes">The number of bytes to clear.</param>
    internal static void Clear(byte* buffer, nuint bytes)
    {
        while (bytes > 0)
        {
            int chunk = (int)Math.Min(bytes, (nuint)ClearChunkBytes);
            CryptographicOperations.ZeroMemory(new Span<byte>(buffer, chunk));
            buffer += chunk;
            bytes -= (nuint)chunk;
        }
    }

    /// <summary>
    /// Starts the idle timer if it is not already running. The caller holds <see cref="_gate" />.
    /// </summary>
    private void EnsureIdleTimer()
    {
        if (_idleTimer is not null)
            return;

        // The timer outlives the derivation that started it, so it must not capture that caller's execution context.
        if (ExecutionContext.IsFlowSuppressed())
        {
            _idleTimer = CreateIdleTimer();
            return;
        }

        using (ExecutionContext.SuppressFlow())
            _idleTimer = CreateIdleTimer();
    }

    /// <summary>
    /// Creates the timer that calls <see cref="TrimIdle" /> once per <see cref="IdleTimeout" />.
    /// </summary>
    /// <returns>The running timer.</returns>
    private ITimer CreateIdleTimer() =>
        _timeProvider.CreateTimer(static state => ((NativeBufferPool)state!).TrimIdle(), this, IdleTimeout, IdleTimeout);
}
