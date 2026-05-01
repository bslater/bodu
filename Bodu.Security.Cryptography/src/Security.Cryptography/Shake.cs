// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Shake.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>SHAKE</c> family of extendable output functions (XOFs) as defined in NIST FIPS 202.
/// Supports security levels of 128 and 256 bits with a configurable output size. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Shake" /> is built on the same <c>Keccak-f[1600]</c> permutation as <c>SHA-3</c>, operating over
/// a 1600-bit (200-byte) state. The two SHAKE variants differ only in their rate and, therefore, their security margin:
/// </para>
/// <list type="bullet">
/// <item><description>SHAKE128: rate = 168 bytes, capacity = 32 bytes, security level = 128 bits.</description></item>
/// <item><description>SHAKE256: rate = 136 bytes, capacity = 64 bytes, security level = 256 bits.</description></item>
/// </list>
/// <para>
/// Unlike the fixed-length SHA-3 variants, SHAKE is an XOF — the output length is independent of the security parameter
/// and may be any positive multiple of 8 bits. The domain separation byte <c>0x1F</c> distinguishes SHAKE from SHA-3
/// (<c>0x06</c>) and from raw Keccak. Multi-rate padding (pad10*1) appends the domain byte, zero or more zero bytes, and
/// a <c>0x80</c> byte at the last position of the final rate block.
/// </para>
/// <para>
/// When used via <see cref="HashAlgorithm" />, <c>HashSizeValue</c> holds the desired output length in bits and
/// <c>securityLevel</c> selects the SHAKE variant. <see cref="HashAlgorithm.ComputeHash(byte[])" /> therefore produces
/// exactly <c>outputBits / 8</c> bytes regardless of which security level is chosen.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>State: 1600 bits (200 bytes); Keccak-f[1600] permutation.</description></item>
///   <item><description>Output size: configurable, any positive multiple of 8 bits.</description></item>
///   <item><description>Security level: 128 (SHAKE128) or 256 (SHAKE256).</description></item>
///   <item><description>Domain separation: <c>0x1F</c>; multi-rate padding (pad10*1).</description></item>
///   <item><description>Specification: NIST FIPS 202.</description></item>
/// </list>
/// <para>
/// <strong>When to choose SHAKE.</strong> Pick SHAKE when an extendable-output function is genuinely required
/// — KMAC inputs, post-quantum signature schemes, hash-based DRBGs, and any protocol that needs more than the
/// fixed-length output of SHA-3 / SHA-256. For ordinary fixed-length hashing prefer SHA-3 (FIPS 202) or
/// <see cref="Blake3"/> (faster on commodity hardware). Use SHAKE128 when 128-bit security is sufficient and
/// throughput matters; use SHAKE256 when the higher capacity is required.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// // SHAKE128 producing 256-bit output.
/// using var shake = new Shake(256, 128);
/// byte[] digest = shake.ComputeHash(Encoding.UTF8.GetBytes("hello"));
///
/// // SHAKE256 producing 512-bit output.
/// using var shake256 = new Shake(512, 256);
/// byte[] longer = shake256.ComputeHash(message);
/// </code>
/// </example>
public sealed class Shake : HashAlgorithm
{
    private const int StateWords = 25;
    private const byte DomainSuffix = 0x1F;

    private static readonly int[] s_validSecurityLevels = { 128, 256 };

    // Round constants for the ι (iota) step — 24 values, one per round.
    private static readonly ulong[] s_roundConstants =
    {
        0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808AUL, 0x8000000080008000UL,
        0x000000000000808BUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
        0x000000000000008AUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000AUL,
        0x000000008000808BUL, 0x800000000000008BUL, 0x8000000000008089UL, 0x8000000000008003UL,
        0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800AUL, 0x800000008000000AUL,
        0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL,
    };

    // ρ (rho) rotation offsets indexed as rho[x + 5*y].
    private static readonly int[] s_rho =
    {
         0,  1, 62, 28, 27,
        36, 44,  6, 55, 20,
         3, 10, 43, 25, 39,
        41, 45, 15, 21,  8,
        18,  2, 61, 56, 14,
    };

    // π (pi) permutation indices mapping state[i] → B[pi[i]].
    private static readonly int[] s_pi =
    {
         0, 10, 20,  5, 15,
        16,  1, 11, 21,  6,
         7, 17,  2, 12, 22,
        23,  8, 18,  3, 13,
        14, 24,  9, 19,  4,
    };

    private readonly ulong[] _state = new ulong[StateWords];
    private readonly byte[] _buffer;
    private readonly int _rateBytes;
    private readonly int _securityLevel;
    private int _buffered;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="Shake" /> class with a 256-bit output using SHAKE128 internals.
    /// </summary>
    public Shake()
        : this(256, 128)
    { }

