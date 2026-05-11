// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for the <c>SipHash</c> family of keyed pseudorandom functions, a fast keyed hash designed by Aumasson and Bernstein
/// for short input messages. See the official <a href="https://131002.net/siphash/">SipHash specification</a> for details.
/// </summary>
/// <typeparam name="T">The concrete SipHash variant derived from this class. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// <see cref="SipHash{T}"/> is a keyed hash function that requires a 128-bit (16-byte) secret key. It mixes each input block into
/// four 64-bit state variables (<c>v0</c> through <c>v3</c>) using Add-Rotate-XOR (ARX) steps, and is designed to resist
/// hash-flooding attacks against hash tables.
/// </para>
/// <para>This base class is extended by:</para>
/// <list type="bullet">
/// <item>
/// <description><see cref="SipHash64"/> produces a 64-bit hash output suitable for compact keyed checksums.</description>
/// </item>
/// <item>
/// <description><see cref="SipHash128"/> produces a 128-bit hash output offering increased collision resistance.</description>
/// </item>
/// </list>
/// <para>
/// Each 64-bit input block is absorbed during a compression phase consisting of <see cref="CompressionRounds"/> rounds. Once all
/// input has been processed, <see cref="FinalizationRounds"/> rounds are applied to produce the final digest. The defaults
/// (<c>c = 2</c>, <c>d = 4</c>) correspond to the standard <c>SipHash-2-4</c> parameterisation.
/// </para>
/// <para>
/// <strong>When to choose SipHash.</strong> SipHash is the de-facto standard for protecting hash tables and
/// bloom filters against collision-based denial-of-service attacks — Python, Ruby, Rust, Perl, and OpenBSD's
/// stdlib all use it for that purpose. Pick <see cref="SipHash64"/> when 64 bits is enough; pick
/// <see cref="SipHash128"/> when collision pressure on the tag width matters. For protocol-level message
/// authentication over long messages prefer HMAC-SHA-256 or <see cref="Blake2b"/>-MAC. Do not use SipHash
/// where the key may be exposed — its security target is hash-flooding resistance, not generic MAC strength.
/// </para>
/// </remarks>
/// <example>
/// The following example configures a <see cref="SipHash64"/> instance to use the stronger <c>SipHash-4-8</c> parameter set.
/// <code language="csharp">
/// using var sipHash = new SipHash64
/// {
///     Key = myKey,
///     CompressionRounds = 4,
///     FinalizationRounds = 8,
/// };
/// byte[] tag = sipHash.ComputeHash(message);
/// </code>
/// </example>
public abstract class SipHash<T>
    : KeyedBlockHashAlgorithm<T>
    where T : SipHash<T>, new()
{
    /// <summary>
    /// The fixed key size in bytes (128 bits).
    /// </summary>
    public const int KeySize = 16;

    /// <summary>
    /// The minimum number of compression rounds required by SipHash.
    /// </summary>
    public const int MinCompressionRounds = 2;

    /// <summary>
    /// The minimum number of finalization rounds required by SipHash.
    /// </summary>
    public const int MinFinalizationRounds = 4;

    private static readonly int s_blockSize = 8;

    private static readonly ulong[] s_initialStates =
    [
        0x736f6d6570736575UL,
        0x646f72616e646f6dUL,
        0x6c7967656e657261UL,
        0x7465646279746573UL,
    ];

    private static readonly int[] s_permittedHashSizes = [64, 128];
    private int _compressionRounds;
    private int _finalizationRounds;
    private ulong _v0, _v1, _v2, _v3;

    /// <summary>
    /// Initializes a new instance of the <see cref="SipHash{T}"/> class with a specified hash size.
    /// </summary>
    /// <param name="hashSize">The desired size of the final hash in bits. Supported values are 64 or 128.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="hashSize"/> is not supported.</exception>
    protected SipHash(int hashSize)
        : base(s_blockSize, KeySize)
    {
        CryptoHelpers.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);

        this.KeyValue = new byte[KeySize];
        CryptoHelpers.FillWithRandomNonZeroBytes(this.KeyValue);
        this._compressionRounds = MinCompressionRounds;
        this._finalizationRounds = MinFinalizationRounds;
        this.HashSizeValue = hashSize;
        this.OnKeyChanged();
    }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the variant and hash output size.
    /// </summary>
    /// <remarks>
    /// Follows the convention "SipHash-c-d-x", where:
    /// <list type="bullet">
    /// <item>
    /// <description><c>c</c>: compression rounds</description>
    /// </item>
    /// <item>
    /// <description><c>d</c>: finalization rounds</description>
    /// </item>
    /// <item>
    /// <description><c>x</c>: output hash size in bits</description>
    /// </item>
    /// </list>
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return $"SipHash-{this.CompressionRounds}-{this.FinalizationRounds}-{this.HashSizeValue}";
        }
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets or sets the number of compression rounds applied to each input block during the SipHash computation.
    /// </summary>
    /// <value>A positive integer greater than or equal to <see cref="MinCompressionRounds"/>. The default is 2.</value>
    /// <remarks>
    /// Compression rounds are performed for every 8-byte message block before finalization. Increasing this value improves diffusion
    /// and resistance to hash-flooding attacks, but also increases computation time.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is less than <see cref="MinCompressionRounds"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown if the hash computation has already begun and the property is modified mid-operation.
    /// </exception>
    public int CompressionRounds
    {
        get
        {
            this.ThrowIfDisposed();
            return this._compressionRounds;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfLessThan(value, MinCompressionRounds);

            this._compressionRounds = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of finalization rounds executed after all message blocks have been absorbed.
    /// </summary>
    /// <value>A positive integer greater than or equal to <see cref="MinFinalizationRounds"/>. The default is 4.</value>
    /// <remarks>
    /// Finalization rounds strengthen the avalanche effect after all input is processed. Increasing this value improves security at the
    /// cost of additional computation during final hash derivation.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is less than <see cref="MinFinalizationRounds"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown if the hash computation has already begun and the property is modified mid-operation.
    /// </exception>
    public int FinalizationRounds
    {
        get
        {
            this.ThrowIfDisposed();
            return this._finalizationRounds;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfLessThan(value, MinFinalizationRounds);

            this._finalizationRounds = value;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
    /// </param>
    /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (this.IsDisposed) return;

        if (disposing)
        {
            this._v0 = this._v1 = this._v2 = this._v3 = 0;
            this._compressionRounds = 0;
            this._finalizationRounds = 0;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Produces the SipHash final padding block: copies the residual 0–7 bytes into an 8-byte
    /// buffer and places the low byte of <paramref name="messageLength"/> into the last slot.
    /// </summary>
    /// <param name="block">The residual input bytes; length must be in <c>[0..7]</c>.</param>
    /// <param name="messageLength">The total processed message length in bytes. Only the low
    /// byte is used per the SipHash specification.</param>
    /// <returns>The padded 8-byte block.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="block"/> is longer than
    /// 7 bytes.</exception>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        if ((uint)block.Length > 7)
            throw new ArgumentOutOfRangeException(nameof(block), "Residual block must be 0-7 bytes.");

        Span<byte> buffer = stackalloc byte[8];
        block.CopyTo(buffer);
        buffer[7] = (byte)messageLength;

        return buffer.ToArray();
    }

    /// <summary>
    /// Processes a single 64-bit block of data using SipHash compression.
    /// </summary>
    /// <param name="block">The 64-bit block to process.</param>
    /// <remarks>Updates internal state using <see cref="PerformSipRounds"/> and XOR operations.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        var b = BinaryPrimitives.ReadUInt64LittleEndian(block);
        this._v3 ^= b;
        this.PerformSipRounds(this._compressionRounds);
        this._v0 ^= b;
    }

    /// <summary>
    /// Finalizes the hash computation and produces the output hash value.
    /// </summary>
    /// <returns>A byte array containing the final hash value (8 or 16 bytes).</returns>
    /// <remarks>Combines all partial input and applies the finalization round logic based on the configured output size.</remarks>
    protected override byte[] ProcessFinalBlock()
    {
        this._v2 ^= (this.HashSizeValue == 64) ? 0xffUL : 0xeeUL;
        this.PerformSipRounds(this._finalizationRounds);

        var hash = new byte[this.HashSizeValue / 8];

        // First 64-bit output
        var h0 = this._v0 ^ this._v1 ^ this._v2 ^ this._v3;
        MemoryMarshal.Write(hash.AsSpan(0, 8), in h0);

        // Optional second block for SipHash-128
        if (this.HashSizeValue == 128)
        {
            this._v1 ^= 0xdd;
            this.PerformSipRounds(this._finalizationRounds);

            var h1 = this._v0 ^ this._v1 ^ this._v2 ^ this._v3;
            MemoryMarshal.Write(hash.AsSpan(8, 8), in h1);
        }

        return hash;
    }

    /// <summary>
    /// Rebuilds the internal SipHash state vectors from the current key whenever the key is assigned or the instance is re-initialised.
    /// </summary>
    /// <remarks>XORs the key halves with the SipHash initial constants, then applies the SipHash-128 finalisation tweak if required.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void OnKeyChanged()
    {
        // OnKeyChanged is only invoked by the Key setter and Initialize, both of which guarantee
        // KeyValue is non-null and of the expected length before this point.
        // Use little-endian reads to match the SipHash specification and ProcessBlock; prior code used
        // host-endian BitConverter, which would produce incorrect digests on big-endian hosts.
        var k0 = BinaryPrimitives.ReadUInt64LittleEndian(this.KeyValue!.AsSpan(0));
        var k1 = BinaryPrimitives.ReadUInt64LittleEndian(this.KeyValue!.AsSpan(8));
        this._v0 = s_initialStates[0] ^ k0;
        this._v1 = s_initialStates[1] ^ k1;
        this._v2 = s_initialStates[2] ^ k0;
        this._v3 = s_initialStates[3] ^ k1;

        if (this.HashSizeValue == 128) this._v1 ^= 0xee;
    }

    /// <summary>
    /// Performs a fixed number of SipHash compression or finalization rounds on the internal state.
    /// </summary>
    /// <param name="iterations">The number of rounds to perform.</param>
    /// <remarks>Each round consists of multiple bitwise operations and rotations defined by the SipHash specification.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.ReadabilityRules",
        "SA1107:Code should not contain multiple statements on one line",
        Justification = "Grouped state assignments intentionally mirror the four-word SipHash state transition, keeping related state loads and write-backs together as single logical permutation steps.")]
    private void PerformSipRounds(int iterations)
    {
        ulong r0 = this._v0, r1 = this._v1, r2 = this._v2, r3 = this._v3;

        for (var i = 0; i < iterations; i++)
        {
            r0 += r1;
            r1 = r1.RotateBitsLeftUnchecked(13);
            r1 ^= r0;
            r0 = r0.RotateBitsLeftUnchecked(32);
            r2 += r3;
            r3 = r3.RotateBitsLeftUnchecked(16);
            r3 ^= r2;
            r0 += r3;
            r3 = r3.RotateBitsLeftUnchecked(21);
            r3 ^= r0;
            r2 += r1;
            r1 = r1.RotateBitsLeftUnchecked(17);
            r1 ^= r2;
            r2 = r2.RotateBitsLeftUnchecked(32);
        }

        this._v0 = r0; this._v1 = r1; this._v2 = r2; this._v3 = r3;
    }
}
