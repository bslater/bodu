// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed implementations of the <c>Skein</c> family of cryptographic hash
/// functions, built by Bruce Schneier and co-authors on top of the <see cref="ThreefishBlockCipher" /> tweakable block
/// cipher and submitted as a finalist to the NIST SHA-3 competition.
/// </summary>
/// <typeparam name="T">The concrete Skein variant derived from this class. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// Skein hashes a message by repeatedly applying the <c>UBI</c> (Unique Block Iteration) mode of operation. Each UBI
/// call feeds the current chaining value and the next message block into <see cref="ThreefishBlockCipher.Encrypt" /> under
/// a tweak that identifies the block's role (configuration, optional key, message, output) together with its position
/// and its first / final flags. The new chaining value is the encryption output XORed with the block, giving the classic
/// Matyas-Meyer-Oseas construction.
/// </para>
/// <para>
/// The base inherits from <see cref="KeyedBlockHashAlgorithm{T}" /> so that the keyed <c>Skein-MAC</c> mode integrates
/// with the shared keyed-hash test infrastructure. Unlike strict keyed-MAC algorithms such as <see cref="SipHash{T}" />,
/// Skein accepts a <em>variable-length optional</em> key: an empty <see cref="Key" /> selects the canonical plain-hash
/// profile (no KEY UBI phase), while any non-empty byte sequence enables Skein-MAC with a preliminary KEY UBI phase.
/// </para>
/// <para>
/// Three fixed state sizes are supported, each implemented by a sealed derived class that wires up the corresponding
/// Threefish variant:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Skein256" /> — 256-bit state, 32-byte blocks, over <see cref="Threefish256Cipher" />.</description></item>
/// <item><description><see cref="Skein512" /> — 512-bit state, 64-byte blocks, over <see cref="Threefish512Cipher" />.</description></item>
/// <item><description><see cref="Skein1024" /> — 1024-bit state, 128-byte blocks, over <see cref="Threefish1024Cipher" />.</description></item>
/// </list>
/// <para>
/// Only the sequential hashing profile of Skein is implemented. Tree hashing, personalization strings, public-key or
/// key-derivation identifiers, and nonce modes are not exposed; the corresponding Skein tweak types are reserved for
/// potential future extension (see <see cref="SkeinTweakType" />).
/// </para>
/// </remarks>
public abstract partial class Skein<T>
    : KeyedBlockHashAlgorithm<T>
    where T : Skein<T>, new()
{
    /// <summary>
    /// The maximum accepted length, in bytes, for <see cref="Key" /> across every Skein variant. Keys longer than this
    /// bound are rejected to prevent unbounded memory usage; this value is far above any practical MAC key.
    /// </summary>
    public const int MaxKeySizeBytes = 1024;

    /// <summary>
    /// The schema identifier placed in the first four bytes of the configuration block.
    /// </summary>
    /// <remarks>Spells ASCII <c>"SHA3"</c> in little-endian byte order, as required by the Skein 1.3 specification.</remarks>
    private const uint SchemaIdentifier = 0x33414853;

    /// <summary>
    /// The Skein protocol version encoded into the configuration block.
    /// </summary>
    private const ushort SchemaVersion = 1;

    /// <summary>
    /// The fixed size, in bytes, of a Skein tweak value.
    /// </summary>
    private const int TweakSizeBytes = 16;

    private readonly ThreefishBlockCipher _cipher;
    private readonly ulong[] _state;
    private readonly ulong[] _initialChainingValue;
    private readonly byte[] _pendingBlock;
    private readonly byte[] _ubiCipherOutput;
    private int _pendingBytes;
    private ulong _messageBytesProcessed;
    private bool _hasProcessedAnyMessageBlock;
    private bool _isChainingValueCached;
    private bool _disposed;

#if !NET6_0_OR_GREATER
    private bool _finalized;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein{T}" /> class using a pre-constructed cipher supplied by a
    /// derived variant.
    /// </summary>
    /// <param name="cipher">
    /// The <see cref="ThreefishBlockCipher" /> instance matching the derived Skein variant's state size. Takes
    /// ownership: the base class disposes it when the Skein instance is disposed. Every UBI call rebuilds the cipher's
    /// key and tweak schedules in place via <see cref="ThreefishBlockCipher.Rekey" />, so the key and tweak used when
    /// the cipher was constructed are overwritten before the first encryption.
    /// </param>
    /// <param name="hashSizeBits">The requested output size, in bits, for this instance.</param>
    /// <param name="validHashSizesBits">The set of output sizes, in bits, that the derived variant permits.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSizeBits" /> is not one of the values in <paramref name="validHashSizesBits" />.
    /// </exception>
    private protected Skein(ThreefishBlockCipher cipher, int hashSizeBits, int[] validHashSizesBits)
        : base(blockSize: cipher.BlockSize, keySize: cipher.BlockSize)
    {
        if (Array.IndexOf(validHashSizesBits, hashSizeBits) < 0)
        {
            cipher.Dispose();
            throw new ArgumentOutOfRangeException(
                nameof(hashSizeBits),
                string.Format(
                    ResourceStrings.CryptographicException_InvalidHashSize,
                    hashSizeBits,
                    string.Join(", ", validHashSizesBits)));
        }

        this._cipher = cipher;
        this.HashSizeValue = hashSizeBits;

        int stateWords = cipher.BlockSize / sizeof(ulong);
        this._state = new ulong[stateWords];
        this._initialChainingValue = new ulong[stateWords];
        this._pendingBlock = new byte[cipher.BlockSize];
        this._ubiCipherOutput = new byte[cipher.BlockSize];

        // Default to the plain-hash profile: empty key means no KEY UBI phase is run. Callers that want the
        // keyed Skein-MAC mode supply a non-empty key via the Key property before hashing.
        this.KeyValue = Array.Empty<byte>();
    }

    /// <summary>
    /// Gets or sets the secret key used to switch Skein into its keyed <c>Skein-MAC</c> mode.
    /// </summary>
    /// <value>
    /// A byte array holding the key material. An empty array — the default — produces a plain, unkeyed hash; a
    /// non-empty array triggers a preliminary <c>KEY</c> UBI phase whenever the algorithm is initialised. Both the
    /// getter and the setter operate on defensive copies so external callers cannot mutate the internal key.
    /// </value>
    /// <returns>A defensive copy of the current key, or an empty array if no key has been configured.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="SipHash{T}" />, Skein does not require a fixed key length: any byte sequence from zero up to
    /// <see cref="MaxKeySizeBytes" /> bytes is valid. Setting the key clears any cached initial chaining value so the
    /// next call to <see cref="Initialize" /> rebuilds the state from the UBI pipeline <c>KEY → CFG</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// The assigned key is longer than <see cref="MaxKeySizeBytes" /> bytes.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// A hash computation has already started and the key may not be reassigned while the algorithm is in use.
    /// </exception>
    public override byte[] Key
    {
        get
        {
            this.ThrowIfDisposed();
            return this.KeyValue.Copy();
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfNull(value);

            if (value.Length > MaxKeySizeBytes)
                throw new CryptographicException(
                    string.Format(
                        ResourceStrings.CryptographicException_InvalidKeySize,
                        value.Length,
                        $"0..{MaxKeySizeBytes}"));

            this.KeyValue = value.Copy();
            this._isChainingValueCached = false;
        }
    }

    /// <summary>
    /// Gets the number of bytes absorbed per Threefish block — equivalently, the Skein state size in bytes.
    /// </summary>
    /// <returns>The block size, in bytes, for this Skein variant (32, 64, or 128).</returns>
    public override int InputBlockSize => this.BlockSizeBytes;

    /// <summary>
    /// Gets the output block size, in bytes, produced per Skein transformation step — mirrors <see cref="InputBlockSize" />.
    /// </summary>
    /// <returns>The block size, in bytes, for this Skein variant (32, 64, or 128).</returns>
    public override int OutputBlockSize => this.BlockSizeBytes;

    /// <summary>
    /// Gets the fully qualified algorithm name, including the state size and the configured output size.
    /// </summary>
    /// <returns>A string of the form <c>"Skein-<i>s</i>-<i>h</i>"</c> — e.g. <c>"Skein-512-256"</c>.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return $"Skein-{this.BlockSizeBytes * 8}-{this.HashSizeValue}";
        }
    }

    /// <summary>
    /// Resets the algorithm to its initial state, recomputing the chaining value from the configuration block (and
    /// the key, if one has been supplied) so that a fresh hash or MAC may be computed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">
    /// The internal key storage has been cleared (set to <see langword="null" />) — the key must be reassigned before
    /// the instance can be reused.
    /// </exception>
    public override void Initialize()
    {
        this.ThrowIfDisposed();

        if (this.KeyValue is null)
            throw new CryptographicException(ResourceStrings.CryptographicException_KeyNotSet);

#if !NET6_0_OR_GREATER
        this.State = 0;
        this._finalized = false;
#endif

        this._pendingBytes = 0;
        this._messageBytesProcessed = 0UL;
        this._hasProcessedAnyMessageBlock = false;
        Array.Clear(this._pendingBlock, 0, this._pendingBlock.Length);

        this.EnsureChainingValueInitialized();
        Array.Copy(this._initialChainingValue, this._state, this._state.Length);
    }

    /// <inheritdoc />
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
        ThrowHelper.ThrowIfLessThan(ibStart, 0);
        ThrowHelper.ThrowIfLessThan(cbSize, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);
        if (this._finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        this.HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
        if (this._finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        this.EnsureChainingStateReadyForHashing();
        this.AbsorbMessage(source);
    }

    /// <summary>
    /// Finalises the Skein computation by flushing the residual message block and running the output UBI chain to
    /// produce the digest.
    /// </summary>
    /// <returns>A byte array containing the computed hash, of length <see cref="HashAlgorithm.HashSize" /> / 8.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
        if (this._finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
        this._finalized = true;
        this.State = 2;
#endif

        this.EnsureChainingStateReadyForHashing();

        // Process the final message block. The tweak's position field carries the total number of real message bytes
        // absorbed (excluding any zero padding applied to bring the block up to the Skein state size).
        int residual = this._pendingBytes;
        if (residual < this.BlockSizeBytes)
        {
            Array.Clear(this._pendingBlock, residual, this.BlockSizeBytes - residual);
        }

        this._messageBytesProcessed += (ulong)residual;
        this.Ubi(
            this._pendingBlock,
            SkeinTweakType.Msg,
            first: !this._hasProcessedAnyMessageBlock,
            final: true,
            position: this._messageBytesProcessed);

        int outputBytes = this.HashSizeValue / 8;
        byte[] digest = GC.AllocateUninitializedArray<byte>(outputBytes);
        this.GenerateOutput(digest);

        this._pendingBytes = 0;
        this._messageBytesProcessed = 0UL;
        this._hasProcessedAnyMessageBlock = false;
        return digest;
    }

    /// <summary>
    /// Processes a single aligned message block. Unused — Skein overrides <see cref="HashCore(ReadOnlySpan{byte})" />
    /// and <see cref="HashFinal" /> directly rather than participating in the
    /// <see cref="BlockHashAlgorithm{T}" /> <c>ProcessBlock → PadBlock → ProcessFinalBlock</c> pipeline, because the
    /// UBI mode requires a one-block lookahead that the pipeline does not express.
    /// </summary>
    /// <param name="block">Ignored.</param>
    /// <exception cref="InvalidOperationException">Always thrown — this method is not expected to be invoked.</exception>
    protected override void ProcessBlock(ReadOnlySpan<byte> block) =>
        throw new InvalidOperationException("Skein bypasses the BlockHashAlgorithm ProcessBlock pipeline.");

    /// <summary>
    /// Pads the final partial block. Unused — see <see cref="ProcessBlock" />.
    /// </summary>
    /// <param name="block">Ignored.</param>
    /// <param name="messageLength">Ignored.</param>
    /// <returns>Never returns — always throws.</returns>
    /// <exception cref="InvalidOperationException">Always thrown — this method is not expected to be invoked.</exception>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) =>
        throw new InvalidOperationException("Skein bypasses the BlockHashAlgorithm PadBlock pipeline.");

    /// <summary>
    /// Finalises the computation via the block pipeline. Unused — see <see cref="ProcessBlock" />.
    /// </summary>
    /// <returns>Never returns — always throws.</returns>
    /// <exception cref="InvalidOperationException">Always thrown — this method is not expected to be invoked.</exception>
    protected override byte[] ProcessFinalBlock() =>
        throw new InvalidOperationException("Skein bypasses the BlockHashAlgorithm ProcessFinalBlock pipeline.");

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm, securely clears all intermediate state, and disposes the
    /// underlying Threefish cipher.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(this._state);
            CryptoHelpers.Clear(this._initialChainingValue);
            CryptoHelpers.Clear(this._pendingBlock);
            CryptoHelpers.Clear(this._ubiCipherOutput);
            CryptoHelpers.ClearAndNullify(ref this.HashValue);

            this._cipher.Dispose();
            this._isChainingValueCached = false;
            this._pendingBytes = 0;
            this._messageBytesProcessed = 0UL;
            this._hasProcessedAnyMessageBlock = false;
            this.HashSizeValue = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the instance has already been disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }

    /// <summary>
    /// Throws if the algorithm has begun processing input and can no longer be reconfigured.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }

    /// <summary>
    /// Ensures the initial chaining value derived from the configuration (and optional key) UBI phases has been
    /// computed and cached for fast reuse on subsequent <see cref="Initialize" /> calls.
    /// </summary>
    private void EnsureChainingStateReadyForHashing()
    {
        if (this._isChainingValueCached)
            return;

        this.EnsureChainingValueInitialized();
        Array.Copy(this._initialChainingValue, this._state, this._state.Length);
    }
}
