// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHash.cs" company="PlaceholderCompany">
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
/// Computes a hash using the <c>CubeHash</c> permutation-based hash algorithm designed by Daniel J. Bernstein and submitted to the
/// NIST SHA-3 competition. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CubeHash"/> operates on a 1024-bit internal state updated through a sequence of ARX (Addition, Rotation, XOR)
/// operations. The number of initialisation, transformation, and finalisation rounds, the hash output size, and the input block size
/// are all configurable. See <a href="https://en.wikipedia.org/wiki/CubeHash">Wikipedia</a> for an overview.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>State size: 1024 bits (32 × 32-bit words).</description></item>
///   <item><description>Output size: configurable, <see cref="MinHashSize"/>–<see cref="MaxHashSize"/> bits (default 512).</description></item>
///   <item><description>Input block size: configurable, <see cref="MinInputBlockSize"/>–<see cref="MaxInputBlockSize"/> bytes (default 32).</description></item>
///   <item><description>Rounds: initialisation, per-block, and finalisation counts each independently configurable up to <see cref="MaxRounds"/>; defaults are 16 / 16 / 32.</description></item>
/// </list>
/// <para>
/// <strong>When to choose CubeHash.</strong> Pick CubeHash for academic study, cryptographic competition
/// reproducibility, or interop with code that has settled on a specific CubeHash parameterisation. The defaults
/// (CubeHash 16/32/+160/512) match the SHA-3 competition submission. For new general-purpose cryptographic
/// hashing prefer SHA-2, SHA-3, or <see cref="Blake2b"/>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using Bodu.Security.Cryptography;
///
/// // Default parameters: 512-bit output, 32-byte input block, 16/16/32 rounds.
/// using var cube = new CubeHash();
/// byte[] digest = cube.ComputeHash(message);
/// </code>
/// </example>
public sealed class CubeHash
    : System.Security.Cryptography.HashAlgorithm
{
    /// <summary>
    /// The maximum allowable size of the computed hash, in bits.
    /// </summary>
    public const int MaxHashSize = 512;

    /// <summary>
    /// The maximum allowable size of the input block, in bytes.
    /// </summary>
    public const int MaxInputBlockSize = 128;

    /// <summary>
    /// The maximum number of rounds permitted for initialisation, processing, or finalization.
    /// </summary>
    public const int MaxRounds = 4096;

    /// <summary>
    /// The minimum allowable size of the computed hash, in bits.
    /// </summary>
    public const int MinHashSize = 8;

    /// <summary>
    /// The minimum allowable size of the input block, in bytes.
    /// </summary>
    public const int MinInputBlockSize = 1;

    /// <summary>
    /// The minimum number of rounds permitted for initialisation, processing, or finalization.
    /// </summary>
    public const int MinRounds = 1;

    private bool _disposed = false;

    // Internal algorithm parameters
    private int _finalizationRounds;

    private int _initializationRounds;
    private uint[] _initializedState;
    private int _inputBlockSizeBytes;
    private bool _isInitializedStateCached = false;

    // Number of bytes accumulated in the current partial block
    private int _pendingBytes;

    private int _rounds;
    private uint[] _state;

#if !NET6_0_OR_GREATER
    private bool _finalized; // flag to block reuse in older .NET
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="CubeHash"/> class with default parameters.
    /// </summary>
    public CubeHash()
    {
        this._state = new uint[32];
        this._initializedState = new uint[32];
        this.HashSizeValue = 512;
        this._inputBlockSizeBytes = 32;
        this._rounds = 16;
        this._initializationRounds = 16;
        this._finalizationRounds = 32;
    }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the variant and hash output size.
    /// </summary>
    /// <remarks>
    /// <para>Follows the <see cref="CubeHash"/> naming convention from the original submission: <c>CubeHashr+b/w+f-h</c>, where:</para>
    /// <list type="bullet">
    /// <item>
    /// <description><c>r</c> = number of initialisation rounds</description>
    /// </item>
    /// <item>
    /// <description><c>b</c> = number of transformation rounds per block</description>
    /// </item>
    /// <item>
    /// <description><c>w</c> = block size in bytes</description>
    /// </item>
    /// <item>
    /// <description><c>f</c> = number of finalization rounds</description>
    /// </item>
    /// <item>
    /// <description><c>h</c> = hash size in bits</description>
    /// </item>
    /// </list>
    /// <para>Example: <c>CubeHash16+32/32+32-256</c>.</para>
    /// </remarks>
    public string AlgorithmName
    {
        get
        {
            this.ThrowIfDisposed();
            return $"CubeHash{this.InitializationRounds}+{this.Rounds}/{this.TransformBlockSize}+{this.FinalizationRounds}-{this.HashSize}";
        }
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets or sets the number of finalization rounds applied after all input has been processed.
    /// </summary>
    /// <remarks>
    /// Finalization rounds provide additional mixing of the internal state to ensure that the final hash output is highly sensitive to
    /// every bit of input data. Increasing this value strengthens final-state diffusion.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than <see cref="MinRounds"/> or greater than <see cref="MaxRounds"/>.</exception>
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
            ThrowHelper.ThrowIfOutOfRange(value, MinRounds, MaxRounds);
            this._finalizationRounds = value;
            this._isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <remarks>
    /// The hash size determines the length of the digest returned by the algorithm. Valid values must be between
    /// <see cref="MinHashSize"/> and <see cref="MaxHashSize"/>, and divisible by 8. Larger sizes increase output strength.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is not within range <see cref="MinHashSize"/> to <see cref="MaxHashSize"/>, or is not a multiple of 8.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
    public new int HashSize
    {
        get
        {
            this.ThrowIfDisposed();
            return this.HashSizeValue;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinHashSize, MaxHashSize);
            ThrowHelper.ThrowIfNotPositiveMultipleOf(value, 8);
            this.HashSizeValue = value;
            this._isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the number of initialisation rounds to run before processing input data.
    /// </summary>
    /// <remarks>
    /// Initialisation rounds mix the initial state of the algorithm before the first input byte is processed. Increasing this value
    /// enhances initial diffusion but increases computation time.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than <see cref="MinRounds"/> or greater than <see cref="MaxRounds"/>.</exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
    public int InitializationRounds
    {
        get
        {
            this.ThrowIfDisposed();
            return this._initializationRounds;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinRounds, MaxRounds);
            this._initializationRounds = value;
            this._isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the number of transformation rounds applied to each full input block.
    /// </summary>
    /// <remarks>
    /// A higher number of rounds provides greater mixing of the state per block, which improves security at the cost of speed.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than <see cref="MinRounds"/> or greater than <see cref="MaxRounds"/>.</exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
    public int Rounds
    {
        get
        {
            this.ThrowIfDisposed();
            return this._rounds;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinRounds, MaxRounds);
            this._rounds = value;
            this._isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the size, in bytes, of the input block used by the CubeHash algorithm to determine when to perform a state transformation.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="HashAlgorithm.InputBlockSize"/>, which is advisory, this property directly affects the output of the hash
    /// function. When the number of accumulated input bytes reaches <c>TransformBlockSize</c>, a transformation round is triggered.
    /// Modifying this value changes the frequency of internal state updates, impacting both performance and security characteristics.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Value is not within range <see cref="MinInputBlockSize"/> to <see cref="MaxInputBlockSize"/>.</exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
    public int TransformBlockSize
    {
        get
        {
            this.ThrowIfDisposed();
            return this._inputBlockSizeBytes;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinInputBlockSize, MaxInputBlockSize);
            this._inputBlockSizeBytes = value;
            this._isInitializedStateCached = false;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        this.State = 0;
        this._finalized = false;
#endif
        this._pendingBytes = 0;

        this.EnsureInitialized();
        this.InitializeVectors();
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
        if (this._disposed) return;

        if (disposing)
        {
            if (this._state is not null)
            {
                CryptoHelpers.ClearAndNullify(ref this._state!);
                CryptoHelpers.ClearAndNullify(ref this._initializedState!);
                this._isInitializedStateCached = false;
            }

            this._finalizationRounds = 0;
            this._initializationRounds = 0;
            this._rounds = 0;
            this._inputBlockSizeBytes = 0;
            this._pendingBytes = 0;

            // CubeHash extends HashAlgorithm directly (not BufferedBlockHashAlgorithm),
            // so the centralised HashValue / HashSizeValue clearing in the latter does not apply here.
            CryptoHelpers.ClearAndNullify(ref this.HashValue);
            this.HashSizeValue = 0;
        }

        this._disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Processes a segment of the input byte array and feeds it into the <see cref="CubeHash"/> hashing algorithm. This method updates
    /// the internal state by processing <paramref name="cbSize"/> bytes starting at the specified <paramref name="ibStart"/> offset.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash.</param>
    /// <param name="ibStart">The zero-based index in <paramref name="array"/> at which to begin reading data.</param>
    /// <param name="cbSize">The number of bytes to process from <paramref name="array"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="ibStart"/> is less than 0.</para>
    /// <para>-or-</para>
    /// <para><paramref name="cbSize"/> is less than 0.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ibStart"/> and <paramref name="cbSize"/> specify a range that exceeds the length of <paramref name="array"/>.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        ThrowHelper.ThrowIfLessThan(ibStart, 0);
        ThrowHelper.ThrowIfLessThan(cbSize, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);
        if (this._finalized)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_AlreadyFinalized);
#endif
        this.EnsureInitialized();
        this.HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Processes the entirety of the input <paramref name="source"/> and feeds it into the <see cref="CubeHash"/> hashing algorithm.
    /// This method updates the internal hash state accordingly by consuming the entire input span.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();
        this.EnsureInitialized();

        var blockSize = this._inputBlockSizeBytes;

        // Complete any in-flight partial block first
        if (this._pendingBytes > 0)
        {
            var needed = blockSize - this._pendingBytes;

            if (source.Length < needed)
            {
                // Not enough data to complete the block — buffer and return
                this.XorBytesIntoState(source, this._pendingBytes);
                this._pendingBytes += source.Length;
                return;
            }

            this.XorBytesIntoState(source[..needed], this._pendingBytes);
            source = source[needed..];
            this._pendingBytes = 0;
            this.PerformRounds(this._rounds);
        }

        // Process full blocks directly
        while (source.Length >= blockSize)
        {
            this.XorBlockIntoState(source[..blockSize]);
            this.PerformRounds(this._rounds);
            source = source[blockSize..];
        }

        // Buffer any remaining partial block
        if (source.Length > 0)
        {
            this.XorBytesIntoState(source, 0);
            this._pendingBytes = source.Length;
        }
    }

    /// <summary>
    /// Finalises the hash computation and returns the computed digest in little-endian byte order.
    /// </summary>
    /// <returns>
    /// A byte array containing the computed hash value. Its length is <see cref="HashAlgorithm.HashSize"/> divided by 8.
    /// </returns>
    /// <exception cref="CryptographicUnexpectedOperationException">Thrown when the hash algorithm has been disposed or has produced an unexpected finalisation state.</exception>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
        if (this._finalized)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_AlreadyFinalized);
        this._finalized = true;
        this.State = 2;
#endif
        this.EnsureInitialized();

        // Append the 0x80 padding byte at the current pending-byte position within the state
        this._state[this._pendingBytes / 4] ^= 0x80u << (8 * (this._pendingBytes % 4));
        this.PerformRounds(this._rounds);

        // Set the finalization flag and apply finalization rounds
        this._state[31] ^= 1U;
        this.PerformRounds(this._finalizationRounds);

        var byteLength = this.HashSize / 8;
        var result = GC.AllocateUninitializedArray<byte>(byteLength);
        for (var i = 0; i < byteLength; i++)
            result[i] = (byte)(this._state[i / 4] >> (8 * (i % 4)));

        return result;
    }

    /// <summary>
    /// Ensures the initial state is computed and cached. Reinitialises the existing state array in-place to avoid allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureInitialized()
    {
        if (this._isInitializedStateCached)
            return;

        // Zero and seed the state with algorithm parameters, then apply initialization rounds
        Array.Clear(this._state, 0, this._state.Length);
        this._state[0] = (uint)(this.HashSizeValue / 8);
        this._state[1] = (uint)this._inputBlockSizeBytes;
        this._state[2] = (uint)this._rounds;
        this.PerformRounds(this._initializationRounds);

        // Cache the post-initialization state for fast resets
        this._state.CopyTo(this._initializedState, 0);
        this._isInitializedStateCached = true;
    }

    /// <summary>
    /// Resets the working state to the cached post-initialisation snapshot.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeVectors() => this._initializedState.CopyTo(this._state, 0);

    /// <summary>
    /// Executes the specified number of CubeHash transformation rounds on the state vector.
    /// </summary>
    /// <param name="roundCount">The number of rounds to perform.</param>
    private void PerformRounds(int roundCount)
    {
        Span<uint> s = this._state;
        Span<uint> lower = s[..16];
        Span<uint> upper = s.Slice(16, 16);

        // temp is used as a scratch permutation buffer; allocated once on the stack for the full call
        Span<uint> temp = stackalloc uint[16];

        for (var r = 0; r < roundCount; r++)
        {
            // Steps 1+2: add lower into upper; scatter lower into temp via XOR-8 permutation
            for (var i = 0; i < 16; i++)
            {
                upper[i] += lower[i];
                temp[i ^ 8] = lower[i];
            }

            // Steps 3+4: rotate temp left by 7 into lower; XOR lower with upper
            for (var i = 0; i < 16; i++)
                lower[i] = temp[i].RotateBitsLeftUnchecked(7) ^ upper[i];

            // Step 5: scatter upper into temp via XOR-2 permutation; copy back to upper
            for (var i = 0; i < 16; i++)
                temp[i ^ 2] = upper[i];
            temp.CopyTo(upper);

            // Steps 6+7: add lower into upper; scatter lower into temp via XOR-4 permutation
            for (var i = 0; i < 16; i++)
            {
                upper[i] += lower[i];
                temp[i ^ 4] = lower[i];
            }

            // Steps 8+9: rotate temp left by 11 into lower; XOR lower with upper
            for (var i = 0; i < 16; i++)
                lower[i] = temp[i].RotateBitsLeftUnchecked(11) ^ upper[i];

            // Step 10: scatter upper into temp via XOR-1 permutation; copy back to upper
            for (var i = 0; i < 16; i++)
                temp[i ^ 1] = upper[i];
            temp.CopyTo(upper);
        }
    }

    /// <summary>
    /// XORs a full input block into the state starting at word zero. Uses a direct reinterpretation cast on
    /// little-endian platforms with word-aligned block sizes; falls back to byte-by-byte XOR otherwise.
    /// </summary>
    /// <param name="block">The input block to XOR into the state. Must have length equal to <see cref="TransformBlockSize"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void XorBlockIntoState(ReadOnlySpan<byte> block)
    {
        // Fast path: little-endian platform with a word-aligned block size avoids per-byte arithmetic entirely
        if (BitConverter.IsLittleEndian && (block.Length & 3) == 0)
        {
            ReadOnlySpan<uint> words = MemoryMarshal.Cast<byte, uint>(block);
            Span<uint> stateSpan = this._state;
            for (var i = 0; i < words.Length; i++)
                stateSpan[i] ^= words[i];

            return;
        }

        // General path: handles big-endian platforms or non-word-aligned block sizes
        this.XorBytesIntoState(block, 0);
    }

    /// <summary>
    /// XORs bytes from <paramref name="source"/> into the state, treating the first byte of <paramref name="source"/> as
    /// residing at <paramref name="stateByteOffset"/> bytes into the current block.
    /// </summary>
    /// <param name="source">The bytes to XOR into the state.</param>
    /// <param name="stateByteOffset">The byte offset within the current block at which to begin writing.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void XorBytesIntoState(ReadOnlySpan<byte> source, int stateByteOffset)
    {
        for (var i = 0; i < source.Length; i++)
        {
            var pos = stateByteOffset + i;
            this._state[pos >> 2] ^= (uint)source[i] << (8 * (pos & 3));
        }
    }

    /// <summary>
    /// Throws an exception if the object has already been disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(nameof(CubeHash));
#endif
    }

    /// <summary>
    /// Throws an exception if algorithm configuration is attempted after state mutation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }
}
