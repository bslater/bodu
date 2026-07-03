// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>CubeHash</c> permutation-based hash algorithm designed by Daniel J. Bernstein and
/// submitted to the NIST SHA-3 competition. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CubeHash" /> operates on a 1024-bit internal state updated through a sequence of ARX (Addition, Rotation,
/// XOR) operations. The number of initialization, transformation, and finalization rounds, the hash output size, and
/// the input block size are all configurable. See <a href="https://en.wikipedia.org/wiki/CubeHash">Wikipedia</a> for an
/// overview.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>State size: 1024 bits (32 × 32-bit words).</description>
/// </item>
/// <item>
/// <description>
/// Output size: 224, 256, 384, or 512 bits (default 512); pass the desired size to <see cref="CubeHash(int)" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// Input block size: configurable, <see cref="MinInputBlockSize" />–<see cref="MaxInputBlockSize" /> bytes (default
/// 32).
/// </description>
/// </item>
/// <item>
/// <description>
/// Rounds: initialization, per-block, and finalization counts each independently configurable up to
/// <see cref="MaxRounds" />; defaults are 16 / 16 / 32.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose CubeHash.</strong> Pick CubeHash for academic study, cryptographic competition
/// reproducibility, or interop with code that has settled on a specific CubeHash parameterization. The defaults
/// (CubeHash 16/32/+160/512) match the SHA-3 competition submission. For new general-purpose cryptographic hashing
/// prefer SHA-2, SHA-3, or <see cref="Blake2b" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// // Default parameters: 512-bit output, 32-byte input block, 16/16/32 rounds.
/// using var cube = new CubeHash();
/// byte[] digest = cube.ComputeHash(message);
///]]>
/// </code>
/// </example>
public sealed class CubeHash
    : System.Security.Cryptography.HashAlgorithm
{
    /// <summary>The maximum allowable size of the computed hash, in bits.</summary>
    public const int MaxHashSize = 512;

    /// <summary>The maximum allowable size of the input block, in bytes.</summary>
    public const int MaxInputBlockSize = 128;

    /// <summary>The maximum number of rounds permitted for initialization, processing, or finalization.</summary>
    public const int MaxRounds = 4096;

    /// <summary>The minimum allowable size of the computed hash, in bits.</summary>
    public const int MinHashSize = 224;

    /// <summary>The minimum allowable size of the input block, in bytes.</summary>
    public const int MinInputBlockSize = 1;

    /// <summary>The minimum number of rounds permitted for initialization, processing, or finalization.</summary>
    public const int MinRounds = 1;

    /// <summary>The set of hash output sizes, in bits, accepted by the algorithm.</summary>
    private static readonly int[] s_permittedHashSizes = [224, 256, 384, 512];

    /// <summary>Gather-index vector for the scatter-by-XOR-8 step: element j receives the value from position j^8, swapping the two 256-bit halves of the 512-bit state register.</summary>
    private static readonly Vector512<uint> s_permXor8 = Vector512.Create(8u, 9u, 10u, 11u, 12u, 13u, 14u, 15u, 0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u);

    /// <summary>Gather-index vector for the scatter-by-XOR-4 step: element j receives the value from position j^4.</summary>
    private static readonly Vector512<uint> s_permXor4 = Vector512.Create(4u, 5u, 6u, 7u, 0u, 1u, 2u, 3u, 12u, 13u, 14u, 15u, 8u, 9u, 10u, 11u);

    /// <summary>Gather-index vector for the scatter-by-XOR-2 step: element j receives the value from position j^2.</summary>
    private static readonly Vector512<uint> s_permXor2 = Vector512.Create(2u, 3u, 0u, 1u, 6u, 7u, 4u, 5u, 10u, 11u, 8u, 9u, 14u, 15u, 12u, 13u);

    /// <summary>Gather-index vector for the scatter-by-XOR-1 step: element j receives the value from position j^1.</summary>
    private static readonly Vector512<uint> s_permXor1 = Vector512.Create(1u, 0u, 3u, 2u, 5u, 4u, 7u, 6u, 9u, 8u, 11u, 10u, 13u, 12u, 15u, 14u);

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed = false;

    /// <summary>The number of finalization rounds applied after all input has been processed.</summary>
    private int _finalizationRounds;

    /// <summary>The number of initialization rounds applied before input is processed.</summary>
    private int _initializationRounds;

    /// <summary>The cached post-initialization state snapshot used to reset the working state without recomputation.</summary>
    private uint[] _initializedState;

    /// <summary>The size, in bytes, of the input block that triggers a state transformation.</summary>
    private int _inputBlockSizeBytes;

    /// <summary>Indicates whether the post-initialization state snapshot has been computed and cached.</summary>
    private bool _isInitializedStateCached = false;

    /// <summary>The number of bytes accumulated in the current partial block.</summary>
    private int _pendingBytes;

    /// <summary>The number of transformation rounds applied to each full input block.</summary>
    private int _rounds;

    /// <summary>The 32-word (1024-bit) internal state updated in place across permutation rounds.</summary>
    private uint[] _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="CubeHash" /> class with default parameters: 512-bit output, 32-byte
    /// input block, and 16 / 16 / 32 initialization / transform / finalization rounds.
    /// </summary>
    public CubeHash()
    {
        _state = new uint[32];
        _initializedState = new uint[32];
        HashSizeValue = 512;
        _inputBlockSizeBytes = 32;
        _rounds = 16;
        _initializationRounds = 16;
        _finalizationRounds = 32;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CubeHash" /> class with the specified hash output size and default
    /// algorithm parameters: 32-byte input block and 16 / 16 / 32 initialization / transform / finalization rounds.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of: 224, 256, 384, or 512.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not a permitted hash size.
    /// </exception>
    public CubeHash(int hashSize)
    {
        CryptographyThrowHelper.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);
        _state = new uint[32];
        _initializedState = new uint[32];
        HashSizeValue = hashSize;
        _inputBlockSizeBytes = 32;
        _rounds = 16;
        _initializationRounds = 16;
        _finalizationRounds = 32;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CubeHash" /> class with fully specified algorithm parameters.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of: 224, 256, 384, or 512.</param>
    /// <param name="rounds">
    /// The number of transformation rounds applied to each full input block. Must be between <see cref="MinRounds" />
    /// and <see cref="MaxRounds" /> inclusive.
    /// </param>
    /// <param name="transformBlockSize">
    /// The size, in bytes, of the input block used to trigger a state transformation. Must be between
    /// <see cref="MinInputBlockSize" /> and <see cref="MaxInputBlockSize" /> inclusive.
    /// </param>
    /// <param name="finalizationRounds">
    /// The number of finalization rounds applied after all input has been processed. Must be between
    /// <see cref="MinRounds" /> and <see cref="MaxRounds" /> inclusive.
    /// </param>
    /// <param name="initializationRounds">
    /// The number of initialization rounds to run before processing input data. Must be between
    /// <see cref="MinRounds" /> and <see cref="MaxRounds" /> inclusive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para>
    /// <paramref name="initializationRounds" />, <paramref name="rounds" />, or <paramref name="finalizationRounds" />
    /// is less than <see cref="MinRounds" /> or greater than <see cref="MaxRounds" />.
    /// </para>
    /// <para>
    /// -or-
    /// </para>
    /// <para>
    /// <paramref name="transformBlockSize" /> is less than <see cref="MinInputBlockSize" /> or greater than
    /// <see cref="MaxInputBlockSize" />.
    /// </para>
    /// <para>
    /// -or-
    /// </para>
    /// <para>
    /// <paramref name="hashSize" /> is not a permitted hash size.
    /// </para>
    /// </exception>
    public CubeHash(int initializationRounds, int rounds, int transformBlockSize, int finalizationRounds, int hashSize)
    {
        ThrowHelper.ThrowIfOutOfRange(initializationRounds, MinRounds, MaxRounds);
        ThrowHelper.ThrowIfOutOfRange(rounds, MinRounds, MaxRounds);
        ThrowHelper.ThrowIfOutOfRange(transformBlockSize, MinInputBlockSize, MaxInputBlockSize);
        ThrowHelper.ThrowIfOutOfRange(finalizationRounds, MinRounds, MaxRounds);
        CryptographyThrowHelper.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);
        _state = new uint[32];
        _initializedState = new uint[32];
        HashSizeValue = hashSize;
        _inputBlockSizeBytes = transformBlockSize;
        _rounds = rounds;
        _initializationRounds = initializationRounds;
        _finalizationRounds = finalizationRounds;
    }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the variant and hash output size.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Follows the <see cref="CubeHash" /> naming convention from the original submission: <c>CubeHashr+b/w+f-h</c>,
    /// where:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><c>r</c> = number of initialization rounds</description>
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
    /// <para>
    /// Example: <c>CubeHash16+32/32+32-256</c>.
    /// </para>
    /// </remarks>
    public string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return $"CubeHash{InitializationRounds}+{Rounds}/{TransformBlockSize}+{FinalizationRounds}-{HashSize}";
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
    /// Finalization rounds provide additional mixing of the internal state to ensure that the final hash output is
    /// highly sensitive to every bit of input data. Increasing this value strengthens final-state diffusion.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is less than <see cref="MinRounds" /> or greater than <see cref="MaxRounds" />.
    /// </exception>
    public int FinalizationRounds
    {
        get
        {
            ThrowIfDisposed();
            return _finalizationRounds;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinRounds, MaxRounds);
            _finalizationRounds = value;
            _isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <remarks>
    /// The hash size determines the length of the digest returned by the algorithm. Valid values are 224, 256, 384, and
    /// 512 bits.
    /// </remarks>
    /// <value>The hash output size in bits.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is not one of the permitted sizes (224, 256, 384, 512).
    /// </exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started.
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
            _isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the number of initialization rounds to run before processing input data.
    /// </summary>
    /// <remarks>
    /// Initialization rounds mix the initial state of the algorithm before the first input byte is processed.
    /// Increasing this value enhances initial diffusion but increases computation time.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is less than <see cref="MinRounds" /> or greater than <see cref="MaxRounds" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started.
    /// </exception>
    public int InitializationRounds
    {
        get
        {
            ThrowIfDisposed();
            return _initializationRounds;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinRounds, MaxRounds);
            _initializationRounds = value;
            _isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the number of transformation rounds applied to each full input block.
    /// </summary>
    /// <remarks>
    /// A higher number of rounds provides greater mixing of the state per block, which improves security at the cost of
    /// speed.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is less than <see cref="MinRounds" /> or greater than <see cref="MaxRounds" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started.
    /// </exception>
    public int Rounds
    {
        get
        {
            ThrowIfDisposed();
            return _rounds;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinRounds, MaxRounds);
            _rounds = value;
            _isInitializedStateCached = false;
        }
    }

    /// <summary>
    /// Gets or sets the size, in bytes, of the input block used by the CubeHash algorithm to determine when to perform
    /// a state transformation.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="HashAlgorithm.InputBlockSize" />, which is advisory, this property directly affects the output
    /// of the hash function. When the number of accumulated input bytes reaches <c>TransformBlockSize</c>, a
    /// transformation round is triggered. Modifying this value changes the frequency of internal state updates,
    /// impacting both performance and security characteristics.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is not within range <see cref="MinInputBlockSize" /> to <see cref="MaxInputBlockSize" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash computation has already started.
    /// </exception>
    public int TransformBlockSize
    {
        get
        {
            ThrowIfDisposed();
            return _inputBlockSizeBytes;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfOutOfRange(value, MinInputBlockSize, MaxInputBlockSize);
            _inputBlockSizeBytes = value;
            _isInitializedStateCached = false;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        ThrowIfDisposed();
        _pendingBytes = 0;

        EnsureInitialized();
        InitializeVectors();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    /// <remarks>
    /// Ensures all internal secrets are overwritten with zeros before releasing resources.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            if (_state is not null)
            {
                CryptographyHelper.ClearAndNullify(ref _state!);
                CryptographyHelper.ClearAndNullify(ref _initializedState!);
                _isInitializedStateCached = false;
            }

            _finalizationRounds = 0;
            _initializationRounds = 0;
            _rounds = 0;
            _inputBlockSizeBytes = 0;
            _pendingBytes = 0;

            // CubeHash extends HashAlgorithm directly (not BufferedBlockHashAlgorithm),
            // so the centralized HashValue / HashSizeValue clearing in the latter does not apply here.
            CryptographyHelper.ClearAndNullify(ref HashValue);
            HashSizeValue = 0;
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Processes a segment of the input byte array and feeds it into the <see cref="CubeHash" /> hashing algorithm.
    /// This method updates the internal state by processing <paramref name="cbSize" /> bytes starting at the specified
    /// <paramref name="ibStart" /> offset.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash.</param>
    /// <param name="ibStart">The zero-based index in <paramref name="array" /> at which to begin reading data.</param>
    /// <param name="cbSize">The number of bytes to process from <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para>
    /// <paramref name="ibStart" /> is less than 0.
    /// </para>
    /// <para>
    /// -or-
    /// </para>
    /// <para>
    /// <paramref name="cbSize" /> is less than 0.
    /// </para>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ibStart" /> and <paramref name="cbSize" /> specify a range that exceeds the length of
    /// <paramref name="array" />.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowIfDisposed();
        EnsureInitialized();
        HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="CubeHash" />
    /// hashing algorithm. This method updates the internal hash state accordingly by consuming the entire input span.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();
        EnsureInitialized();

        int blockSize = _inputBlockSizeBytes;

        // Complete any in-flight partial block first
        if (_pendingBytes > 0)
        {
            int needed = blockSize - _pendingBytes;

            if (source.Length < needed)
            {
                // Not enough data to complete the block — buffer and return
                XorBytesIntoState(source, _pendingBytes);
                _pendingBytes += source.Length;
                return;
            }

            XorBytesIntoState(source[..needed], _pendingBytes);
            source = source[needed..];
            _pendingBytes = 0;
            PerformRounds(_rounds);
        }

        // Process full blocks directly
        while (source.Length >= blockSize)
        {
            XorBlockIntoState(source[..blockSize]);
            PerformRounds(_rounds);
            source = source[blockSize..];
        }

        // Buffer any remaining partial block
        if (source.Length > 0)
        {
            XorBytesIntoState(source, 0);
            _pendingBytes = source.Length;
        }
    }

    /// <summary>
    /// Finalizes the hash computation and returns the computed digest in little-endian byte order.
    /// </summary>
    /// <returns>
    /// A byte array containing the computed hash value. Its length is <see cref="HashAlgorithm.HashSize" /> divided by
    /// 8.
    /// </returns>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when the hash algorithm has been disposed or has produced an unexpected finalization state.
    /// </exception>
    protected override byte[] HashFinal()
    {
        ThrowIfDisposed();
        EnsureInitialized();

        // Append the 0x80 padding byte at the current pending-byte position within the state
        _state[_pendingBytes / 4] ^= 0x80u << (8 * (_pendingBytes % 4));
        PerformRounds(_rounds);

        // Set the finalization flag and apply finalization rounds
        _state[31] ^= 1U;
        PerformRounds(_finalizationRounds);

        int byteLength = HashSize / 8;
        byte[] result = GC.AllocateUninitializedArray<byte>(byteLength);
        for (int i = 0; i < byteLength; i++)
            result[i] = (byte)(_state[i / 4] >> (8 * (i % 4)));

        return result;
    }

    /// <summary>
    /// Ensures the initial state is computed and cached. Reinitializes the existing state array in-place to avoid
    /// allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureInitialized()
    {
        if (_isInitializedStateCached)
            return;

        // Zero and seed the state with algorithm parameters, then apply initialization rounds
        CryptographyHelper.Clear(_state);
        _state[0] = (uint)(HashSizeValue / 8);
        _state[1] = (uint)_inputBlockSizeBytes;
        _state[2] = (uint)_rounds;
        PerformRounds(_initializationRounds);

        // Cache the post-initialization state for fast resets
        _state.CopyTo(_initializedState, 0);
        _isInitializedStateCached = true;
    }

    /// <summary>
    /// Resets the working state to the cached post-initialization snapshot.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeVectors() => _initializedState.CopyTo(_state, 0);

    /// <summary>
    /// Executes the specified number of CubeHash transformation rounds on the internal state vector, dispatching to an
    /// AVX-512F implementation when the hardware supports it and falling back to a scalar implementation otherwise.
    /// </summary>
    /// <param name="roundCount">The number of rounds to perform.</param>
    private void PerformRounds(int roundCount)
    {
        if (SimdCapabilities.Avx512F)
            PerformRoundsAvx512(roundCount);
        else
            PerformRoundsScalar(roundCount);
    }

    /// <summary>
    /// Executes the specified number of CubeHash transformation rounds using AVX-512F vector instructions. Loads the
    /// 32-word state into two 512-bit registers at entry and stores back on exit, keeping all round-to-round state in
    /// registers. Each scatter-by-XOR permutation becomes a single <c>VPERMD</c> instruction and each rotation becomes
    /// a single <c>VPROLD</c>.
    /// </summary>
    /// <param name="roundCount">The number of rounds to perform.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void PerformRoundsAvx512(int roundCount)
    {
        ref uint stateRef = ref MemoryMarshal.GetArrayDataReference(_state);

        var lower = Vector512.LoadUnsafe(ref stateRef);
        var upper = Vector512.LoadUnsafe(ref Unsafe.Add(ref stateRef, 16));

        for (int r = 0; r < roundCount; r++)
        {
            // Steps 1+2: upper += lower; scatter lower into temp via XOR-8 permutation (VPADDD + VPERMD)
            upper += lower;
            Vector512<uint> temp = Avx512F.PermuteVar16x32(lower, s_permXor8);

            // Steps 3+4: rotate temp left by 7 into lower; XOR lower with upper (VPROLD + VPXORD)
            lower = Avx512F.RotateLeft(temp, 7) ^ upper;

            // Step 5: scatter upper via XOR-2 permutation (VPERMD); upper = result
            upper = Avx512F.PermuteVar16x32(upper, s_permXor2);

            // Steps 6+7: upper += lower; scatter lower into temp via XOR-4 permutation (VPADDD + VPERMD)
            upper += lower;
            temp = Avx512F.PermuteVar16x32(lower, s_permXor4);

            // Steps 8+9: rotate temp left by 11 into lower; XOR lower with upper (VPROLD + VPXORD)
            lower = Avx512F.RotateLeft(temp, 11) ^ upper;

            // Step 10: scatter upper via XOR-1 permutation (VPERMD); upper = result
            upper = Avx512F.PermuteVar16x32(upper, s_permXor1);
        }

        Vector512.StoreUnsafe(lower, ref stateRef);
        Vector512.StoreUnsafe(upper, ref Unsafe.Add(ref stateRef, 16));
    }

    /// <summary>
    /// Executes the specified number of CubeHash transformation rounds using scalar interior-ref arithmetic with
    /// bounds-check-free accesses. Used as the fallback when AVX-512F is unavailable.
    /// </summary>
    /// <param name="roundCount">The number of rounds to perform.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void PerformRoundsScalar(int roundCount)
    {
        Span<uint> s = _state;

        // temp is used as a scratch permutation buffer; allocated once on the stack for the full call
        Span<uint> temp = stackalloc uint[16];

        // Pre-offset refs for lower and upper halves mirror the original Span.Slice approach so the JIT
        // sees a constant base per half rather than recomputing (16 + i) on every upper access.
        // tempRef bypasses the per-element bounds checks for the non-monotonic XOR scatter indices
        // (i^8, i^2, i^4, i^1) that the JIT cannot prove stay in-range from a loop-bound alone.
        ref uint lowerRef = ref MemoryMarshal.GetReference(s);
        ref uint upperRef = ref Unsafe.Add(ref lowerRef, 16);
        ref uint tempRef = ref MemoryMarshal.GetReference(temp);

        for (int r = 0; r < roundCount; r++)
        {
            // Steps 1+2: add lower into upper; scatter lower into temp via XOR-8 permutation
            for (int i = 0; i < 16; i++)
            {
                Unsafe.Add(ref upperRef, i) += Unsafe.Add(ref lowerRef, i);
                Unsafe.Add(ref tempRef, i ^ 8) = Unsafe.Add(ref lowerRef, i);
            }

            // Steps 3+4: rotate temp left by 7 into lower; XOR lower with upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref lowerRef, i) = Unsafe.Add(ref tempRef, i).RotateBitsLeftUnchecked(7) ^ Unsafe.Add(ref upperRef, i);

            // Step 5: scatter upper into temp via XOR-2 permutation; copy back to upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref tempRef, i ^ 2) = Unsafe.Add(ref upperRef, i);
            temp.CopyTo(s.Slice(16, 16));

            // Steps 6+7: add lower into upper; scatter lower into temp via XOR-4 permutation
            for (int i = 0; i < 16; i++)
            {
                Unsafe.Add(ref upperRef, i) += Unsafe.Add(ref lowerRef, i);
                Unsafe.Add(ref tempRef, i ^ 4) = Unsafe.Add(ref lowerRef, i);
            }

            // Steps 8+9: rotate temp left by 11 into lower; XOR lower with upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref lowerRef, i) = Unsafe.Add(ref tempRef, i).RotateBitsLeftUnchecked(11) ^ Unsafe.Add(ref upperRef, i);

            // Step 10: scatter upper into temp via XOR-1 permutation; copy back to upper
            for (int i = 0; i < 16; i++)
                Unsafe.Add(ref tempRef, i ^ 1) = Unsafe.Add(ref upperRef, i);
            temp.CopyTo(s.Slice(16, 16));
        }
    }

    /// <summary>
    /// XORs a full input block into the state starting at word zero. Uses a direct reinterpretation cast on
    /// little-endian platforms with word-aligned block sizes; falls back to byte-by-byte XOR otherwise.
    /// </summary>
    /// <param name="block">
    /// The input block to XOR into the state. Must have length equal to <see cref="TransformBlockSize" />.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void XorBlockIntoState(ReadOnlySpan<byte> block)
    {
        // Fast path: little-endian platform with a word-aligned block size avoids per-byte arithmetic entirely
        if (BitConverter.IsLittleEndian && (block.Length & 3) == 0)
        {
            ReadOnlySpan<uint> words = MemoryMarshal.Cast<byte, uint>(block);
            Span<uint> stateSpan = _state;
            for (int i = 0; i < words.Length; i++)
                stateSpan[i] ^= words[i];

            return;
        }

        // General path: handles big-endian platforms or non-word-aligned block sizes
        XorBytesIntoState(block, 0);
    }

    /// <summary>
    /// XORs bytes from <paramref name="source" /> into the state, treating the first byte of <paramref name="source" />
    /// as residing at <paramref name="stateByteOffset" /> bytes into the current block.
    /// </summary>
    /// <param name="source">The bytes to XOR into the state.</param>
    /// <param name="stateByteOffset">The byte offset within the current block at which to begin writing.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void XorBytesIntoState(ReadOnlySpan<byte> source, int stateByteOffset)
    {
        for (int i = 0; i < source.Length; i++)
        {
            int pos = stateByteOffset + i;
            _state[pos >> 2] ^= (uint)source[i] << (8 * (pos & 3));
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Throws an exception if algorithm configuration is attempted after state mutation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (State != 0)
            throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.Crypt_Invalid_ReconfigurationNotAllowed);
    }
}
