// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Whirlpool.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 512-bit cryptographic hash using the <c>Whirlpool</c> algorithm designed by Paulo S. L. M.
/// Barreto and Vincent Rijmen. Supports all three published revisions: <c>Whirlpool-0</c> (2000),
/// <c>Whirlpool-T</c> (2001) and the final <c>Whirlpool</c> function standardised by <c>ISO/IEC 10118-3</c>
/// in 2003. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Whirlpool is a Merkle–Damgård construction wrapped around an internal 512-bit block cipher (<c>W</c>)
/// that borrows the wide-trail design principles of the Rijndael family. Input is consumed in 64-byte
/// blocks; the final message length (in bits) is appended in a 256-bit big-endian trailer after a single
/// <c>0x80</c> padding byte, in the standard manner.
/// </para>
/// <para>
/// The selected revision is controlled by <see cref="Version" />. The default is
/// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />, which matches the <c>ISO/IEC 10118-3</c> standard.
/// <see cref="Version" /> may be changed before any input has been consumed; attempting to change it once
/// hashing has started throws <see cref="CryptographicUnexpectedOperationException" />. Calling
/// <see cref="Initialize" /> returns the instance to the reconfigurable state.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: 512 bits (64 bytes), fixed.</description></item>
///   <item><description>Block size: 64 bytes (512 bits); 256-bit big-endian length field.</description></item>
///   <item><description>Internal cipher <c>W</c> on the wide-trail (Rijndael-family) design principle.</description></item>
///   <item><description>Selectable revision: <see cref="WhirlpoolVersion.WhirlpoolInfo1"/> (Whirlpool-0, 2000), <see cref="WhirlpoolVersion.WhirlpoolInfo2"/> (Whirlpool-T, 2001), or <see cref="WhirlpoolVersion.WhirlpoolInfo3"/> (ISO/IEC 10118-3, 2003 — default).</description></item>
/// </list>
/// <para>
/// <strong>When to choose Whirlpool.</strong> Pick Whirlpool when interoperability with software that produces
/// or expects ISO/IEC 10118-3 Whirlpool digests is required — TrueCrypt-era disk encryption metadata, certain
/// European e-government standards, and some content-addressed stores. For a modern 512-bit cryptographic hash
/// without an interop constraint use SHA-512 or <see cref="Blake2b"/>; both are faster on contemporary hardware.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var whirlpool = new Whirlpool { Version = WhirlpoolVersion.WhirlpoolInfo3 };
/// byte[] digest = whirlpool.ComputeHash(message);
/// </code>
/// </example>
public sealed partial class Whirlpool
    : BlockHashAlgorithm<Whirlpool>
{
    private const int BlockSizeBytesValue = 64;
    private const int HashSizeBytesValue = 64;
    private const int HashSizeBitsValue = HashSizeBytesValue * 8;
    private const int LengthFieldBytes = 32;

    private readonly ulong[] _state = new ulong[8];
    private WhirlpoolVersion _version = WhirlpoolVersion.WhirlpoolInfo3;
    private bool _inputConsumed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Whirlpool" /> class configured for
    /// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />, the standardised <c>ISO/IEC 10118-3</c> revision.
    /// </summary>
    public Whirlpool()
        : base(BlockSizeBytesValue)
    {
        this.HashSizeValue = HashSizeBitsValue;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets or sets the published <see cref="WhirlpoolVersion" /> used to compute the hash value.
    /// </summary>
    /// <value>The Whirlpool revision selected for subsequent hashing operations.</value>
    /// <returns>The currently configured Whirlpool revision.</returns>
    /// <remarks>
    /// <para>
    /// The revision must be assigned before any input has been consumed. Once
    /// <see cref="HashAlgorithm.TransformBlock" /> or any <c>ComputeHash</c> overload has started a
    /// computation, the value becomes immutable until <see cref="Initialize" /> is called.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned value is not a defined <see cref="WhirlpoolVersion" /> member.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The hash algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started and the algorithm is no longer reconfigurable.
    /// </exception>
    public WhirlpoolVersion Version
    {
        get
        {
            this.ThrowIfDisposed();
            return this._version;
        }

        set
        {
            this.ThrowIfDisposed();
            if (this._inputConsumed)
                throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_ReconfigurationNotAllowed);
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);

            this._version = value;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
        base.Initialize();
        Array.Clear(this._state);
        this._inputConsumed = false;
    }

    /// <inheritdoc />
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        base.HashCore(array, ibStart, cbSize);
        this._inputConsumed = true;
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        base.HashCore(source);
        this._inputConsumed = true;
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears the internal state.
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
            CryptoHelpers.ClearAndNullify(ref this.HashValue);
            this.HashSizeValue = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        int inputLength = block.Length;

        // Whirlpool appends 0x80, a sequence of zero bytes, and a 256-bit big-endian length trailer.
        // A second padded block is required whenever the residual (including the 0x80 byte) does not
        // leave room for the 32-byte length field in the same block.
        bool needsSecondBlock = inputLength + 1 + LengthFieldBytes > BlockSizeBytesValue;
        int totalLength = needsSecondBlock ? BlockSizeBytesValue * 2 : BlockSizeBytesValue;

        byte[] padded = new byte[totalLength];
        block.CopyTo(padded);
        padded[inputLength] = 0x80;

        // Only the low 64 bits of the bit count are populated; the upper 192 bits remain zero. This
        // matches the behaviour of every widely deployed Whirlpool implementation.
        ulong bitLength = messageLength * 8;
        BinaryPrimitives.WriteUInt64BigEndian(padded.AsSpan(totalLength - 8), bitLength);

        return padded;
    }

    /// <inheritdoc />
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        VariantTables tables = GetTables(this._version);
        ulong[] mul = tables.Multiplication;
        ulong[][] rcon = tables.RoundKeys;

        Span<ulong> message = stackalloc ulong[8];
        Span<ulong> key = stackalloc ulong[8];
        Span<ulong> state = stackalloc ulong[8];
        Span<ulong> tempKey = stackalloc ulong[8];
        Span<ulong> tempState = stackalloc ulong[8];

        // Read the message block (big-endian) and initialise the round key from the hash state.
        // The first sigma step XORs the message against the initial key to seed the cipher state.
        for (int i = 0; i < 8; i++)
        {
            message[i] = BinaryPrimitives.ReadUInt64BigEndian(block.Slice(i * 8, 8));
            key[i] = this._state[i];
            state[i] = message[i] ^ key[i];
        }

        for (int r = 0; r < RoundCount; r++)
        {
            // Evolve the round key by one application of the non-linear round function with the
            // variant-specific constant in column 0 and zeros elsewhere.
            ApplyRound(key, rcon[r], tempKey, mul);
            tempKey.CopyTo(key);

            // Apply the same round function to the cipher state using the newly evolved round key.
            ApplyRound(state, key, tempState, mul);
            tempState.CopyTo(state);
        }

        // Miyaguchi–Preneel finalisation: H_{i+1} = W_{H_i}(M) ⊕ M ⊕ H_i.
        for (int i = 0; i < 8; i++)
            this._state[i] ^= state[i] ^ message[i];
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        byte[] output = new byte[HashSizeBytesValue];
        for (int i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt64BigEndian(output.AsSpan(i * 8, 8), this._state[i]);

        return output;
    }

    /// <summary>
    /// Applies one round of the Whirlpool <c>W</c> cipher to <paramref name="state" />, combining the
    /// non-linear substitution, shift-column, MixRows and AddRoundKey operations via the precomputed
    /// multiplication table <paramref name="mul" />.
    /// </summary>
    /// <param name="state">The eight 64-bit input words.</param>
    /// <param name="roundKey">The eight 64-bit round-key words XORed into the round output.</param>
    /// <param name="output">The span that receives the round output; must have length 8.</param>
    /// <param name="mul">The flat 8 × 256 multiplication table for the active variant.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyRound(ReadOnlySpan<ulong> state, ReadOnlySpan<ulong> roundKey, Span<ulong> output, ulong[] mul)
    {
        const ulong M = 0xFF;
        ulong s0 = state[0], s1 = state[1], s2 = state[2], s3 = state[3];
        ulong s4 = state[4], s5 = state[5], s6 = state[6], s7 = state[7];

        output[0] = mul[(s0 >> 56) & M]
                  ^ mul[0x100 | ((s7 >> 48) & M)]
                  ^ mul[0x200 | ((s6 >> 40) & M)]
                  ^ mul[0x300 | ((s5 >> 32) & M)]
                  ^ mul[0x400 | ((s4 >> 24) & M)]
                  ^ mul[0x500 | ((s3 >> 16) & M)]
                  ^ mul[0x600 | ((s2 >> 8) & M)]
                  ^ mul[0x700 | (s1 & M)]
                  ^ roundKey[0];

        output[1] = mul[(s1 >> 56) & M]
                  ^ mul[0x100 | ((s0 >> 48) & M)]
                  ^ mul[0x200 | ((s7 >> 40) & M)]
                  ^ mul[0x300 | ((s6 >> 32) & M)]
                  ^ mul[0x400 | ((s5 >> 24) & M)]
                  ^ mul[0x500 | ((s4 >> 16) & M)]
                  ^ mul[0x600 | ((s3 >> 8) & M)]
                  ^ mul[0x700 | (s2 & M)]
                  ^ roundKey[1];

        output[2] = mul[(s2 >> 56) & M]
                  ^ mul[0x100 | ((s1 >> 48) & M)]
                  ^ mul[0x200 | ((s0 >> 40) & M)]
                  ^ mul[0x300 | ((s7 >> 32) & M)]
                  ^ mul[0x400 | ((s6 >> 24) & M)]
                  ^ mul[0x500 | ((s5 >> 16) & M)]
                  ^ mul[0x600 | ((s4 >> 8) & M)]
                  ^ mul[0x700 | (s3 & M)]
                  ^ roundKey[2];

        output[3] = mul[(s3 >> 56) & M]
                  ^ mul[0x100 | ((s2 >> 48) & M)]
                  ^ mul[0x200 | ((s1 >> 40) & M)]
                  ^ mul[0x300 | ((s0 >> 32) & M)]
                  ^ mul[0x400 | ((s7 >> 24) & M)]
                  ^ mul[0x500 | ((s6 >> 16) & M)]
                  ^ mul[0x600 | ((s5 >> 8) & M)]
                  ^ mul[0x700 | (s4 & M)]
                  ^ roundKey[3];

        output[4] = mul[(s4 >> 56) & M]
                  ^ mul[0x100 | ((s3 >> 48) & M)]
                  ^ mul[0x200 | ((s2 >> 40) & M)]
                  ^ mul[0x300 | ((s1 >> 32) & M)]
                  ^ mul[0x400 | ((s0 >> 24) & M)]
                  ^ mul[0x500 | ((s7 >> 16) & M)]
                  ^ mul[0x600 | ((s6 >> 8) & M)]
                  ^ mul[0x700 | (s5 & M)]
                  ^ roundKey[4];

        output[5] = mul[(s5 >> 56) & M]
                  ^ mul[0x100 | ((s4 >> 48) & M)]
                  ^ mul[0x200 | ((s3 >> 40) & M)]
                  ^ mul[0x300 | ((s2 >> 32) & M)]
                  ^ mul[0x400 | ((s1 >> 24) & M)]
                  ^ mul[0x500 | ((s0 >> 16) & M)]
                  ^ mul[0x600 | ((s7 >> 8) & M)]
                  ^ mul[0x700 | (s6 & M)]
                  ^ roundKey[5];

        output[6] = mul[(s6 >> 56) & M]
                  ^ mul[0x100 | ((s5 >> 48) & M)]
                  ^ mul[0x200 | ((s4 >> 40) & M)]
                  ^ mul[0x300 | ((s3 >> 32) & M)]
                  ^ mul[0x400 | ((s2 >> 24) & M)]
                  ^ mul[0x500 | ((s1 >> 16) & M)]
                  ^ mul[0x600 | ((s0 >> 8) & M)]
                  ^ mul[0x700 | (s7 & M)]
                  ^ roundKey[6];

        output[7] = mul[(s7 >> 56) & M]
                  ^ mul[0x100 | ((s6 >> 48) & M)]
                  ^ mul[0x200 | ((s5 >> 40) & M)]
                  ^ mul[0x300 | ((s4 >> 32) & M)]
                  ^ mul[0x400 | ((s3 >> 24) & M)]
                  ^ mul[0x500 | ((s2 >> 16) & M)]
                  ^ mul[0x600 | ((s1 >> 8) & M)]
                  ^ mul[0x700 | (s0 & M)]
                  ^ roundKey[7];
    }
}
