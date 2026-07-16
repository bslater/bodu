// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Abstract base class for ASCON extendable output functions (XOFs) as defined in NIST SP 800-232. Implements the
/// shared sponge construction, residual-buffer management, padding, and Ascon-p permutation used by
/// <see cref="AsconXof128" /> and <see cref="AsconCxof128" />.
/// </summary>
/// <typeparam name="T">The concrete XOF type derived from this class.</typeparam>
/// <remarks>
/// <para>
/// All ASCON XOF algorithms share a 320-bit internal state of five 64-bit words, a 64-bit (8-byte) rate, and a
/// variable-length output. They differ in their pre-computed initialization state, in the number of absorption rounds
/// (pb), and in whether a customization string may be supplied before absorption.
/// </para>
/// <para>
/// The lifecycle of an instance is:
/// </para>
/// <list type="number">
/// <item>
/// <description>Optionally customize (only <see cref="AsconCxof128" />).</description>
/// </item>
/// <item>
/// <description>Call <see cref="Absorb" /> zero or more times to supply input data.</description>
/// </item>
/// <item>
/// <description>
/// Call <see cref="Squeeze" /> one or more times to produce output of any length. Once squeezing has begun, no further
/// data may be absorbed.
/// </description>
/// </item>
/// <item>
/// <description>Call <see cref="Initialize" /> to reset the instance and reuse it for a new message.</description>
/// </item>
/// </list>
/// <para>
/// Padding follows the Ascon convention: the byte immediately after the last absorbed byte is XORed with <c>0x01</c>,
/// and the remaining rate bytes retain their current state values. The transition from absorption to squeezing always
/// applies the full 12-round permutation (Ascon-p12). Subsequent squeeze blocks use pb rounds between extractions.
/// </para>
/// <para>
/// <strong>Don't derive from this class directly.</strong> Use one of the two concrete XOFs that extend it:
/// </para>
/// <list type="bullet">
/// <item>
/// <term><see cref="AsconXof128" /></term>
/// <description>Plain Ascon XOF — variable-length output without a customization string.</description>
/// </item>
/// <item>
/// <term><see cref="AsconCxof128" /></term>
/// <description>
/// Customizable Ascon XOF — accepts a customization string before absorption to domain-separate output families.
/// </description>
/// </item>
/// </list>
/// <para>
/// For fixed-length Ascon hashing use <see cref="AsconHash256" /> or <see cref="AsconHashA256" />; for the AEAD member
/// of the Ascon suite use <see cref="AsconAead128" />.
/// </para>
/// <para>
/// The concrete type <typeparamref name="T" /> must expose a public parameterless constructor to satisfy the base
/// class's <c>new()</c> constraint.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Consume through a concrete derivative — produce a 32-byte digest from "hello".
/// using var xof = new AsconXof128();
/// xof.Absorb("hello"u8);
///
/// byte[] digest = new byte[32];
/// xof.Squeeze(digest);
///
/// // Squeeze additional output of any length — the XOF can produce as many bytes as needed.
/// byte[] more = new byte[64];
/// xof.Squeeze(more);
///
/// // Reuse the instance for a new message via Initialize.
/// xof.Initialize();
/// xof.Absorb("world"u8);
/// xof.Squeeze(digest);
///]]>
/// </code>
/// </example>
/// <seealso cref="AsconXof128"/> <seealso cref="AsconCxof128"/> <seealso cref="AsconHash"/>
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
public abstract class AsconXof<T>
    : IDisposable
    where T : AsconXof<T>, new()
{
    /// <summary>The sponge rate in bytes (64-bit rate).</summary>
    private const int BlockSize = 8; // 64-bit rate

    /// <summary>The canonical algorithm identifier reported by <see cref="AlgorithmName" />.</summary>
    private readonly string _algorithmName;

    /// <summary>The number of Ascon-p rounds applied after each absorbed block and between squeeze blocks.</summary>
    private readonly int _absorptionRounds;

    /// <summary>The pre-computed initial state word 0 used to seed the sponge on reset.</summary>
    private readonly ulong _iv0;

    /// <summary>The pre-computed initial state word 1 used to seed the sponge on reset.</summary>
    private readonly ulong _iv1;

    /// <summary>The pre-computed initial state word 2 used to seed the sponge on reset.</summary>
    private readonly ulong _iv2;

    /// <summary>The pre-computed initial state word 3 used to seed the sponge on reset.</summary>
    private readonly ulong _iv3;

    /// <summary>The pre-computed initial state word 4 used to seed the sponge on reset.</summary>
    private readonly ulong _iv4;

    /// <summary>The buffer that accumulates partial input bytes that do not yet fill a full rate block.</summary>
    private readonly byte[] _residualBuffer = new byte[BlockSize];

    /// <summary>The buffer that holds the most recently squeezed rate block awaiting output.</summary>
    private readonly byte[] _squeezeBuffer = new byte[BlockSize];

    /// <summary>The 320-bit Ascon sponge state of five 64-bit words.</summary>
    private AsconState _state;

    /// <summary>The number of bytes currently held in <see cref="_residualBuffer" />.</summary>
    private int _residualBytes;

    /// <summary>The read offset into <see cref="_squeezeBuffer" /> for the next squeezed byte.</summary>
    private int _squeezeBufOffset;

    /// <summary>The number of unread bytes remaining in <see cref="_squeezeBuffer" />.</summary>
    private int _squeezeBufAvailable;

    /// <summary>Indicates whether the sponge has transitioned from absorbing to squeezing.</summary>
    private bool _squeezing;

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconXof{T}" /> class with the specified algorithm parameters.
    /// </summary>
    /// <param name="iv0">Pre-computed initial state word 0 (result of applying Ascon-p12 to the raw IV).</param>
    /// <param name="iv1">Pre-computed initial state word 1.</param>
    /// <param name="iv2">Pre-computed initial state word 2.</param>
    /// <param name="iv3">Pre-computed initial state word 3.</param>
    /// <param name="iv4">Pre-computed initial state word 4.</param>
    /// <param name="absorptionRounds">
    /// Number of Ascon-p rounds applied after each absorbed block and between squeeze blocks. Must be between 1 and 12.
    /// </param>
    /// <param name="algorithmName">
    /// The canonical algorithm identifier string as defined in NIST SP 800-232. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="absorptionRounds" /> is less than 1 or greater than 12.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The IV state words are assigned together to mirror the five-word Ascon initial state and keep the algorithm parameter initialization grouped as a single logical operation.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "The five IV words are intentionally kept together on the first line because they form the complete five-word Ascon initial state; the remaining parameters configure behavior rather than state.")]
    protected AsconXof(
        ulong iv0, ulong iv1, ulong iv2, ulong iv3, ulong iv4,
        int absorptionRounds,
        string algorithmName)
    {
        ThrowHelper.ThrowIfNull(algorithmName);
        ThrowHelper.ThrowIfLessThan(absorptionRounds, 1);
        ThrowHelper.ThrowIfGreaterThan(absorptionRounds, 12);

        _iv0 = iv0; _iv1 = iv1; _iv2 = iv2; _iv3 = iv3; _iv4 = iv4;
        _absorptionRounds = absorptionRounds;
        _algorithmName = algorithmName;
        Initialize();
    }

    /// <summary>
    /// Gets the canonical algorithm name for this XOF variant as defined in NIST SP 800-232.
    /// </summary>
    /// <value>A string such as <c>"ASCON-XOF128"</c> or <c>"ASCON-CXOF128"</c>.</value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return _algorithmName;
        }
    }

    /// <summary>
    /// Resets the instance to its initial state, discarding any absorbed data or squeezed output, so that it is ready
    /// to accept a new message.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public virtual void Initialize()
    {
        ThrowIfDisposed();
        _state = new AsconState { S0 = _iv0, S1 = _iv1, S2 = _iv2, S3 = _iv3, S4 = _iv4 };
        _residualBytes = 0;
        _squeezing = false;
        _squeezeBufOffset = 0;
        _squeezeBufAvailable = 0;
        CryptographyHelper.Clear(_residualBuffer.AsSpan(0, BlockSize));
        CryptographyHelper.Clear(_squeezeBuffer.AsSpan(0, BlockSize));
    }

    /// <summary>
    /// Absorbs <paramref name="data" /> into the sponge state. May be called multiple times before the first
    /// <see cref="Squeeze" />.
    /// </summary>
    /// <param name="data">The input data to absorb. May be empty.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Squeeze" /> has already been called; call <see cref="Initialize" /> to reset and start over.
    /// </exception>
    public virtual void Absorb(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        if (_squeezing)
            throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_XofSqueezeAfterAbsorb);

        ProcessInputBlocks(data);
    }

    /// <summary>
    /// Squeezes <paramref name="output" /><c>.Length</c> bytes from the sponge into <paramref name="output" />. May be
    /// called multiple times to produce an unbounded output stream.
    /// </summary>
    /// <param name="output">Destination for the squeezed bytes. May be empty.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public void Squeeze(Span<byte> output)
    {
        ThrowIfDisposed();

        if (!_squeezing)
            BeginSqueezing();

        int outOff = 0;
        while (outOff < output.Length)
        {
            if (_squeezeBufAvailable == 0)
            {
                _state.Permute(_absorptionRounds);
                _state.SqueezeRate64(_squeezeBuffer);
                _squeezeBufOffset = 0;
                _squeezeBufAvailable = BlockSize;
            }

            int toCopy = Math.Min(_squeezeBufAvailable, output.Length - outOff);
            _squeezeBuffer.AsSpan(_squeezeBufOffset, toCopy).CopyTo(output[outOff..]);
            _squeezeBufOffset += toCopy;
            _squeezeBufAvailable -= toCopy;
            outOff += toCopy;
        }
    }

    /// <summary>
    /// Squeezes exactly <paramref name="outputLength" /> bytes and returns them as a new array.
    /// </summary>
    /// <param name="outputLength">The number of bytes to produce. Must be greater than zero.</param>
    /// <returns>A new byte array of length <paramref name="outputLength" /> containing the squeezed output.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputLength" /> is less than or equal to zero.
    /// </exception>
    public byte[] GetHash(int outputLength)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfLessThanOrEqual(outputLength, 0);

        byte[] result = new byte[outputLength];
        Squeeze(result);
        return result;
    }

    /// <summary>
    /// Creates a new instance of <typeparamref name="T" /> using its public parameterless constructor.
    /// </summary>
    /// <returns>A new, uninitialized <typeparamref name="T" /> instance.</returns>
    public static T Create() => new();

    /// <summary>
    /// Hashes <paramref name="source" /> in a single pass and returns <paramref name="outputLength" /> bytes.
    /// </summary>
    /// <param name="source">The input data to hash.</param>
    /// <param name="outputLength">The number of output bytes to produce. Must be greater than zero.</param>
    /// <returns>A byte array of length <paramref name="outputLength" /> containing the XOF output.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputLength" /> is less than or equal to zero.
    /// </exception>
    public static byte[] HashData(ReadOnlySpan<byte> source, int outputLength)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(outputLength, 0);

        var xof = new T();
        xof.Absorb(source);
        return xof.GetHash(outputLength);
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the internal sponge state.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases managed resources and clears the sponge state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> for unmanaged
    /// only.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _state.Clear();
            CryptographyHelper.Clear(ref _state);
            CryptographyHelper.Clear(_residualBuffer);
            CryptographyHelper.Clear(_squeezeBuffer);
        }

        _disposed = true;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Finalizes a sponge absorption phase by padding the residual buffer with <c>0x01</c> at the next unused byte
    /// position, absorbing the padded block, and applying <see cref="_absorptionRounds" /> Ascon-p rounds. Resets the
    /// residual counter to zero so that the next call to <see cref="Absorb" /> starts from a clean state.
    /// </summary>
    /// <remarks>
    /// Derived classes (specifically <see cref="AsconCxof128" />) call this to close the customization phase before
    /// injecting a domain-separation constant.
    /// </remarks>
    protected void FinalizeAbsorptionPhase()
    {
        Span<byte> pad = stackalloc byte[BlockSize];
        _residualBuffer.AsSpan(0, _residualBytes).CopyTo(pad);
        pad[_residualBytes] ^= 0x01;
        _state.AbsorbRate64(pad);
        _state.Permute(_absorptionRounds);
        _residualBytes = 0;
        CryptographyHelper.Clear(_residualBuffer.AsSpan(0, BlockSize));
    }

    /// <summary>
    /// Accumulates <paramref name="data" /> into the residual buffer, flushing complete 8-byte blocks into the state.
    /// </summary>
    /// <param name="data">The bytes to absorb.</param>
    private void ProcessInputBlocks(ReadOnlySpan<byte> data)
    {
        int pos = 0;

        if (_residualBytes > 0)
        {
            int needed = BlockSize - _residualBytes;
            if (data.Length >= needed)
            {
                data[..needed].CopyTo(_residualBuffer.AsSpan(_residualBytes));
                _state.AbsorbRate64(_residualBuffer);
                _state.Permute(_absorptionRounds);
                _residualBytes = 0;
                pos += needed;
            }
            else
            {
                data.CopyTo(_residualBuffer.AsSpan(_residualBytes));
                _residualBytes += data.Length;
                return;
            }
        }

        while (pos + BlockSize <= data.Length)
        {
            _state.AbsorbRate64(data.Slice(pos, BlockSize));
            _state.Permute(_absorptionRounds);
            pos += BlockSize;
        }

        int remaining = data.Length - pos;
        if (remaining > 0)
        {
            data.Slice(pos, remaining).CopyTo(_residualBuffer.AsSpan(0, remaining));
            _residualBytes = remaining;
        }
    }

    /// <summary>
    /// Transitions from the absorption phase to the squeeze phase: pads the residual, applies Ascon-p12, and populates
    /// the first squeeze block.
    /// </summary>
    private void BeginSqueezing()
    {
        // Apply Ascon padding: 0x01 at the next unused byte in the residual buffer.
        Span<byte> padded = stackalloc byte[BlockSize];
        _residualBuffer.AsSpan(0, _residualBytes).CopyTo(padded);
        padded[_residualBytes] ^= 0x01;

        _state.AbsorbRate64(padded);

        // Transition permutation: always p12 regardless of absorption round count.
        _state.Permute(12);

        // Pre-fill the squeeze buffer with the first block (no permutation before it).
        _state.SqueezeRate64(_squeezeBuffer);
        _squeezeBufOffset = 0;
        _squeezeBufAvailable = BlockSize;
        _squeezing = true;
    }
}
