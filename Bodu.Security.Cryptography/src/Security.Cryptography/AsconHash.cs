// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Abstract base class for ASCON cryptographic hash algorithms as defined in NIST SP 800-232. Implements the shared sponge
/// construction, padding, and Ascon-p permutation used by all fixed-output ASCON hash variants.
/// </summary>
/// <typeparam name="T">
/// The concrete hash algorithm type derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// All ASCON hash algorithms share a 320-bit internal state comprising five 64-bit words, a 64-bit (8-byte) rate, and a 256-bit
/// output. They differ in their initialisation vector and in the number of Ascon-p rounds applied after each absorbed block. The
/// initialisation and squeezing phases always use the full 12-round permutation (Ascon-p12).
/// </para>
/// <para>
/// Padding follows the Ascon convention: the byte immediately after the last input byte is set to <c>0x80</c>, and the remaining
/// rate bytes are zero. A padding block is always appended, even when the message length is a multiple of the eight-byte rate.
/// </para>
/// <para>
/// Concrete derived types supply the initialisation vector, absorption round count, and canonical algorithm name via the protected
/// constructor. No further overrides are required.
/// </para>
/// </remarks>
public abstract partial class AsconHash<T>
    : BlockHashAlgorithm<T>
    where T : AsconHash<T>, new()
{
    private readonly string _algorithmName;
    private readonly int _absorptionRounds;
    private readonly ulong _initializationVector;

    private bool _disposed;
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;
    private ulong _s4;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHash{T}" /> class with the specified algorithm parameters.
    /// </summary>
    /// <param name="initializationVector">
    /// The 64-bit initialisation vector encoding the algorithm variant, rate, and round counts per NIST SP 800-232.
    /// </param>
    /// <param name="absorptionRounds">
    /// The number of Ascon-p rounds applied after each absorbed block. Must be between 1 and 12 inclusive.
    /// </param>
    /// <param name="algorithmName">
    /// The canonical algorithm identifier string as defined in NIST SP 800-232. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="algorithmName" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="absorptionRounds" /> is less than 1 or greater than 12.
    /// </exception>
    protected AsconHash(ulong initializationVector, int absorptionRounds, string algorithmName)
        : base(8)
    {
        ThrowHelper.ThrowIfNull(algorithmName);
        ThrowHelper.ThrowIfLessThan(absorptionRounds, 1);
        ThrowHelper.ThrowIfGreaterThan(absorptionRounds, 12);

        this._initializationVector = initializationVector;
        this._absorptionRounds = absorptionRounds;
        this._algorithmName = algorithmName;
        this.HashSizeValue = 256;
        this.Initialize();
    }

    /// <summary>
    /// Gets the canonical algorithm name for this hash function variant as defined in NIST SP 800-232.
    /// </summary>
    /// <value>A string such as <c>"ASCON-HASH256"</c> or <c>"ASCON-HASHA256"</c> identifying the variant.</value>
    /// <returns>The algorithm identifier string supplied at construction.</returns>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    public string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return this._algorithmName;
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

        this._s0 = this._initializationVector;
        this._s1 = 0;
        this._s2 = 0;
        this._s3 = 0;
        this._s4 = 0;
        this.ApplyPermutation(12);
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the internal sponge state.
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
    /// An 8-byte array containing the residual bytes followed by <c>0x01</c> at the next position and zero bytes thereafter,
    /// matching the little-endian word representation used throughout the Ascon sponge state.
    /// </returns>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        Span<byte> padded = stackalloc byte[8];
        block.CopyTo(padded);
        padded[block.Length] = 0x01;
        return padded.ToArray();
    }

    /// <summary>
    /// Absorbs a single 8-byte rate block into the sponge state and applies the Ascon-p permutation using the configured
    /// absorption round count.
    /// </summary>
    /// <param name="block">The 8-byte input block to absorb. Its length must equal the configured block size.</param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        this._s0 ^= BinaryPrimitives.ReadUInt64LittleEndian(block);
        this.ApplyPermutation(this._absorptionRounds);
    }

    /// <summary>
    /// Squeezes the 256-bit hash output from the sponge state by extracting four successive 64-bit words, each preceded by a
    /// 12-round Ascon-p permutation except the first.
    /// </summary>
    /// <returns>A 32-byte array containing the final hash digest.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        byte[] hash = new byte[32];

        BinaryPrimitives.WriteUInt64LittleEndian(hash.AsSpan(0, 8), this._s0);
        this.ApplyPermutation(12);
        BinaryPrimitives.WriteUInt64LittleEndian(hash.AsSpan(8, 8), this._s0);
        this.ApplyPermutation(12);
        BinaryPrimitives.WriteUInt64LittleEndian(hash.AsSpan(16, 8), this._s0);
        this.ApplyPermutation(12);
        BinaryPrimitives.WriteUInt64LittleEndian(hash.AsSpan(24, 8), this._s0);

        return hash;
    }
}
