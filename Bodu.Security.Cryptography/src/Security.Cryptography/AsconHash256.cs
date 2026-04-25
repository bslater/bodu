// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>ASCON-HASH256</c> cryptographic hash algorithm as defined in NIST SP 800-232. Produces a 256-bit
/// (32-byte) digest using the Ascon-p permutation over a 320-bit sponge state. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ASCON-HASH256 is a sponge-construction hash function with a 320-bit internal state comprising five 64-bit words. Input is absorbed
/// in 64-bit (8-byte) rate blocks; the 12-round Ascon-p permutation is applied after each block. The 256-bit output is produced by
/// squeezing four successive 64-bit words from the state, interleaved with a 12-round permutation between each squeeze.
/// </para>
/// <para>
/// Padding follows the Ascon convention: the byte immediately after the last input byte is set to <c>0x80</c>, and the remaining
/// bytes in the rate block are set to zero. A padding block is always appended, even when the message length is a multiple of the
/// eight-byte rate.
/// </para>
/// <para>
/// ASCON-HASH256 was standardised by NIST in SP 800-232 as part of the ASCON lightweight cryptography suite. It is well suited for
/// constrained environments where a formally standardised, lightweight hash function is required.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var hash = new AsconHash256();
/// byte[] digest = hash.ComputeHash(message);
/// </code>
/// </example>
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
public sealed partial class AsconHash256
    : BlockHashAlgorithm<AsconHash256>
{
    /// <summary>
    /// The initialisation vector for ASCON-HASH256 as defined in NIST SP 800-232, encoding key length (0), rate (64 bits),
    /// and round counts (a = 12, b = 12).
    /// </summary>
    private const ulong InitializationVector = 0x00400c0000000100UL;

    private bool _disposed;
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;
    private ulong _s4;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHash256" /> class.
    /// </summary>
    public AsconHash256()
        : base(8)
    {
        this.HashSizeValue = 256;
        this.Initialize();
    }

    /// <summary>
    /// Gets the canonical algorithm name for this hash function.
    /// </summary>
    /// <value>The string <c>"ASCON-HASH256"</c> as defined in NIST SP 800-232.</value>
    /// <returns>The fixed string <c>"ASCON-HASH256"</c>.</returns>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    public string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return "ASCON-HASH256";
        }
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    public override int InputBlockSize => 8;

    /// <inheritdoc />
    public override int OutputBlockSize => 32;

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
        base.Initialize();

        this._s0 = InitializationVector;
        this._s1 = 0;
        this._s2 = 0;
        this._s3 = 0;
        this._s4 = 0;
        this.ApplyPermutation(12);
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the internal state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged
    /// resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.ClearAndNullify(ref this.HashValue);
            this._s0 = this._s1 = this._s2 = this._s3 = this._s4 = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Pads the final partial input block according to the Ascon padding rule.
    /// </summary>
    /// <param name="block">
    /// The residual input bytes (zero to seven bytes) remaining after all complete 8-byte blocks have been processed.
    /// </param>
    /// <param name="messageLength">The total number of input bytes consumed before this call. Not used by Ascon padding.</param>
    /// <returns>
    /// An 8-byte array containing the residual bytes followed by <c>0x80</c> at the next position and zero bytes thereafter.
    /// </returns>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        Span<byte> padded = stackalloc byte[8];
        block.CopyTo(padded);
        padded[block.Length] = 0x80;
        return padded.ToArray();
    }

    /// <summary>
    /// Absorbs a single 8-byte rate block into the sponge state and applies the Ascon-p12 permutation.
    /// </summary>
    /// <param name="block">The 8-byte input block to absorb. Its length must equal the configured block size.</param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        this._s0 ^= BinaryPrimitives.ReadUInt64BigEndian(block);
        this.ApplyPermutation(12);
    }

    /// <summary>
    /// Squeezes the 256-bit hash output from the sponge state by extracting four successive 64-bit words, each separated by a
    /// 12-round Ascon-p permutation.
    /// </summary>
    /// <returns>A 32-byte array containing the ASCON-HASH256 digest.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        byte[] hash = new byte[32];

        BinaryPrimitives.WriteUInt64BigEndian(hash.AsSpan(0, 8), this._s0);
        this.ApplyPermutation(12);
        BinaryPrimitives.WriteUInt64BigEndian(hash.AsSpan(8, 8), this._s0);
        this.ApplyPermutation(12);
        BinaryPrimitives.WriteUInt64BigEndian(hash.AsSpan(16, 8), this._s0);
        this.ApplyPermutation(12);
        BinaryPrimitives.WriteUInt64BigEndian(hash.AsSpan(24, 8), this._s0);

        return hash;
    }
}
