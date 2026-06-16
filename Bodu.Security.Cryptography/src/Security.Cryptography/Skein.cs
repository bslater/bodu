// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed implementations of the <c>Skein</c> family of cryptographic hash
/// functions, built by Bruce Schneier and co-authors on top of the <see cref="ThreefishBlockCipher" /> tweakable block
/// cipher and submitted as a finalist to the NIST SHA-3 competition.
/// </summary>
/// <typeparam name="T">
/// The concrete Skein variant derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// Skein hashes a message by repeatedly applying the <c>UBI</c> (Unique Block Iteration) mode of operation. Each UBI
/// call feeds the current chaining value and the next message block into <see cref="ThreefishBlockCipher.Encrypt" />
/// under a tweak that identifies the block's role (configuration, optional key, message, output) together with its
/// position and its first / final flags. The new chaining value is the encryption output XORed with the block, giving
/// the classic Matyas-Meyer-Oseas construction.
/// </para>
/// <para>
/// The base inherits from <see cref="KeyedBlockHashAlgorithm{T}" /> so that the keyed <c>Skein-MAC</c> mode integrates
/// with the shared keyed-hash test infrastructure. Unlike strict keyed-MAC algorithms such as <see cref="SipHash{T}" />
/// , Skein accepts a <em>variable-length optional</em> key: an empty <see cref="Key" /> selects the canonical
/// plain-hash profile (no KEY UBI phase), while any non-empty byte sequence enables Skein-MAC with a preliminary KEY
/// UBI phase.
/// </para>
/// <para>
/// Three fixed state sizes are supported, each implemented by a sealed derived class that wires up the corresponding
/// Threefish variant:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Skein256" /> — 256-bit state, 32-byte blocks, over <see cref="Threefish256Cipher" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Skein512" /> — 512-bit state, 64-byte blocks, over <see cref="Threefish512Cipher" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Skein1024" /> — 1024-bit state, 128-byte blocks, over <see cref="Threefish1024Cipher" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// Only the sequential hashing profile of Skein is implemented. Tree hashing, personalization strings, public-key or
/// key-derivation identifiers, and nonce modes are not exposed; the corresponding Skein tweak types are reserved for
/// potential future extension (see <see cref="SkeinTweakType" />).
/// </para>
/// <para>
/// <strong>When to choose Skein.</strong> Pick the Skein family for interop with code that has standardized on it (the
/// SHA-3 finalist round attracted a long tail of adopters, and Skein remains common in research code). Skein-512 is the
/// recommended default; Skein-256 is the narrower variant and Skein-1024 the widest. For new general-purpose
/// cryptographic hashing without an interop requirement <see cref="Blake2b" /> is faster on commodity 64-bit hardware
/// and SHA-2 / SHA-3 are more widely deployed. The Threefish primitive itself is also available standalone via
/// <see cref="Threefish" />.
/// </para>
/// </remarks>
/// <seealso cref="Skein256"/> <seealso cref="Skein512"/> <seealso cref="Skein1024"/> <seealso cref="Threefish"/>
/// <seealso cref="KeyedBlockHashAlgorithm{T}"/>
public abstract partial class Skein<T>
    : KeyedBlockHashAlgorithm<T>
    where T : Skein<T>, new()
{
    /// <summary>
    /// Maximum accepted length for <see cref="Key" /> across every Skein variant is 8192 bits (1024 bytes). Keys longer
    /// than this bound are rejected to prevent unbounded memory usage; this value is far above any practical MAC key.
    /// </summary>
    public const int MaxKeySize = 8192;

    /// <summary>
    /// The schema identifier placed in the first four bytes of the configuration block.
    /// </summary>
    /// <remarks>
    /// Spells ASCII <c>"SHA3"</c> in little-endian byte order, as required by the Skein 1.3 specification.
    /// </remarks>
    private const uint SchemaIdentifier = 0x33414853;

    /// <summary>
    /// The Skein protocol version encoded into the configuration block.
    /// </summary>
    private const ushort SchemaVersion = 1;

    /// <summary>
    /// Length of the Skein tweak value is 128 bits (16 bytes). Byte length is derived inline via
    /// <see cref="TweakSize" /> / 8 where needed.
    /// </summary>
    private const int TweakSize = 128;

    private readonly ThreefishBlockCipher _cipher;
    private readonly ulong[] _state;
    private readonly ulong[] _initialChainingValue;
    private readonly byte[] _pendingBlock;
    private readonly byte[] _ubiCipherOutput;
    private int _pendingBytes;
    private ulong _messageBytesProcessed;
    private bool _hasProcessedAnyMessageBlock;
    private bool _isChainingValueCached;


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
        CryptographyThrowHelper.ThrowIfInvalidHashSize(hashSizeBits, validHashSizesBits);

        _cipher = cipher;
        HashSizeValue = hashSizeBits;

        int blockBytes = cipher.BlockSize / 8;
        int stateWords = blockBytes / sizeof(ulong);
        _state = new ulong[stateWords];
        _initialChainingValue = new ulong[stateWords];
        _pendingBlock = new byte[blockBytes];
        _ubiCipherOutput = new byte[blockBytes];

        // Default to the plain-hash profile: empty key means no KEY UBI phase is run. Callers that want the
        // keyed Skein-MAC mode supply a non-empty key via the Key property before hashing.
        KeyValue = [];
    }

    /// <summary>
    /// Gets or sets the secret key used to switch Skein into its keyed <c>Skein-MAC</c> mode.
    /// </summary>
    /// <value>
    /// A byte array holding the key material. An empty array — the default — produces a plain, unkeyed hash; a
    /// non-empty array triggers a preliminary <c>KEY</c> UBI phase whenever the algorithm is initialized. Both the
    /// getter and the setter operate on defensive copies so external callers cannot mutate the internal key.
    /// </value>
    /// <returns>A defensive copy of the current key, or an empty array if no key has been configured.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="SipHash{T}" />, Skein does not require a fixed key length: any byte sequence from zero up to
    /// <see cref="MaxKeySize" /> / 8 bytes is valid. Setting the key clears any cached initial chaining value so the
    /// next call to <see cref="Initialize" /> rebuilds the state from the UBI pipeline <c>KEY → CFG</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// The assigned key is longer than <see cref="MaxKeySize" /> / 8 bytes.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// A hash computation has already started and the key may not be reassigned while the algorithm is in use.
    /// </exception>
    public override byte[] Key
    {
        get
        {
            ThrowIfDisposed();

            return KeyValue is null
                ? throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KeyNotSet)
                : KeyValue?.Copy() ?? [];
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfNull(value);

            if (value.Length > MaxKeySize / 8)
                throw new CryptographicException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CryptoResourceStrings.Crypt_Invalid_KeySize,
                        value.Length * 8,
                        $"0..{MaxKeySize}"));

            KeyValue = value.Copy();
            _isChainingValueCached = false;
        }
    }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the state size and the configured output size.
    /// </summary>
    /// <returns>A string of the form <c>"Skein-<i>s</i>-<i>h</i>"</c> — e.g. <c>"Skein-512-256"</c>.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return $"Skein-{BlockSize}-{HashSizeValue}";
        }
    }

    /// <summary>
    /// Resets the algorithm to its initial state, recomputing the chaining value from the configuration block (and the
    /// key, if one has been supplied) so that a fresh hash or MAC may be computed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">
    /// The internal key storage has been cleared (set to <see langword="null" />) — the key must be reassigned before
    /// the instance can be reused.
    /// </exception>
    public override void Initialize()
    {
        ThrowIfDisposed();

        if (KeyValue is null)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KeyNotSet);

        _pendingBytes = 0;
        _messageBytesProcessed = 0UL;
        _hasProcessedAnyMessageBlock = false;
        CryptographyHelper.Clear(_pendingBlock);

        EnsureChainingValueInitialized();
        Array.Copy(_initialChainingValue, _state, _state.Length);
    }

    /// <inheritdoc />
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowIfDisposed();


        HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();


        EnsureChainingStateReadyForHashing();
        AbsorbMessage(source);
    }

    /// <summary>
    /// Finalizes the Skein computation by flushing the residual message block and running the output UBI chain to
    /// produce the digest.
    /// </summary>
    /// <returns>
    /// A byte array containing the computed hash, of length <see cref="HashAlgorithm.HashSize" /> / 8.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    protected override byte[] HashFinal()
    {
        ThrowIfDisposed();


        EnsureChainingStateReadyForHashing();

        // Process the final message block. The tweak's position field carries the total number of real message bytes
        // absorbed (excluding any zero padding applied to bring the block up to the Skein state size).
        int residual = _pendingBytes;
        int blockBytes = BlockSize / 8;
        if (residual < blockBytes)
        {
            CryptographyHelper.Clear(_pendingBlock.AsSpan(residual, blockBytes - residual));
        }

        _messageBytesProcessed += (ulong)residual;
        Ubi(
            _pendingBlock,
            SkeinTweakType.Msg,
            first: !_hasProcessedAnyMessageBlock,
            final: true,
            position: _messageBytesProcessed);

        int outputBytes = HashSizeValue / 8;
        byte[] digest = GC.AllocateUninitializedArray<byte>(outputBytes);
        GenerateOutput(digest);

        _pendingBytes = 0;
        _messageBytesProcessed = 0UL;
        _hasProcessedAnyMessageBlock = false;
        return digest;
    }

    // ----------------------------------------------------------------------------------------------------
    // Pipeline-bypass overrides.
    //
    // Skein inherits the Merkle–Damgård <c>ProcessBlock → PadBlock → ProcessFinalBlock</c> abstracts from
    // <see cref="KeyedBlockHashAlgorithm{T}"/> for compatibility with the keyed-block test infrastructure,
    // but the UBI compression mode requires a one-block lookahead that the pipeline does not express. The
    // overrides above (Initialize, HashCore, HashFinal) drive UBI directly, so the inherited pipeline
    // methods are unreachable from any happy-path code path. They remain present as defensive contract
    // markers — anyone routing input through the inherited pipeline by mistake gets a loud, immediate
    // <see cref="InvalidOperationException"/> rather than silently incorrect output. Re-parenting Skein
    // onto a non-pipeline base would remove these throws but would also force a parallel refactor of the
    // shared keyed-block test infrastructure (SkeinTests → KeyedBlockHashAlgorithmTests → BlockHashAlgorithmTests),
    // which exceeds the value of removing the markers.
    // ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// Pipeline contract marker — see the section comment above. Skein's UBI lookahead bypasses <c>ProcessBlock</c>
    /// entirely; this override exists only to fail loudly if the inherited Merkle–Damgård pipeline is ever wired up
    /// against a Skein instance.
    /// </summary>
    /// <param name="block">Ignored.</param>
    /// <exception cref="InvalidOperationException">Always thrown — this method is not on the happy path.</exception>
    protected override void ProcessBlock(ReadOnlySpan<byte> block) =>
        throw new InvalidOperationException(
            CryptoResourceStrings.Op_Invalid_SkeinBypassesProcessBlock);

    /// <summary>
    /// Satisfies the <see cref="BlockHashAlgorithm{T}.PadBlock(ReadOnlySpan{byte}, ulong)" /> contract, but is not used
    /// by the Skein implementation.
    /// </summary>
    /// <param name="block">
    /// The final partial block supplied by the base pipeline. Skein does not consume this value here because final
    /// block processing is performed by the UBI chaining path.
    /// </param>
    /// <param name="messageLength">
    /// The total message length supplied by the base pipeline. Skein does not consume this value here because UBI
    /// encodes position and final-block state in the tweak field.
    /// </param>
    /// <returns>This method never returns because Skein bypasses the base padding pipeline.</returns>
    /// <exception cref="InvalidOperationException">
    /// Always thrown because Skein performs finalization through UBI rather than
    /// <see cref="BlockHashAlgorithm{T}.PadBlock(ReadOnlySpan{byte}, ulong)" />.
    /// </exception>
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) =>
        throw new InvalidOperationException(
            CryptoResourceStrings.Op_Invalid_SkeinBypassesPadBlock);

    /// <summary>
    /// Satisfies the base final-block processing contract, but is unreachable for Skein because hash finalization is
    /// performed by the OUTPUT UBI phase.
    /// </summary>
    /// <returns>This method never returns because Skein bypasses the base final-block pipeline.</returns>
    /// <exception cref="InvalidOperationException">
    /// Always thrown because Skein drives finalization from <see cref="HashFinal" /> rather than
    /// <see cref="BlockHashAlgorithm{T}.ProcessFinalBlock" />.
    /// </exception>
    protected override byte[] ProcessFinalBlock() =>
        throw new InvalidOperationException(
            CryptoResourceStrings.Op_Invalid_SkeinBypassesProcessFinalBlock);

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm, securely clears all intermediate state, and disposes the
    /// underlying Threefish cipher.
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
            CryptographyHelper.Clear(_initialChainingValue);
            CryptographyHelper.Clear(_pendingBlock);
            CryptographyHelper.Clear(_ubiCipherOutput);

            _cipher.Dispose();

            _isChainingValueCached = false;
            _pendingBytes = 0;
            _messageBytesProcessed = 0UL;
            _hasProcessedAnyMessageBlock = false;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Ensures the initial chaining value derived from the configuration (and optional key) UBI phases has been
    /// computed and cached for fast reuse on subsequent <see cref="Initialize" /> calls.
    /// </summary>
    private void EnsureChainingStateReadyForHashing()
    {
        if (_isChainingValueCached)
            return;

        EnsureChainingValueInitialized();
        Array.Copy(_initialChainingValue, _state, _state.Length);
    }
}
