// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Shake.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>SHAKE</c> family of extendable output functions (XOFs) as defined in NIST FIPS 202.
/// Supports security levels of 128 and 256 bits with a configurable output size. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Shake"/> is built on the same <c>Keccak-f[1600]</c> permutation as <c>SHA-3</c>, operating over
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
/// When used via <see cref="HashAlgorithm"/>, <c>HashSizeValue</c> holds the desired output length in bits and
/// <c>securityLevel</c> selects the SHAKE variant. <see cref="HashAlgorithm.ComputeHash(byte[])"/> therefore produces
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
public sealed class Shake : BufferedBlockHashAlgorithm<Shake>
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
#pragma warning disable SA1137 // Elements should have the same indentation
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
#pragma warning restore SA1137 // Elements should have the same indentation

    private readonly ulong[] _state = new ulong[StateWords];
    private readonly int _securityLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="Shake"/> class with a 256-bit output using SHAKE128 internals.
    /// </summary>
    public Shake()
        : this(256, 128)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Shake"/> class with the specified output size and security level.
    /// </summary>
    /// <param name="outputBits">
    /// The desired output size in bits. Must be a positive value divisible by 8.
    /// </param>
    /// <param name="securityLevel">
    /// The SHAKE security level in bits. Must be either 128 (SHAKE128, rate = 168 bytes) or 256 (SHAKE256,
    /// rate = 136 bytes).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputBits"/> is not a positive multiple of 8, or <paramref name="securityLevel"/> is not
    /// 128 or 256.
    /// </exception>
    public Shake(int outputBits, int securityLevel)
        : base(ValidateAndComputeRateBytes(outputBits, securityLevel))
    {
        this.HashSizeValue = outputBits;
        this._securityLevel = securityLevel;
    }

    /// <summary>
    /// Validates the constructor arguments and returns the absorption rate, in bytes, used to size the inherited
    /// residual buffer.
    /// </summary>
    /// <param name="outputBits">The candidate output size in bits.</param>
    /// <param name="securityLevel">The candidate SHAKE security level.</param>
    /// <returns>The absorption rate in bytes (168 for SHAKE128, 136 for SHAKE256).</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="outputBits"/> is not a positive multiple of 8, or <paramref name="securityLevel"/> is not
    /// 128 or 256.
    /// </exception>
    private static int ValidateAndComputeRateBytes(int outputBits, int securityLevel)
    {
        if (outputBits <= 0 || outputBits % 8 != 0)
            throw new ArgumentOutOfRangeException(nameof(outputBits),
                string.Format(CryptoResourceStrings.CryptographicException_InvalidHashSize, outputBits, "any positive multiple of 8"));

        if (Array.IndexOf(s_validSecurityLevels, securityLevel) == -1)
            throw new ArgumentOutOfRangeException(nameof(securityLevel),
                string.Format(CryptoResourceStrings.CryptographicException_InvalidHashSize, securityLevel, string.Join(", ", s_validSecurityLevels)));

        return (1600 - 2 * securityLevel) / 8;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// Returns either <c>"SHAKE128"</c> or <c>"SHAKE256"</c> matching the security level selected at construction.
    /// The output length is independent of the algorithm name and is reflected by <see cref="HashSize"/>.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return $"SHAKE{this._securityLevel}";
        }
    }

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
    /// input has already been absorbed throws <see cref="CryptographicUnexpectedOperationException"/>.
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
    /// <remarks>Clears the 1600-bit Keccak state. The inherited residual rate buffer and counters are cleared by the base call.</remarks>
    public override void Initialize()
    {
        base.Initialize();
        Array.Clear(this._state);
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the internal sponge state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to
    /// release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this.IsDisposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this._state);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Accumulates input bytes in the inherited residual rate buffer. Whenever a complete rate block has been
    /// gathered the buffer is XORed into the sponge state, the Keccak-f permutation is applied, and the buffer is
    /// cleared ready for the next block.
    /// </remarks>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
        Span<byte> rateBuffer = this._residualBlock.Span;
        var rateBytes = this.BlockSizeBytes;

        while (source.Length > 0)
        {
            var available = rateBytes - this._residualBytes;
            var take = Math.Min(available, source.Length);

            source.Slice(0, take).CopyTo(rateBuffer.Slice(this._residualBytes));
            this._residualBytes += take;
            source = source.Slice(take);

            if (this._residualBytes == rateBytes)
            {
                XorBlockIntoState(rateBuffer, this._state, rateBytes);
                KeccakF(this._state);
                rateBuffer.Clear();
                this._residualBytes = 0;
            }
        }
    }

    /// <summary>
    /// Finalises the hash computation by applying SHAKE multi-rate padding, absorbing the final block, and
    /// squeezing the requested number of output bytes from the sponge state.
    /// </summary>
    /// <returns>
    /// A byte array of length <see cref="HashAlgorithm.HashSize"/> / 8 containing the squeezed output.
    /// </returns>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();
        Span<byte> rateBuffer = this._residualBlock.Span;
        var rateBytes = this.BlockSizeBytes;

        // Apply multi-rate padding: domain suffix byte at the current buffer position, then 0x80 at the last byte.
        rateBuffer[this._residualBytes] ^= DomainSuffix;
        rateBuffer[rateBytes - 1] ^= 0x80;

        // Absorb the final padded block into the state.
        XorBlockIntoState(rateBuffer, this._state, rateBytes);
        KeccakF(this._state);

        // Squeeze output bytes from the state (little-endian lane serialisation).
        var outputBytes = this.HashSizeValue / 8;
        var output = new byte[outputBytes];
        var written = 0;
        var remaining = outputBytes;

        while (remaining > 0)
        {
            var take = Math.Min(remaining, rateBytes);
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
        var c = new ulong[5];
        var b = new ulong[StateWords];

        for (var round = 0; round < 24; round++)
        {
            // θ (theta): column parity and mixing.
            for (var x = 0; x < 5; x++)
                c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];

            for (var x = 0; x < 5; x++)
            {
                var d = c[(x + 4) % 5] ^ c[(x + 1) % 5].RotateBitsLeftUnchecked(1);
                for (var y = 0; y < 5; y++)
                    state[x + y * 5] ^= d;
            }

            // ρ and π combined: rotate each lane and scatter to the π-permuted position.
            // s_rho[0] is 0; RotateBitsLeftUnchecked delegates to BitOperations.RotateLeft, which
            // handles a zero shift correctly without the undefined `value >> 64` shift the hand-rolled
            // form would produce.
            for (var i = 0; i < StateWords; i++)
                b[s_pi[i]] = state[i].RotateBitsLeftUnchecked(s_rho[i]);

            // χ (chi): non-linear mixing within each row.
            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                    state[x + y * 5] = b[x + y * 5] ^ ((~b[(x + 1) % 5 + y * 5]) & b[(x + 2) % 5 + y * 5]);
            }

            // ι (iota): XOR a round constant into lane (0,0).
            state[0] ^= s_roundConstants[round];
        }
    }

    /// <summary>
    /// XORs a byte block into the Keccak state using little-endian 64-bit lane interpretation.
    /// </summary>
    /// <param name="block">The byte block to XOR into the state. Must be at least <paramref name="rateBytes"/> bytes long.</param>
    /// <param name="state">The 25-element state array to update.</param>
    /// <param name="rateBytes">The number of bytes from <paramref name="block"/> to absorb.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void XorBlockIntoState(ReadOnlySpan<byte> block, ulong[] state, int rateBytes)
    {
        var lanes = rateBytes / 8;
        for (var i = 0; i < lanes; i++)
            state[i] ^= BinaryPrimitives.ReadUInt64LittleEndian(block.Slice(i * 8, 8));

        // Handle any trailing bytes that do not fill a complete 8-byte lane.
        var remainder = rateBytes % 8;
        if (remainder > 0)
        {
            ulong partial = 0;
            var baseOffset = lanes * 8;
            for (var b = 0; b < remainder; b++)
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
        var lanes = destination.Length / 8;
        for (var i = 0; i < lanes; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(i * 8, 8), state[i]);

        // Serialise any sub-lane tail bytes.
        var remainder = destination.Length % 8;
        if (remainder > 0)
        {
            var lane = state[lanes];
            var baseOffset = lanes * 8;
            for (var b = 0; b < remainder; b++)
                destination[baseOffset + b] = (byte)(lane >> (8 * b));
        }
    }

}