    /// <summary>
    /// Initialises a new instance of the <see cref="Shake" /> class with the specified output size and security level.
    /// </summary>
    /// <param name="outputBits">
    /// The desired output size in bits. Must be a positive value divisible by 8.
    /// </param>
    /// <param name="securityLevel">
    /// The SHAKE security level in bits. Must be either 128 (SHAKE128, rate = 168 bytes) or 256 (SHAKE256,
    /// rate = 136 bytes).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputBits" /> is not a positive multiple of 8, or <paramref name="securityLevel" /> is not
    /// 128 or 256.
    /// </exception>
    public Shake(int outputBits, int securityLevel)
    {
        if (outputBits <= 0 || outputBits % 8 != 0)
            throw new ArgumentOutOfRangeException(nameof(outputBits),
                string.Format(CryptoResourceStrings.CryptographicException_InvalidHashSize, outputBits, "any positive multiple of 8"));

        if (Array.IndexOf(s_validSecurityLevels, securityLevel) == -1)
            throw new ArgumentOutOfRangeException(nameof(securityLevel),
                string.Format(CryptoResourceStrings.CryptographicException_InvalidHashSize, securityLevel, string.Join(", ", s_validSecurityLevels)));

        this.HashSizeValue = outputBits;
        this._securityLevel = securityLevel;
        this._rateBytes = (1600 - 2 * securityLevel) / 8;
        this._buffer = new byte[this._rateBytes];
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets the security level, in bits, of the SHAKE variant in use.
    /// </summary>
    /// <value>Either 128 (SHAKE128) or 256 (SHAKE256).</value>
    /// <returns>The security level selected at construction time.</returns>
    public int SecurityLevel
    {
        get
        {
            this.ThrowIfDisposed();
            return this._securityLevel;
        }
    }

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <value>The current output size in bits. Must be a positive multiple of 8.</value>
    /// <returns>The currently configured output size in bits.</returns>
    /// <remarks>
    /// Because SHAKE is an XOF, the output length may be changed freely between computations. The security level
    /// (and therefore the rate) is fixed at construction time and cannot be altered. Changing this property after
    /// input has already been absorbed throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned value is not a positive multiple of 8.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started and the algorithm is no longer reconfigurable.
    /// </exception>
    public new int HashSize
    {
        get
        {
            this.ThrowIfDisposed();
            return this.HashSizeValue;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();

            if (value <= 0 || value % 8 != 0)
                throw new ArgumentOutOfRangeException(nameof(value),
                    string.Format(CryptoResourceStrings.CryptographicException_InvalidHashSize, value, "any positive multiple of 8"));

            this.HashSizeValue = value;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
        Array.Clear(this._state);
        Array.Clear(this._buffer);
        this._buffered = 0;
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the internal sponge state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to
    /// release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this._state);
            CryptoHelpers.Clear(this._buffer);
            CryptoHelpers.ClearAndNullify(ref this.HashValue);
            this._buffered = 0;
            this.HashSizeValue = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Absorbs a segment of input data into the sponge state, processing complete rate-sized blocks as they
    /// become available.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash.</param>
    /// <param name="ibStart">The zero-based index in <paramref name="array" /> at which to begin reading.</param>
    /// <param name="cbSize">The number of bytes to process from <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        this.ThrowIfDisposed();
        this.Absorb(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Absorbs a span of input data into the sponge state, processing complete rate-sized blocks as they
    /// become available.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
        this.Absorb(source);
    }

    /// <summary>
    /// Finalises the hash computation by applying SHAKE multi-rate padding, absorbing the final block, and
    /// squeezing the requested number of output bytes from the sponge state.
    /// </summary>
    /// <returns>
    /// A byte array of length <see cref="HashAlgorithm.HashSize" /> / 8 containing the squeezed output.
    /// </returns>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();

        // Apply multi-rate padding: domain suffix byte at the current buffer position, then 0x80 at the last byte.
        this._buffer[this._buffered] ^= DomainSuffix;
        this._buffer[this._rateBytes - 1] ^= 0x80;

        // Absorb the final padded block into the state.
        XorBlockIntoState(this._buffer, this._state, this._rateBytes);
        KeccakF(this._state);

        // Squeeze output bytes from the state (little-endian lane serialisation).
        int outputBytes = this.HashSizeValue / 8;
        byte[] output = new byte[outputBytes];
        int written = 0;
        int remaining = outputBytes;

        while (remaining > 0)
        {
            int take = Math.Min(remaining, this._rateBytes);
            WriteLanesToBytes(this._state, output.AsSpan(written, take));
            written += take;
            remaining -= take;

            if (remaining > 0)
                KeccakF(this._state);
        }

        return output;
    }

    /// <summary>
    /// Applies the full <c>Keccak-f[1600]</c> permutation — 24 rounds of θ, ρ, π, χ, and ι — to the
    /// supplied 25-word state array in place.
    /// </summary>
    /// <param name="state">The 25-element state array to permute. Modified in place.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void KeccakF(ulong[] state)
    {
        ulong[] c = new ulong[5];
        ulong[] b = new ulong[StateWords];

        for (int round = 0; round < 24; round++)
        {
            // θ (theta): column parity and mixing.
            for (int x = 0; x < 5; x++)
                c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];

            for (int x = 0; x < 5; x++)
            {
                ulong d = c[(x + 4) % 5] ^ RotateLeft(c[(x + 1) % 5], 1);
                for (int y = 0; y < 5; y++)
                    state[x + y * 5] ^= d;
            }

            // ρ and π combined: rotate each lane and scatter to the π-permuted position.
            for (int i = 0; i < StateWords; i++)
                b[s_pi[i]] = RotateLeft(state[i], s_rho[i]);

            // χ (chi): non-linear mixing within each row.
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                    state[x + y * 5] = b[x + y * 5] ^ ((~b[(x + 1) % 5 + y * 5]) & b[(x + 2) % 5 + y * 5]);
            }

            // ι (iota): XOR a round constant into lane (0,0).
            state[0] ^= s_roundConstants[round];
        }
    }

