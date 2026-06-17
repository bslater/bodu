// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Abstract base class for ASCON cryptographic hash algorithms as defined in NIST SP 800-232. Implements the shared
/// sponge construction, padding, and Ascon-p permutation used by all fixed-output ASCON hash variants.
/// </summary>
/// <typeparam name="T">The concrete hash algorithm type derived from this class.</typeparam>
/// <remarks>
/// <para>
/// All ASCON hash algorithms share a 320-bit internal state comprising five 64-bit words, a 64-bit (8-byte) rate, and a
/// 256-bit output. They differ in their pre-computed initialization state and in the number of Ascon-p rounds applied
/// after each absorbed block. The initial squeeze always uses the full 12-round permutation (Ascon-p12); subsequent
/// squeeze blocks use the same round count as absorption.
/// </para>
/// <para>
/// Padding follows the Ascon convention: the byte immediately after the last input byte is set to <c>0x01</c> (the
/// little-endian sentinel bit), and the remaining rate bytes are zero. A padding block is always appended, even when
/// the message length is a multiple of the eight-byte rate.
/// </para>
/// <para>
/// Concrete derived types supply the five pre-computed post-initialization state words and the absorption round count
/// via the protected constructor. No further overrides are required.
/// </para>
/// <para>
/// The concrete type <typeparamref name="T" /> must also expose a public parameterless constructor to satisfy the base
/// class's <c>new()</c> constraint.
/// </para>
/// </remarks>
public abstract partial class AsconHash<T>
    : BlockHashAlgorithm<T>
    where T : AsconHash<T>, new()
{
    /// <summary>
    /// The canonical algorithm identifier string supplied by the derived variant.
    /// </summary>
    private readonly string _algorithmName;

    /// <summary>
    /// The number of Ascon-p permutation rounds applied after each absorbed message block.
    /// </summary>
    private readonly int _absorptionRounds;

    /// <summary>
    /// The pre-computed post-initialization state word 0 supplied by the derived variant.
    /// </summary>
    private readonly ulong _iv0;

    /// <summary>
    /// The pre-computed post-initialization state word 1 supplied by the derived variant.
    /// </summary>
    private readonly ulong _iv1;

    /// <summary>
    /// The pre-computed post-initialization state word 2 supplied by the derived variant.
    /// </summary>
    private readonly ulong _iv2;

    /// <summary>
    /// The pre-computed post-initialization state word 3 supplied by the derived variant.
    /// </summary>
    private readonly ulong _iv3;

    /// <summary>
    /// The pre-computed post-initialization state word 4 supplied by the derived variant.
    /// </summary>
    private readonly ulong _iv4;

    /// <summary>
    /// Indicates whether the next processed block is the final padded block and must use the 12-round Ascon-p12
    /// permutation rather than the configured absorption round count.
    /// </summary>
    private bool _useP12ForFinalPad;

    /// <summary>
    /// The 320-bit Ascon sponge state comprising five 64-bit words, updated in place during absorption and squeezing.
    /// </summary>
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
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="absorptionRounds" /> is less than 1 or greater than 12.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The IV state words are assigned together to mirror the five-word Ascon state layout and keep the constructor initialization visually aligned with the algorithm specification.")]
    protected AsconHash(ulong iv0, ulong iv1, ulong iv2, ulong iv3, ulong iv4, int absorptionRounds, string algorithmName)
        : base(64)
    {
        ThrowHelper.ThrowIfNull(algorithmName);
        ThrowHelper.ThrowIfLessThan(absorptionRounds, 1);
        ThrowHelper.ThrowIfGreaterThan(absorptionRounds, 12);

        _iv0 = iv0; _iv1 = iv1; _iv2 = iv2; _iv3 = iv3; _iv4 = iv4;
        _absorptionRounds = absorptionRounds;
        _algorithmName = algorithmName;
        HashSizeValue = 256;
        Initialize();
    }

    /// <summary>
    /// Gets the canonical algorithm name for this hash function variant as defined in NIST SP 800-232.
    /// </summary>
    /// <value>A string such as <c>"ASCON-HASH256"</c> or <c>"ASCON-HASHA256"</c> identifying the variant.</value>
    /// <returns>The algorithm identifier string supplied at construction.</returns>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return _algorithmName;
        }
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// Loads the pre-computed initial state directly — no permutation is needed because the constants supplied by the
    /// derived class are already the result of applying Ascon-p12 to the raw IV.
    /// </remarks>
    public override void Initialize()
    {
        base.Initialize();
        _state = new AsconState { S0 = _iv0, S1 = _iv1, S2 = _iv2, S3 = _iv3, S4 = _iv4 };
        _useP12ForFinalPad = false;
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the internal sponge state.
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
            _state.Clear();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Pads the final partial input block according to the Ascon padding rule.
    /// </summary>
    /// <param name="block">
    /// The residual input bytes (zero to seven bytes) remaining after all complete 8-byte blocks have been processed.
    /// </param>
    /// <param name="messageLength">
    /// The total number of input bytes consumed before this call. Not used by Ascon padding.
    /// </param>
    /// <returns>
    /// An 8-byte array containing the residual bytes followed by <c>0x01</c> at the next position and zero bytes
    /// thereafter, matching the little-endian word representation used throughout the Ascon sponge state.
    /// </returns>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        Span<byte> padded = stackalloc byte[8];
        block.CopyTo(padded);
        padded[block.Length] = 0x01;

        // Signal ProcessBlock to use 12 rounds for this final padded block, which corresponds
        // to the initial squeeze permutation in the reference implementation.
        _useP12ForFinalPad = true;
        return padded.ToArray();
    }

    /// <summary>
    /// Absorbs a single 8-byte rate block into the sponge state and applies the Ascon-p permutation. Full message
    /// blocks use the configured absorption round count; the final padded block always uses 12 rounds to match the
    /// reference squeeze initialization.
    /// </summary>
    /// <param name="block">The 8-byte input block to absorb. Its length must equal the configured block size.</param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        _state.AbsorbRate64(block);

        // The final padded block must use 12 rounds (Ascon-p12) because it corresponds to the
        // initial permutation of the squeeze phase in the reference algorithm. Regular absorption
        // blocks use _absorptionRounds (12 for HASH256, 8 for HASHA256).
        int rounds = _useP12ForFinalPad ? 12 : _absorptionRounds;
        _useP12ForFinalPad = false;
        _state.Permute(rounds);
    }

    /// <summary>
    /// Squeezes the 256-bit hash output from the sponge state by extracting four successive 64-bit words, with
    /// <c>_absorptionRounds</c> Ascon-p permutation rounds applied between each extraction.
    /// </summary>
    /// <returns>A 32-byte array containing the final hash digest.</returns>
    protected override byte[] ProcessFinalBlock()
    {
        byte[] hash = new byte[32];

        _state.SqueezeRate64(hash.AsSpan(0, 8));
        _state.Permute(_absorptionRounds);
        _state.SqueezeRate64(hash.AsSpan(8, 8));
        _state.Permute(_absorptionRounds);
        _state.SqueezeRate64(hash.AsSpan(16, 8));
        _state.Permute(_absorptionRounds);
        _state.SqueezeRate64(hash.AsSpan(24, 8));

        return hash;
    }
}
