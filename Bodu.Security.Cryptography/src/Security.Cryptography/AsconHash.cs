// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// output. They differ in their pre-computed initialisation state and in the number of Ascon-p rounds applied after each absorbed
/// block. The initial squeeze always uses the full 12-round permutation (Ascon-p12); subsequent squeeze blocks use the same round
/// count as absorption.
/// </para>
/// <para>
/// Padding follows the Ascon convention: the byte immediately after the last input byte is set to <c>0x01</c> (the little-endian
/// sentinel bit), and the remaining rate bytes are zero. A padding block is always appended, even when the message length is a
/// multiple of the eight-byte rate.
/// </para>
/// <para>
/// Concrete derived types supply the five pre-computed post-initialisation state words and the absorption round count via the
/// protected constructor. No further overrides are required.
/// </para>
/// </remarks>
public abstract partial class AsconHash<T>
    : BlockHashAlgorithm<T>
    where T : AsconHash<T>, new()
{
    private readonly string _algorithmName;
    private readonly int _absorptionRounds;
    private readonly ulong _iv0;
    private readonly ulong _iv1;
    private readonly ulong _iv2;
    private readonly ulong _iv3;
    private readonly ulong _iv4;

    private bool _disposed;
    private bool _useP12ForFinalPad;
    private AsconState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconHash{T}" /> class with the specified algorithm parameters.
    /// </summary>
    /// <param name="iv0">Pre-computed initial state word 0 (result of applying Ascon-p12 to the raw IV).</param>
    /// <param name="iv1">Pre-computed initial state word 1.</param>
    /// <param name="iv2">Pre-computed initial state word 2.</param>
    /// <param name="iv3">Pre-computed initial state word 3.</param>
    /// <param name="iv4">Pre-computed initial state word 4.</param>
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
    protected AsconHash(ulong iv0, ulong iv1, ulong iv2, ulong iv3, ulong iv4, int absorptionRounds, string algorithmName)
        : base(8)
    {
        ThrowHelper.ThrowIfNull(algorithmName);
        ThrowHelper.ThrowIfLessThan(absorptionRounds, 1);
        ThrowHelper.ThrowIfGreaterThan(absorptionRounds, 12);

        this._iv0 = iv0; this._iv1 = iv1; this._iv2 = iv2; this._iv3 = iv3; this._iv4 = iv4;
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
    /// <remarks>
    /// Loads the pre-computed initial state directly — no permutation is needed because the constants
    /// supplied by the derived class are already the result of applying Ascon-p12 to the raw IV.
    /// </remarks>
    protected override void OnInitialize()
    {
        this._state = new AsconState { S0 = this._iv0, S1 = this._iv1, S2 = this._iv2, S3 = this._iv3, S4 = this._iv4 };
        this._useP12ForFinalPad = false;
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
            this._state.Clear();
            this.HashSizeValue = 0;
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

        // Signal ProcessBlock to use 12 rounds for this final padded block, which corresponds
        // to the initial squeeze permutation in the reference implementation.
        this._useP12ForFinalPad = true;
        return padded.ToArray();
    }

    /// <summary>
    /// Absorbs a single 8-byte rate block into the sponge state and applies the Ascon-p permutation. Full message blocks use
    /// the configured absorption round count; the final padded block always uses 12 rounds to match the reference squeeze
    /// initialisation.
    /// </summary>
    /// <param name="block">The 8-byte input block to absorb. Its length must equal the configured block size.</param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        this._state.AbsorbRate64(block);

        // The final padded block must use 12 rounds (Ascon-p12) because it corresponds to the
        // initial permutation of the squeeze phase in the reference algorithm. Regular absorption
        // blocks use _absorptionRounds (12 for HASH256, 8 for HASHA256).
        int rounds = this._useP12ForFinalPad ? 12 : this._absorptionRounds;
        this._useP12ForFinalPad = false;
        this._state.Permute(rounds);
    }

    /// <summary>
    /// Squeezes the 256-bit hash output from the sponge state by extracting four successive 64-bit words, with
    /// <c>_absorptionRounds</c> Ascon-p permutation rounds applied between each extraction.
    /// </summary>
    /// <returns>A 32-byte array containing the final hash digest.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        byte[] hash = new byte[32];

        this._state.SqueezeRate64(hash.AsSpan(0, 8));
        this._state.Permute(this._absorptionRounds);
        this._state.SqueezeRate64(hash.AsSpan(8, 8));
        this._state.Permute(this._absorptionRounds);
        this._state.SqueezeRate64(hash.AsSpan(16, 8));
        this._state.Permute(this._absorptionRounds);
        this._state.SqueezeRate64(hash.AsSpan(24, 8));

        return hash;
    }
}
