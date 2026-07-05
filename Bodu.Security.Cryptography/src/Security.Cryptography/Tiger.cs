// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Tiger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>Tiger</c> cryptographic hash algorithm by Ross Anderson and Eli Biham (1996), optimized
/// for 64-bit platforms. Supports output sizes of 128, 160, or 192 bits and both the original
/// <see cref="TigerHashingVariant.Tiger" /> and <see cref="TigerHashingVariant.Tiger2" /> padding variants. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Tiger" /> processes input in 512-bit (64-byte) blocks using three 64-bit internal state variables. Each
/// block is mixed into the state by three passes, and each pass performs eight S-box driven mixing rounds separated by
/// a key schedule applied to the block words.
/// </para>
/// <para>
/// The full 192-bit digest is always computed internally; shorter outputs (<c>Tiger/128</c> and <c>Tiger/160</c>) are
/// produced by truncation after finalization. The padding byte is selected via <see cref="Variant" />:
/// <see cref="TigerHashingVariant.Tiger" /> uses <c>0x01</c> (the original specification) and
/// <see cref="TigerHashingVariant.Tiger2" /> uses <c>0x80</c>.
/// </para>
/// <para>
/// Although no longer recommended for new security-sensitive applications, Tiger has not been broken in the classical
/// collision sense and is still useful for legacy interoperability and as a fast integrity hash.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 128, 160, or 192 bits — internally always 192 bits, then truncated.</description>
/// </item>
/// <item>
/// <description>Block size: 64 bytes (512 bits); three 64-bit state variables.</description>
/// </item>
/// <item>
/// <description>Three passes per block, eight S-box rounds per pass; optimized for 64-bit hosts.</description>
/// </item>
/// <item>
/// <description>
/// Padding variant: <see cref="TigerHashingVariant.Tiger" /> (<c>0x01</c>) or <see cref="TigerHashingVariant.Tiger2" />
/// (<c>0x80</c>).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Tiger.</strong> Pick Tiger only for legacy interoperability — TigerTree (Merkle hash of
/// Tiger-192 leaves) is still seen in older P2P and content-addressed storage systems. For any new security design use
/// a SHA-2 family member or <see cref="Blake2b" />; for fast non-cryptographic fingerprinting the algorithms in
/// <c>Bodu.IO.Hashing</c> are usually a better fit.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var tiger = new Tiger(192) { Variant = TigerHashingVariant.Tiger2 };
/// byte[] digest = tiger.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso href="https://www.cs.technion.ac.il/~biham/Reports/Tiger/">Tiger home page (Anderson / Biham)</seealso>
/// <seealso href="../guides/cryptography/tiger.html">Using Tiger</seealso>
/// <seealso href="../guides/cryptography/hashing.html#pattern-3--a-cryptographic-digest">Cryptographic digest guide
/// </seealso>
public sealed partial class Tiger
    : BlockHashAlgorithm<Tiger>
{
    /// <summary>The number of bits in the full Tiger digest that is always computed internally before truncation.</summary>
    private const int MaxOutputBits = 192;

    /// <summary>The set of hash output sizes, in bits, that the Tiger algorithm permits.</summary>
    private static readonly int[] s_permittedHashSizes = [128, 160, 192];

    /// <summary>The first 64-bit chaining variable of the Tiger state.</summary>
    private ulong _state0 = 0x0123456789ABCDEF;

    /// <summary>The second 64-bit chaining variable of the Tiger state.</summary>
    private ulong _state1 = 0xFEDCBA9876543210;

    /// <summary>The third 64-bit chaining variable of the Tiger state.</summary>
    private ulong _state2 = 0xF096A5B4C3B2E187;

    /// <summary>The padding variant that selects the final-block padding byte (<c>0x01</c> for Tiger, <c>0x80</c> for Tiger2).</summary>
    private TigerHashingVariant _variant = TigerHashingVariant.Tiger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tiger" /> class with a 192-bit output hash size.
    /// </summary>
    public Tiger()
        : this(192)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tiger" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">The desired output size in bits. Must be one of 128, 160, or 192.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="hashSize" /> is not valid.</exception>
    public Tiger(int hashSize)
        : base(512)
    {
        CryptographyThrowHelper.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);

        HashSizeValue = hashSize;
    }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the variant and hash output size.
    /// </summary>
    /// <value>
    /// A string in the form <c>Tiger/x</c>, where <c>x</c> is the number of bits in the final hash output.
    /// </value>
    /// <remarks>
    /// <para>
    /// The name follows the convention <c>Tiger/x</c>, where <c>x</c> is the number of output bits-typically 128, 160,
    /// or 192. These correspond to the standard Tiger variants: <c>Tiger/128</c>, <c>Tiger/160</c>, and
    /// <c>Tiger/192</c>.
    /// </para>
    /// <para>
    /// The full 192-bit internal state is always computed. If a shorter output length is selected, the result is
    /// truncated after finalization to match the configured <see cref="HashSize" />.
    /// </para>
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return $"Tiger/{HashSizeValue}";
        }
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <remarks>
    /// Valid values are <c>128</c>, <c>160</c>, or <c>192</c>. This determines how many bits of the internal state are
    /// returned in the final digest. Larger sizes increase output strength but may reduce compatibility with some Tiger
    /// implementations.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not 128, 160, or 192.</exception>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown if the hash computation has already started.
    /// </exception>
    public new int HashSize
    {
        get
        {
            ThrowIfDisposed();
            return HashSizeValue;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            CryptographyThrowHelper.ThrowIfInvalidHashSize(value, s_permittedHashSizes);

            HashSizeValue = value;
        }
    }

    /// <summary>
    /// Gets or sets the Tiger variant to use when computing the hash value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="TigerHashingVariant" /> determines the padding byte used in the final message block:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="TigerHashingVariant.Tiger" /> uses a padding byte of <c>0x01</c> as per the original Tiger
    /// specification.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="TigerHashingVariant.Tiger2" /> uses a padding byte of <c>0x80</c> as introduced in the Tiger2 variant
    /// to match typical Merkle�Damg�rd padding semantics.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// The variant must be specified before hash computation begins. Changing it after processing has started will
    /// throw an exception.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the assigned value is not a valid <see cref="TigerHashingVariant" /> enumeration value.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the hash algorithm instance has already been disposed.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown if the hash algorithm has already started processing input and is in an immutable state.
    /// </exception>
    public TigerHashingVariant Variant
    {
        get
        {
            ThrowIfDisposed();
            return _variant;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);

            _variant = value;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Restores the three 64-bit chaining variables to their Tiger-specified initial constants.
    /// </remarks>
    public override void Initialize()
    {
        base.Initialize();
        _state0 = 0x0123456789ABCDEF;
        _state1 = 0xFEDCBA9876543210;
        _state2 = 0xF096A5B4C3B2E187;
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears the internal state variables.
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
            _state0 = _state1 = _state2 = 0;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        int inputLength = block.Length;
        int blockBytes = BlockSize / 8;
        bool needsSecondBlock = inputLength >= blockBytes - 8;
        int totalLength = needsSecondBlock ? blockBytes * 2 : blockBytes;

        // Use stackalloc if small enough; fallback to heap if larger
        Span<byte> padded = totalLength <= 128
            ? stackalloc byte[totalLength]
            : new byte[totalLength];

        // Copy input to padded buffer
        block.CopyTo(padded);

        // Write padding byte depending on Tiger variant
        padded[inputLength] = _variant == TigerHashingVariant.Tiger ? (byte)0x01 : (byte)0x80;

        // Clear bytes between padding and message length field
        padded.Slice(inputLength + 1, totalLength - inputLength - 1 - 8).Clear();

        // Append message length in bits in little-endian at end
        ulong bitLength = messageLength * 8;
        BinaryPrimitives.WriteUInt64LittleEndian(padded[(totalLength - 8)..], bitLength);

        return padded[..totalLength].ToArray();
    }

    /// <inheritdoc />
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        // Tiger processes the message as little-endian 64-bit words. Read them explicitly rather than reinterpreting
        // the raw bytes, so the digest is correct on big-endian hosts too (a host-endian cast would byte-swap there).
        Span<ulong> blockWords = stackalloc ulong[8];
        for (int i = 0; i < 8; i++)
            blockWords[i] = BinaryPrimitives.ReadUInt64LittleEndian(block.Slice(i * 8));

        TransformBlock(blockWords);
    }

    /// <summary>
    /// Finalizes the hash computation and returns the digest, truncated to <see cref="HashSize" /> / 8 bytes where
    /// applicable.
    /// </summary>
    /// <returns>
    /// A byte array of 16, 20, or 24 bytes corresponding to the configured hash size (128, 160, or 192 bits).
    /// </returns>
    protected override byte[] ProcessFinalBlock()
    {
        Span<byte> output = stackalloc byte[MaxOutputBits / 8];
        BinaryPrimitives.WriteUInt64LittleEndian(output[0..8], _state0);
        BinaryPrimitives.WriteUInt64LittleEndian(output[8..16], _state1);
        BinaryPrimitives.WriteUInt64LittleEndian(output[16..24], _state2);

        return output[..(HashSizeValue / 8)].ToArray();
    }

    /// <summary>
    /// Applies one full "pass" of eight Tiger mixing rounds with the specified multiplier.
    /// </summary>
    /// <param name="a">The first state word; updated in place.</param>
    /// <param name="b">The second state word; updated in place.</param>
    /// <param name="c">The third state word; updated in place.</param>
    /// <param name="x">The eight message words for this pass.</param>
    /// <param name="mul">The pass multiplier (5, 7, or 9).</param>
    private static void DoPass(ref ulong a, ref ulong b, ref ulong c, ReadOnlySpan<ulong> x, int mul)
    {
        Round(ref a, ref b, ref c, x[0], mul);
        Round(ref b, ref c, ref a, x[1], mul);
        Round(ref c, ref a, ref b, x[2], mul);
        Round(ref a, ref b, ref c, x[3], mul);
        Round(ref b, ref c, ref a, x[4], mul);
        Round(ref c, ref a, ref b, x[5], mul);
        Round(ref a, ref b, ref c, x[6], mul);
        Round(ref b, ref c, ref a, x[7], mul);
    }

    /// <summary>
    /// Applies the Tiger key schedule to mutate the block before subsequent passes.
    /// </summary>
    /// <param name="x">The eight message words; updated in place per the Tiger key-schedule transform.</param>
    private static void KeySchedule(Span<ulong> x)
    {
        x[0] -= x[7] ^ 0xA5A5A5A5A5A5A5A5UL;
        x[1] ^= x[0];
        x[2] += x[1];
        x[3] -= x[2] ^ (~x[1] << 19);
        x[4] ^= x[3];
        x[5] += x[4];
        x[6] -= x[5] ^ (~x[4] >> 23);
        x[7] ^= x[6];
        x[0] += x[7];
        x[1] -= x[0] ^ (~x[7] << 19);
        x[2] ^= x[1];
        x[3] += x[2];
        x[4] -= x[3] ^ (~x[2] >> 23);
        x[5] ^= x[4];
        x[6] += x[5];
        x[7] -= x[6] ^ 0x0123456789ABCDEFUL;
    }

    /// <summary>
    /// Performs a single Tiger mixing round on three state values and one message word.
    /// </summary>
    /// <param name="a">The first state word; updated in place.</param>
    /// <param name="b">The second state word; updated in place.</param>
    /// <param name="c">The third state word; updated in place.</param>
    /// <param name="x">The message word for this round.</param>
    /// <param name="mul">The pass multiplier (5, 7, or 9).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Round(ref ulong a, ref ulong b, ref ulong c, ulong x, int mul)
    {
        ulong tmp = c ^= x;

        a -=
            s_sBox0[(byte)(tmp >> 0)] ^
            s_sBox1[(byte)(tmp >> 16)] ^
            s_sBox2[(byte)(tmp >> 32)] ^
            s_sBox3[(byte)(tmp >> 48)];

        tmp = b +=
            s_sBox3[(byte)(tmp >> 8)] ^
            s_sBox2[(byte)(tmp >> 24)] ^
            s_sBox1[(byte)(tmp >> 40)] ^
            s_sBox0[(byte)(tmp >> 56)];

        b = tmp * (ulong)mul;
    }

    /// <summary>
    /// Transforms the state using the Tiger compression function.
    /// </summary>
    /// <param name="blockWords">The 512-bit input block represented as 8 x 64-bit words.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TransformBlock(Span<ulong> blockWords)
    {
        ulong a = _state0, b = _state1, c = _state2;

        DoPass(ref a, ref b, ref c, blockWords, 5);
        KeySchedule(blockWords);
        DoPass(ref c, ref a, ref b, blockWords, 7);
        KeySchedule(blockWords);
        DoPass(ref b, ref c, ref a, blockWords, 9);

        _state0 = a ^ _state0;
        _state1 = b - _state1;
        _state2 = c + _state2;
    }
}