    /// <summary>
    /// XORs a byte block into the Keccak state using little-endian 64-bit lane interpretation.
    /// </summary>
    /// <param name="block">The byte block to XOR into the state. Must be at most <paramref name="rateBytes" /> bytes long.</param>
    /// <param name="state">The 25-element state array to update.</param>
    /// <param name="rateBytes">The number of bytes in <paramref name="block" /> to process.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void XorBlockIntoState(byte[] block, ulong[] state, int rateBytes)
    {
        int lanes = rateBytes / 8;
        for (int i = 0; i < lanes; i++)
            state[i] ^= BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(i * 8, 8));

        // Handle any trailing bytes that do not fill a complete 8-byte lane.
        int remainder = rateBytes % 8;
        if (remainder > 0)
        {
            ulong partial = 0;
            int baseOffset = lanes * 8;
            for (int b = 0; b < remainder; b++)
                partial |= (ulong)block[baseOffset + b] << (8 * b);

            state[lanes] ^= partial;
        }
    }

    /// <summary>
    /// Serialises the leading lanes of the Keccak state into the destination span using little-endian byte order.
    /// </summary>
    /// <param name="state">The source 25-element state array.</param>
    /// <param name="destination">The span to write output bytes into. Its length determines how many bytes are extracted.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteLanesToBytes(ulong[] state, Span<byte> destination)
    {
        int lanes = destination.Length / 8;
        for (int i = 0; i < lanes; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(i * 8, 8), state[i]);

        // Serialise any sub-lane tail bytes.
        int remainder = destination.Length % 8;
        if (remainder > 0)
        {
            ulong lane = state[lanes];
            int baseOffset = lanes * 8;
            for (int b = 0; b < remainder; b++)
                destination[baseOffset + b] = (byte)(lane >> (8 * b));
        }
    }

    /// <summary>
    /// Rotates a 64-bit value left by the specified number of bits.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="shift">The number of bit positions to rotate left. Must be in the range [0, 63].</param>
    /// <returns>The value rotated left by <paramref name="shift" /> positions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RotateLeft(ulong value, int shift) =>
        shift == 0 ? value : (value << shift) | (value >> (64 - shift));

    /// <summary>
    /// Absorbs the supplied span into the sponge, filling the internal rate buffer and invoking
    /// <c>Keccak-f</c> whenever a full rate block has accumulated.
    /// </summary>
    /// <param name="source">The bytes to absorb.</param>
    private void Absorb(ReadOnlySpan<byte> source)
    {
        while (source.Length > 0)
        {
            int available = this._rateBytes - this._buffered;
            int take = Math.Min(available, source.Length);

            source.Slice(0, take).CopyTo(this._buffer.AsSpan(this._buffered));
            this._buffered += take;
            source = source.Slice(take);

            if (this._buffered == this._rateBytes)
            {
                XorBlockIntoState(this._buffer, this._state, this._rateBytes);
                KeccakF(this._state);
                Array.Clear(this._buffer);
                this._buffered = 0;
            }
        }
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> if this instance has already been disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(this._disposed, this);

    /// <summary>
    /// Throws <see cref="CryptographicUnexpectedOperationException" /> if the algorithm has already started
    /// processing input and can no longer be reconfigured.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }
}
