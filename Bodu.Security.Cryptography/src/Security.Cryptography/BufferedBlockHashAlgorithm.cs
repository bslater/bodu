// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferedBlockHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Bodu;
using Bodu.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the shared infrastructure for hash algorithms that consume input in fixed-size blocks. Owns the residual buffer,
/// the running total of bytes consumed, the disposal latch, and the <see cref="HashAlgorithm.HashCore(byte[], int, int)"/>
/// to <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})"/> delegation.
/// </summary>
/// <typeparam name="T">The concrete hash algorithm derived from this class. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// This class is the common ancestor for both block-buffered patterns offered by the library:
/// <see cref="BlockHashAlgorithm{T}"/> (Merkle&#8211;Damg&#229;rd-style; pads the final partial block before processing) and
/// the Blake-family-style sibling that defers the final full block until <see cref="HashAlgorithm.HashFinal"/> so that
/// a finalisation flag may be raised on the last compression call.
/// </para>
/// <para>
/// The grandparent intentionally does <em>not</em> implement <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})"/> or
/// <see cref="HashAlgorithm.HashFinal"/>, because the buffering loops and finalisation shapes of the two derived patterns
/// genuinely differ. Each derived base owns its own <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})"/> override and
/// reads <see cref="BlockSizeBytes"/>, <see cref="_residualBlock"/>, <see cref="_residualBytes"/> and
/// <see cref="_totalBytes"/> directly.
/// </para>
/// <para>Derived classes must implement the following:</para>
/// <list type="bullet">
/// <item><description><see cref="HashAlgorithm.Initialize"/> resets any algorithm-specific state (chaining variables, IV, schedule). Override and call <c>base.Initialize()</c> first.</description></item>
/// <item><description><see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})"/> consumes the input span using the buffering shape required by the algorithm family.</description></item>
/// <item><description><see cref="HashAlgorithm.HashFinal"/> finalises the computation and returns the digest.</description></item>
/// </list>
/// <para>
/// <strong>Don't derive from this class directly.</strong> Use one of the four pattern-specific bases that
/// extend it — the buffering loops and finalisation shapes differ enough that the right derivation point
/// depends on which family the algorithm belongs to:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Merkle–Damgård, unkeyed</term>
///     <description><see cref="BlockHashAlgorithm{T}"/> — Tiger, Whirlpool, Snefru, the SHA-2 family,
///     classic block-padding hashes that finalise by padding the last partial block.</description>
///   </item>
///   <item>
///     <term>Merkle–Damgård, keyed</term>
///     <description><see cref="KeyedBlockHashAlgorithm{T}"/> — Poly1305, SipHash, and any keyed hash whose
///     finalisation is "pad then compress".</description>
///   </item>
///   <item>
///     <term>Blake-style, unkeyed</term>
///     <description><see cref="DeferredFinalBlockHashAlgorithm{T}"/> — BLAKE3 and other algorithms that need
///     to defer the last full block so a finalisation flag can be set.</description>
///   </item>
///   <item>
///     <term>Blake-style, optionally keyed</term>
///     <description><see cref="KeyedDeferredFinalBlockHashAlgorithm{T}"/> — BLAKE2b, BLAKE2s, and the RFC 7693
///     keyed-MAC variants of BLAKE-family hashes.</description>
///   </item>
/// </list>
/// <para>
/// Derive from <see cref="BufferedBlockHashAlgorithm{T}"/> directly only when implementing a <em>new</em>
/// buffering pattern that doesn't fit either family — e.g. a sponge construction with a non-Merkle–Damgård
/// finalisation step.
/// </para>
/// </remarks>
public abstract class BufferedBlockHashAlgorithm<T>
    : HashAlgorithm
    where T : BufferedBlockHashAlgorithm<T>, new()
{
    /// <summary>
    /// Gets the canonical, fully-qualified algorithm name for this instance, including any size or variant
    /// qualifiers (for example, <c>"Tiger/192"</c>, <c>"Skein-512-256"</c>, <c>"BLAKE2b-512"</c>,
    /// <c>"ASCON-HASH256"</c>, <c>"SipHash-2-4-64"</c>).
    /// </summary>
    /// <returns>A string identifying the algorithm and its current configuration.</returns>
    /// <remarks>
    /// Derived classes implement this property to expose a stable, consumer-facing identifier suitable for
    /// logging, telemetry, registry keys, or interop with hash-name catalogues. Implementations should be
    /// pure and side-effect-free — the value may be queried before any input has been consumed and after
    /// disposal as part of error reporting.
    /// </remarks>
    public abstract string AlgorithmName { get; }

    /// <summary>
    /// The fixed size, in bytes, of each block consumed by the algorithm.
    /// </summary>
    protected readonly int BlockSizeBytes;

    /// <summary>
    /// Backing buffer that accumulates input bytes which do not yet form a complete block. Sized at
    /// <see cref="BlockSizeBytes"/> at construction. Cleared by <see cref="Initialize"/> and overwritten with zeros
    /// during disposal.
    /// </summary>
    protected readonly Memory<byte> _residualBlock;

    /// <summary>
    /// Number of bytes currently held in <see cref="_residualBlock"/>. Always in the range <c>[0, BlockSizeBytes]</c>.
    /// Reset to <c>0</c> by <see cref="Initialize"/>.
    /// </summary>
    protected int _residualBytes;

    /// <summary>
    /// Running total of bytes consumed by the algorithm so far. Excludes any bytes still held in
    /// <see cref="_residualBlock"/> when used by derived classes that update the total before processing each block.
    /// Reset to <c>0</c> by <see cref="Initialize"/>.
    /// </summary>
    protected ulong _totalBytes;

    /// <summary>
    /// Indicates whether <see cref="Dispose(bool)"/> has been called. Used to guard <see cref="ThrowIfDisposed"/> and
    /// to suppress duplicate disposal work.
    /// </summary>
    private bool _disposed;

#if !NET6_0_OR_GREATER

    /// <summary>
    /// Indicates whether the hash computation has been finalised. Used in .NET Standard environments to enforce the
    /// "no input after finalisation" contract that .NET 6+ enforces in the framework itself.
    /// </summary>
    protected bool _finalized;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedBlockHashAlgorithm{T}"/> class with the specified input
    /// block size.
    /// </summary>
    /// <param name="blockSize">
    /// The fixed size, in bits, of each block consumed by the algorithm. Must be greater than zero and a positive
    /// multiple of 8.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="blockSize"/> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Allocates the residual buffer at <paramref name="blockSize"/> / 8 bytes. The internal
    /// <see cref="BlockSizeBytes"/> field exposes the byte-length form for byte-oriented buffering loops. Derived
    /// classes are expected to override <see cref="Initialize"/>, call <c>base.Initialize()</c> first to clear the
    /// inherited buffer and counters, then reset their own algorithm-specific state so the two halves stay in sync.
    /// </remarks>
    protected BufferedBlockHashAlgorithm(int blockSize)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);
        this.BlockSizeBytes = blockSize / 8;
        this._residualBlock = new byte[blockSize / 8];
    }

    /// <summary>
    /// Resets the algorithm to its initial state by clearing the residual buffer and the running byte total.
    /// Derived classes override this method, call <c>base.Initialize()</c> first, and then reset their own
    /// algorithm-specific state (chaining variables, IV, key-derived schedule).
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// <para>
    /// This method does not reset the <see cref="HashAlgorithm.State"/> property explicitly on .NET 6+ targets &#8212;
    /// the framework manages that transition. On earlier targets, derived classes that need the already-finalised
    /// guard should reset their <c>_finalized</c> backing field from their own <c>Initialize</c> override.
    /// </para>
    /// <para>
    /// Derived classes that need to validate state before the reset (for example, a keyed MAC that refuses to be
    /// re-initialised when no key has been set) should perform that validation before calling
    /// <c>base.Initialize()</c>. Once the base call returns, the residual buffer is empty,
    /// <see cref="_residualBytes"/> is <c>0</c>, and <see cref="_totalBytes"/> is <c>0</c>.
    /// </para>
    /// </remarks>
    public override void Initialize()
    {
        this.ThrowIfDisposed();
        this._residualBlock.Span.Clear();
        this._residualBytes = 0;
        this._totalBytes = 0UL;
    }

    /// <summary>
    /// Releases the resources used by the algorithm and zeros the residual buffer.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release
    /// only unmanaged resources.
    /// </param>
    /// <remarks>
    /// <para>
    /// The dispose latch ensures that subsequent calls are no-ops and that the residual buffer is cleared at most once.
    /// </para>
    /// <para>
    /// Derived classes that hold algorithm-specific state, buffers, keys, or other secret material should override
    /// <see cref="Dispose(bool)"/>, clear their own state when <paramref name="disposing"/> is <see langword="true"/>,
    /// and then call the base implementation.
    /// </para>
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this._residualBlock);
            CryptoHelpers.ClearAndNullify(ref this.HashValue);
            this._residualBytes = 0;
            this._totalBytes = 0UL;
            this.HashSizeValue = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Consumes the supplied input span and updates the algorithm state. Derived classes implement the buffering
    /// loop appropriate to their family (immediate-fire-on-full-block for Merkle&#8211;Damg&#229;rd hashes,
    /// defer-on-full-block for Blake-family hashes).
    /// </summary>
    /// <param name="source">The input bytes to consume. May be empty, partial, exact-block, or multi-block in length.</param>
    /// <remarks>
    /// This overload is re-marked <c>abstract</c> by the grandparent to force every derived base to provide an
    /// explicit implementation. Without this, the default <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})"/>
    /// implementation would allocate a temporary byte array and call back into
    /// <see cref="HashCore(byte[], int, int)"/>, producing infinite recursion through the grandparent's forwarding
    /// override.
    /// </remarks>
    protected abstract override void HashCore(ReadOnlySpan<byte> source);

    /// <summary>
    /// Validates the supplied byte-array slice and forwards it to the
    /// <see cref="HashCore(ReadOnlySpan{byte})"/> overload that derived classes implement.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash.</param>
    /// <param name="ibStart">The zero-based index in <paramref name="array"/> at which to begin reading data.</param>
    /// <param name="cbSize">The number of bytes to process from <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="ibStart"/> is less than zero.</para>
    /// <para>-or-</para>
    /// <para><paramref name="cbSize"/> is less than zero.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ibStart"/> and <paramref name="cbSize"/> specify a range that exceeds the length of
    /// <paramref name="array"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// On target frameworks prior to .NET 6, the hash algorithm has already been finalised and cannot accept more
    /// input data.
    /// </exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(array, ibStart, cbSize);

#if !NET6_0_OR_GREATER
        if (this._finalized)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        this.HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed. Read-only and updated exactly once by
    /// <see cref="Dispose(bool)"/> after the residual buffer and counters have been cleared.
    /// </summary>
    /// <returns><see langword="true"/> once disposal has begun; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Derived classes follow the canonical dispose pattern — guard the body of their own
    /// <see cref="Dispose(bool)"/> override with <c>if (this.IsDisposed) return;</c>, clear their own state when
    /// <c>disposing</c> is <see langword="true"/>, and call <c>base.Dispose(disposing)</c> last. Derived classes
    /// must not declare a private <c>_disposed</c> field of their own — the latch is owned exclusively by this
    /// base class.
    /// </remarks>
    protected bool IsDisposed => this._disposed;

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }

    /// <summary>
    /// Throws if the algorithm has begun processing and can no longer be reconfigured.
    /// </summary>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown if an attempt is made to change configuration after the algorithm has started.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }
}
