// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Whirlpool.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 512-bit cryptographic hash using the <c>Whirlpool</c> algorithm designed by Paulo S. L. M. Barreto and
/// Vincent Rijmen. Supports all three published revisions: <c>Whirlpool-0</c> (2000), <c>Whirlpool-T</c> (2001) and the
/// final <c>Whirlpool</c> function standardized by <c>ISO/IEC 10118-3</c> in 2003. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Whirlpool is a Merkle-Damgård construction wrapped around an internal 512-bit block cipher (<c>W</c>) that borrows
/// the wide-trail design principles of the Rijndael family. Input is consumed in 64-byte blocks; the final message
/// length (in bits) is appended in a 256-bit big-endian trailer after a single <c>0x80</c> padding byte, in the
/// standard manner.
/// </para>
/// <para>
/// The selected revision is controlled by <see cref="Version" />. The default is
/// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />, which matches the <c>ISO/IEC 10118-3</c> standard.
/// <see cref="Version" /> may be changed before any input has been consumed; attempting to change it once hashing has
/// started throws <see cref="CryptographicUnexpectedOperationException" />. Calling <see cref="Initialize" /> returns
/// the instance to the reconfigurable state.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 512 bits (64 bytes), fixed.</description>
/// </item>
/// <item>
/// <description>Block size: 64 bytes (512 bits); 256-bit big-endian length field.</description>
/// </item>
/// <item>
/// <description>Internal cipher <c>W</c> on the wide-trail (Rijndael-family) design principle.</description>
/// </item>
/// <item>
/// <description>
/// Selectable revision: <see cref="WhirlpoolVersion.WhirlpoolInfo1" /> (2000),
/// <see cref="WhirlpoolVersion.WhirlpoolInfo1" /> (Whirlpool-T, 2001), or
/// <see cref="WhirlpoolVersion.WhirlpoolInfo3" /> (ISO/IEC 10118-3, 2003 - default).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Whirlpool.</strong> Pick Whirlpool when interoperability with software that produces or
/// expects ISO/IEC 10118-3 Whirlpool digests is required - TrueCrypt-era disk encryption metadata, certain European
/// e-government standards, and some content-addressed stores. For a modern 512-bit cryptographic hash without an
/// interop constraint use SHA-512 or <see cref="Blake2b" />; both are faster on contemporary hardware.
/// </para>
/// <para>
/// This implementation is constant-time in its control flow, but the S-box lookup tables are read at message-dependent
/// indices, so hashing secret data (for example inside a keyed construction) is <b>not</b> hardened against timing or
/// cache-based side-channel attacks.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var whirlpool = new Whirlpool { Version = WhirlpoolVersion.WhirlpoolInfo3 };
/// byte[] digest = whirlpool.ComputeHash(message);
///]]>
/// </code>
/// </example>
public sealed partial class Whirlpool
    : BlockHashAlgorithm
{
    /// <summary>Length of the Whirlpool compression block is 512 bits (64 bytes).</summary>
    private const int BlockSizeBits = 512;

    /// <summary>Length of the Whirlpool digest is 512 bits (64 bytes).</summary>
    private const int HashSizeBits = 512;

    /// <summary>Length of the Whirlpool message-length trailer appended during padding is 256 bits (32 bytes).</summary>
    private const int LengthFieldBits = 256;

    /// <summary>The eight 64-bit chaining variables updated in place across the Merkle-Damgård compression.</summary>
    private readonly ulong[] _state = new ulong[8];

    /// <summary>The selected Whirlpool revision used to compute the hash value.</summary>
    private WhirlpoolVersion _version = WhirlpoolVersion.WhirlpoolInfo3;

    /// <summary>Indicates whether any input has been consumed, latching the <see cref="Version" /> setter once hashing starts.</summary>
    private bool _inputConsumed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Whirlpool" /> class configured for
    /// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />, the standardized <c>ISO/IEC 10118-3</c> revision.
    /// </summary>
    public Whirlpool()
        : base(BlockSizeBits)
    {
        HashSizeValue = HashSizeBits;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// Returns one of <c>"Whirlpool-0"</c>, <c>"Whirlpool-T"</c>, or <c>"Whirlpool"</c> matching the configured
    /// <see cref="Version" /> - corresponding to the 2000, 2001, and ISO/IEC 10118-3 (2003) revisions respectively.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return _version switch
            {
                WhirlpoolVersion.WhirlpoolInfo1 => "Whirlpool-0",
                WhirlpoolVersion.WhirlpoolInfo2 => "Whirlpool-T",
                _ => "Whirlpool",
            };
        }
    }

    /// <summary>
    /// Gets or sets the published <see cref="WhirlpoolVersion" /> used to compute the hash value.
    /// </summary>
    /// <value>The Whirlpool revision selected for subsequent hashing operations.</value>
    /// <remarks>
    /// <para>
    /// The revision must be assigned before any input has been consumed. Once
    /// <see cref="HashAlgorithm.TransformBlock" /> or any <c>ComputeHash</c> overload has started a computation, the
    /// value becomes immutable until <see cref="Initialize" /> is called.
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
            ThrowIfDisposed();
            return _version;
        }

        set
        {
            ThrowIfDisposed();
            if (_inputConsumed)
                throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.Crypt_Invalid_ReconfigurationNotAllowed);
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);

            _version = value;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Clears the eight 64-bit chaining variables and unlatches the <see cref="Version" /> setter.
    /// </remarks>
    public override void Initialize()
    {
        base.Initialize();
        CryptographyHelper.Clear(_state);
        _inputConsumed = false;
    }

    /// <inheritdoc />
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        base.HashCore(array, ibStart, cbSize);
        _inputConsumed = true;
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        base.HashCore(source);
        _inputConsumed = true;
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears the internal state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            CryptographyHelper.Clear(_state);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override int PadBlock(ReadOnlySpan<byte> block, ulong messageLength, Span<byte> destination)
    {
        int inputLength = block.Length;
        const int blockBytes = BlockSizeBits / 8;
        const int lengthFieldBytes = LengthFieldBits / 8;

        // Whirlpool appends 0x80, a sequence of zero bytes, and a 256-bit big-endian length trailer.
        // A second padded block is required whenever the residual (including the 0x80 byte) does not
        // leave room for the 32-byte length field in the same block.
        bool needsSecondBlock = inputLength + 1 + lengthFieldBytes > blockBytes;
        int totalLength = needsSecondBlock ? blockBytes * 2 : blockBytes;

        Span<byte> padded = destination[..totalLength];
        padded.Clear();
        block.CopyTo(padded);
        padded[inputLength] = 0x80;

        // Only the low 64 bits of the bit count are populated; the upper 192 bits remain zero. This
        // matches the behavior of every widely deployed Whirlpool implementation.
        ulong bitLength = messageLength * 8;
        BinaryPrimitives.WriteUInt64BigEndian(padded[(totalLength - 8)..], bitLength);

        return totalLength;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The cipher's key and state each alternate between two eight-word buffers on the stack, one read and the other
    /// written by each round, so no round copies its result back. Whirlpool has ten rounds, an even number, so both end
    /// in the buffers they started in.
    /// </remarks>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        VariantTables tables = GetTables(_version);
        ref ulong mul = ref MemoryMarshal.GetArrayDataReference(tables.Multiplication);
        ReadOnlySpan<ulong> constants = tables.RoundConstants;

        // The message, the key and the cipher state twice each, and the key schedule's round key (c_r, 0, ..., 0).
        Span<ulong> words = stackalloc ulong[48];
        ref ulong message = ref MemoryMarshal.GetReference(words);
        ref ulong key = ref Unsafe.Add(ref message, 8);
        ref ulong nextKey = ref Unsafe.Add(ref message, 16);
        ref ulong state = ref Unsafe.Add(ref message, 24);
        ref ulong nextState = ref Unsafe.Add(ref message, 32);
        ref ulong roundConstant = ref Unsafe.Add(ref message, 40);
        words.Slice(41, 7).Clear();

        // Read the message block (big-endian) and initialize the round key from the hash state.
        // The first sigma step XORs the message against the initial key to seed the cipher state.
        for (int i = 0; i < 8; i++)
        {
            ulong word = BinaryPrimitives.ReadUInt64BigEndian(block.Slice(i * 8, 8));
            Unsafe.Add(ref message, i) = word;
            Unsafe.Add(ref key, i) = _state[i];
            Unsafe.Add(ref state, i) = word ^ _state[i];
        }

        for (int r = 0; r < RoundCount; r += 2)
        {
            // Evolve the round key by one application of the non-linear round function with the variant-specific
            // constant in column 0 and zeros elsewhere, then apply the same round function to the cipher state with
            // the newly evolved round key. The second half of the pass runs the next round back the other way.
            roundConstant = constants[r];
            ApplyRound(ref key, ref roundConstant, ref nextKey, ref mul);
            ApplyRound(ref state, ref nextKey, ref nextState, ref mul);

            roundConstant = constants[r + 1];
            ApplyRound(ref nextKey, ref roundConstant, ref key, ref mul);
            ApplyRound(ref nextState, ref key, ref state, ref mul);
        }

        // Miyaguchi-Preneel finalization: H_{i+1} = W_{H_i}(M) ⊕ M ⊕ H_i.
        for (int i = 0; i < 8; i++)
            _state[i] ^= Unsafe.Add(ref state, i) ^ Unsafe.Add(ref message, i);
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        byte[] output = new byte[HashSizeBits / 8];
        for (int i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt64BigEndian(output.AsSpan(i * 8, 8), _state[i]);

        return output;
    }

    /// <summary>
    /// Applies one round of the Whirlpool <c>W</c> cipher to eight input words, combining the non-linear substitution,
    /// shift-column, MixRows and AddRoundKey operations via the precomputed multiplication table.
    /// </summary>
    /// <param name="input">The first of the eight 64-bit input words.</param>
    /// <param name="roundKey">The first of the eight 64-bit round-key words XORed into the round output.</param>
    /// <param name="output">
    /// The first of the eight words that receive the round output; not overlapping the input.
    /// </param>
    /// <param name="mul">The first entry of the variant's flat 8 × 256 multiplication table.</param>
    /// <remarks>
    /// Each table index is a column number shifted left by 8 and ORed with one byte of an input word, so it is always
    /// below 8 × 256, the table's length: the lookups need no bounds check.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyRound(ref ulong input, ref ulong roundKey, ref ulong output, ref ulong mul)
    {
        ulong s0 = input, s1 = Unsafe.Add(ref input, 1), s2 = Unsafe.Add(ref input, 2), s3 = Unsafe.Add(ref input, 3);
        ulong s4 = Unsafe.Add(ref input, 4), s5 = Unsafe.Add(ref input, 5), s6 = Unsafe.Add(ref input, 6), s7 = Unsafe.Add(ref input, 7);

        output = Unsafe.Add(ref mul, (byte)(s0 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s7 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s6 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s5 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s4 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s3 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s2 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s1)
            ^ roundKey;

        Unsafe.Add(ref output, 1) = Unsafe.Add(ref mul, (byte)(s1 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s0 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s7 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s6 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s5 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s4 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s3 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s2)
            ^ Unsafe.Add(ref roundKey, 1);

        Unsafe.Add(ref output, 2) = Unsafe.Add(ref mul, (byte)(s2 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s1 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s0 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s7 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s6 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s5 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s4 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s3)
            ^ Unsafe.Add(ref roundKey, 2);

        Unsafe.Add(ref output, 3) = Unsafe.Add(ref mul, (byte)(s3 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s2 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s1 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s0 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s7 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s6 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s5 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s4)
            ^ Unsafe.Add(ref roundKey, 3);

        Unsafe.Add(ref output, 4) = Unsafe.Add(ref mul, (byte)(s4 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s3 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s2 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s1 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s0 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s7 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s6 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s5)
            ^ Unsafe.Add(ref roundKey, 4);

        Unsafe.Add(ref output, 5) = Unsafe.Add(ref mul, (byte)(s5 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s4 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s3 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s2 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s1 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s0 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s7 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s6)
            ^ Unsafe.Add(ref roundKey, 5);

        Unsafe.Add(ref output, 6) = Unsafe.Add(ref mul, (byte)(s6 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s5 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s4 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s3 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s2 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s1 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s0 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s7)
            ^ Unsafe.Add(ref roundKey, 6);

        Unsafe.Add(ref output, 7) = Unsafe.Add(ref mul, (byte)(s7 >> 56))
            ^ Unsafe.Add(ref mul, 0x100 | (byte)(s6 >> 48))
            ^ Unsafe.Add(ref mul, 0x200 | (byte)(s5 >> 40))
            ^ Unsafe.Add(ref mul, 0x300 | (byte)(s4 >> 32))
            ^ Unsafe.Add(ref mul, 0x400 | (byte)(s3 >> 24))
            ^ Unsafe.Add(ref mul, 0x500 | (byte)(s2 >> 16))
            ^ Unsafe.Add(ref mul, 0x600 | (byte)(s1 >> 8))
            ^ Unsafe.Add(ref mul, 0x700 | (byte)s0)
            ^ Unsafe.Add(ref roundKey, 7);
    }
}
