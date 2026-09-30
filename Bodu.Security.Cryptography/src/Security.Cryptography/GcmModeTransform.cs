// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Galois/Counter Mode (GCM) to a 128-bit block cipher, providing single-pass authenticated encryption with
/// associated data (AEAD) per NIST SP 800-38D.
/// </summary>
/// <remarks>
/// <para>
/// GCM combines counter-mode encryption with GHASH authentication over <c>GF(2¹²⁸)</c>:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Hash subkey: <c>H = E_K(0¹²⁸)</c>.</description>
/// </item>
/// <item>
/// <description>Initial counter <c>J0 = nonce ‖ 0x00000001</c>; payload counter starts at <c>J0 + 1</c>.</description>
/// </item>
/// <item>
/// <description>Ciphertext: <c>C_i = P_i ⊕ E_K(counter_i)</c>, counter incremented per block.</description>
/// </item>
/// <item>
/// <description>Tag: <c>T = GHASH_H(AAD ‖ C ‖ len(AAD)‖len(C)) ⊕ E_K(J0)</c>.</description>
/// </item>
/// </list>
/// <para>
/// GF(2¹²⁸) multiplication uses the irreducible polynomial <c>x¹²⁸ + x⁷ + x² + x + 1</c> with big-endian bit ordering
/// and the reduction constant <c>0xE1</c> in the most-significant byte.
/// </para>
/// <para>
/// <strong>Nonce length.</strong> This implementation accepts only the 96-bit (12-byte) nonce form, which is what every
/// interoperable GCM consumer uses (TLS 1.2/1.3, IPsec ESP, SSH, QUIC). The SP 800-38D §7.1 GHASH-based derivation for
/// other nonce lengths is intentionally not supported.
/// </para>
/// <para>
/// <strong>Lifecycle.</strong> Each instance encrypts or decrypts exactly one message. A second call to
/// <see cref="Encrypt" /> or <see cref="Decrypt" /> throws <see cref="InvalidOperationException" />. The instance must
/// be disposed when finished; <see cref="Dispose" /> clears the GHASH subkey, initial counter, running counter, and
/// cached associated data. The supplied <see cref="IBlockCipher" /> is not disposed by this type - ownership remains
/// with the caller.
/// </para>
/// <para>
/// <strong>Length limits.</strong> SP 800-38D §5.2.1.1 caps the plaintext at <c>2³⁹ − 256</c> bits and the associated
/// data at <c>2⁶⁴</c> bits per <c>(key, nonce)</c> pair. <see cref="Encrypt" /> / <see cref="Decrypt" /> /
/// <see cref="ProcessAssociatedData" /> all defensively call <c>ValidatePlaintextLength</c> or <c>ValidateAadLength</c>
/// , which throw <see cref="CryptographicException" /> when the input length would breach the spec ceiling. Both checks
/// are dead code through the public <see cref="ReadOnlySpan{Byte}" /> surface today (the <see cref="int" />-typed
/// length caps inputs at ≈ 2 GiB, far below either limit) but document the invariant at the call site and protect any
/// future surface that admits longer inputs. The 32-bit counter is separately guarded against wrapping past
/// <c>0xFFFFFFFF</c> - see <c>Encrypt_WhenCounterWouldWrapPast0xFFFFFFFF</c>.
/// </para>
/// <para>
/// <strong>When to use GCM.</strong> The default modern AEAD mode - single-pass, parallelisable, and
/// hardware-accelerated on AES-NI / PCLMULQDQ. The cost is fragility under nonce reuse: a single repeated
/// <c>(key, nonce)</c> pair leaks the GHASH subkey and forfeits authentication forever. For nonce-misuse resistance
/// prefer <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />; for constrained environments prefer
/// <see cref="CcmModeTransform" />; for a single-pass alternative without GCM's failure profile prefer
/// <see cref="OcbModeTransform" />.
/// </para>
/// <para>
/// <strong>When to use GCM.</strong> The default modern AEAD mode - TLS 1.2/1.3, IPsec ESP, SSH, QUIC, and most
/// file-format AEAD layers all use AES-GCM. Single-pass, parallelisable, hardware-accelerated on AES-NI / PCLMULQDQ,
/// and the fastest AEAD on commodity x86/ARM. The cost is fragility under nonce reuse: a single repeated
/// <c>(key, nonce)</c> pair leaks the GHASH key and forfeits authentication forever. Use only when the caller can <em>
/// guarantee</em> nonce uniqueness - usually via a 96-bit counter or a random nonce drawn from a large enough space.
/// For nonce-misuse resistance prefer <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />; for
/// constrained environments prefer <see cref="CcmModeTransform" />; for a single-pass alternative without GCM's
/// fragility profile prefer <see cref="OcbModeTransform" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// // GCM takes the 96-bit (12-byte) nonce directly - J0 is derived internally as nonce || 0x00000001.
/// using IAeadBlockCipherModeTransform gcm = new GcmModeTransform(cipher, nonce);
/// byte[] sealed_ = gcm.Encrypt(plaintext, associatedData: header);
/// using IAeadBlockCipherModeTransform dec = new GcmModeTransform(cipher, nonce);
/// byte[] recovered = dec.Decrypt(sealed_, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#gcm--the-workhorse">GCM walk-through in the AEAD-modes guide
/// </seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class GcmModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>SP 800-38D §5.2.1.1 associated-data length ceiling: <c>2⁶⁴</c> bits, expressed in bytes ( <c>2⁶⁴ / 8 = 2⁶¹ = 2 305 843 009 213 693 952</c>). Internal so tests can validate the constant.</summary>
    internal const long MaxAadBytes = 1L << 61;

    /// <summary>SP 800-38D §5.2.1.1 plaintext length ceiling: <c>2³⁹ − 256</c> bits, expressed in bytes ( <c>(2³⁹ − 256) / 8 = 68 719 476 704</c>). Internal so tests can validate the constant.</summary>
    internal const long MaxPlaintextBytes = ((1L << 39) - 256) / 8;

    /// <summary>The fixed GCM block size in bits (128 bits = 16 bytes).</summary>
    private const int BlockSize = 128;

    /// <summary>The GCM authentication tag size in bits (128 bits = 16 bytes).</summary>
    private const int DefaultTagSize = 128;

    /// <summary>The required GCM nonce size in bits (96 bits = 12 bytes).</summary>
    private const int NonceSize = 96;

    /// <summary>The 128-bit block cipher used by GCM. Not owned by this instance.</summary>
    private readonly IBlockCipher _cipher;

    /// <summary>The cached associated authenticated data, or <see langword="null" /> until it has been processed.</summary>
    private byte[]? _aad;

    /// <summary>Indicates whether the associated data has been processed.</summary>
    private bool _aadProcessed;

    /// <summary>Indicates whether this single-use transform has completed an encryption or decryption.</summary>
    private bool _completed;

    /// <summary>The running CTR counter block, incremented per processed block.</summary>
    private byte[]? _counter;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>The GHASH subkey <c>H = E_K(0¹²⁸)</c>, prepared for the process's GHASH kernel; reset to <see langword="default" /> on disposal.</summary>
    private Ghash.Key _hashKey;

    /// <summary>The initial counter block <c>J0</c>, reserved as the base for the authentication tag.</summary>
    private byte[]? _j0;

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmModeTransform" /> class with a 96-bit GCM nonce.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonce">The 96-bit (12-byte) nonce. Must be unique per key.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="nonce" /> is not exactly
    /// <see cref="NonceSize" /> bytes.
    /// </exception>
    public GcmModeTransform(IBlockCipher cipher, byte[] nonce)
        : this(
            cipher,
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : new ReadOnlySpan<byte>(nonce),
            nameof(nonce),
            useInitialCounterBlock: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmModeTransform" /> class with a 96-bit GCM nonce.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonce">The 96-bit (12-byte) nonce. Must be unique per key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="nonce" /> is not exactly
    /// <see cref="NonceSize" /> bytes.
    /// </exception>
    public GcmModeTransform(IBlockCipher cipher, ReadOnlySpan<byte> nonce)
        : this(cipher, nonce, nameof(nonce), useInitialCounterBlock: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmModeTransform" /> class with a typed 96-bit GCM nonce.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonce">The 96-bit (12-byte) nonce. Must be unique per key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="nonce" /> is not exactly
    /// <see cref="NonceSize" /> bytes.
    /// </exception>
    /// <remarks>
    /// Convenience overload over the span form for callers using the <see cref="Nonce" /> value type - typically
    /// produced by <see cref="Nonce.Random(int)" /> - which keeps nonces distinct from keys and tags in calling code.
    /// </remarks>
    public GcmModeTransform(IBlockCipher cipher, Nonce nonce)
        : this(cipher, nonce.AsSpan(), nameof(nonce), useInitialCounterBlock: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GcmModeTransform" /> class and derives J0 from a 12-byte nonce or
    /// uses a precomputed J0 directly.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonceOrJ0">
    /// Either a 12-byte nonce or a 16-byte precomputed J0, depending on <paramref name="useInitialCounterBlock" />.
    /// </param>
    /// <param name="parameterName">
    /// The name of the parameter from the calling overload, used in <see cref="ArgumentException" /> messages.
    /// </param>
    /// <param name="useInitialCounterBlock">
    /// When <see langword="true" />, treats <paramref name="nonceOrJ0" /> as a precomputed J0 block; otherwise as a
    /// 12-byte nonce.
    /// </param>
    private GcmModeTransform(
        IBlockCipher cipher,
        ReadOnlySpan<byte> nonceOrJ0,
        string parameterName,
        bool useInitialCounterBlock)
    {
        _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));

        CryptographyThrowHelper.ThrowIfBlockSizeNotEqualTo(cipher, BlockSize, "GCM", nameof(cipher));

        if (useInitialCounterBlock)
        {
            if (nonceOrJ0.Length != BlockSize / 8)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_InitialCounterBlockLength, BlockSize / 8),
                    parameterName);
            }
        }
        else
        {
            if (nonceOrJ0.Length != NonceSize / 8)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_GcmNonceLength, NonceSize / 8),
                    parameterName);
            }
        }

        _j0 = new byte[BlockSize / 8];
        _counter = new byte[BlockSize / 8];

        // H = E_K(0¹²⁸), prepared once for the GHASH kernel and then cleared.
        Span<byte> zeroBlock = stackalloc byte[BlockSize / 8];
        Span<byte> h = stackalloc byte[BlockSize / 8];
        try
        {
            _cipher.Encrypt(zeroBlock, h);
            _hashKey = Ghash.Key.ForGhash(h, Ghash.SelectKernel());
        }
        finally
        {
            CryptographyHelper.Clear(h);
        }

        // Build J0.
        if (useInitialCounterBlock)
        {
            nonceOrJ0.CopyTo(_j0);
        }
        else
        {
            // J0 = nonce || 0x00000001.
            nonceOrJ0.CopyTo(_j0);
            _j0[15] = 0x01;
        }

        // CTR counter starts at inc32(J0); J0 itself is reserved for the tag. The standard derivation
        // J0 = nonce || 0x00000001 cannot wrap here; only a test-supplied J0 ending in 0xFFFFFFFF could.
        _j0.CopyTo(_counter, 0);
        if (IncrementCounter32(_counter))
        {
            throw new ArgumentException(
                CryptoResourceStrings.Arg_Invalid_InitialCounterBlockReserved,
                parameterName);
        }
    }

    /// <inheritdoc />
    /// <value>Length of the GCM authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => DefaultTagSize;

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// The instance has already encrypted or decrypted a message.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The implicit plaintext length (<c><paramref name="ciphertextWithTag" />.Length − 16</c>) exceeds the SP 800-38D
    /// §5.2.1.1 ceiling. Unreachable through the public int-typed span surface today.
    /// </exception>
    /// <remarks>
    /// <strong>Authentication pattern: verify-before-release.</strong> The GHASH-derived tag is compared in constant
    /// time before the CTR decryption stream is applied to <paramref name="output" />; no plaintext byte is ever
    /// written when authentication fails. If the underlying cipher throws mid-transform, the plaintext region of
    /// <paramref name="output" /> is zeroed before the exception propagates. See
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for the library-wide failure contract.
    /// </remarks>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        if (ciphertextWithTag.Length < DefaultTagSize / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, DefaultTagSize / 8),
                nameof(ciphertextWithTag));
        }

        int plaintextLength = ciphertextWithTag.Length - (DefaultTagSize / 8);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, plaintextLength);

        ValidatePlaintextLength(plaintextLength);

        try
        {
            EnsureAssociatedDataProcessed();

            ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength, DefaultTagSize / 8);

            Span<byte> expectedTag = stackalloc byte[DefaultTagSize / 8];
            try
            {
                ComputeTag(_aad.AsSpan(), ciphertext, expectedTag);

                // Verify before producing plaintext. On failure, zero whatever the caller passed in
                // so a pre-filled output buffer cannot leak data the caller may have used to seed
                // the destination - defense-in-depth aligned with AsconAead128.Decrypt.
                if (!CryptographicOperations.FixedTimeEquals(expectedTag, receivedTag))
                {
                    CryptographyHelper.Clear(output[..plaintextLength]);
                    throw new CryptographicException(
                        CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
                }

                ApplyCtr(ciphertext, output[..plaintextLength]);

                return plaintextLength;
            }
            finally
            {
                CryptographyHelper.Clear(expectedTag);
            }
        }
        catch
        {
            // Zero the plaintext region on any failure - a tag mismatch or a fault from the underlying
            // cipher mid-transform - so partially written plaintext or keystream bytes never leak.
            CryptographyHelper.Clear(output[..plaintextLength]);
            throw;
        }
        finally
        {
            _completed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by this instance and clears the GHASH subkey, initial counter, running counter, and
    /// cached associated data from memory. Idempotent. Does not dispose the supplied <see cref="IBlockCipher" /> -
    /// ownership remains with the caller.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _hashKey = default;
        CryptographyHelper.ClearAndNullify(ref _j0);
        CryptographyHelper.ClearAndNullify(ref _counter);
        CryptographyHelper.ClearAndNullify(ref _aad);

        _completed = true;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// The instance has already encrypted or decrypted a message.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="plaintext" /> length exceeds the SP 800-38D §5.2.1.1 ceiling. Unreachable through the public
    /// int-typed span surface today.
    /// </exception>
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        int required = checked(plaintext.Length + (DefaultTagSize / 8));
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, required);

        ValidatePlaintextLength(plaintext.Length);

        try
        {
            EnsureAssociatedDataProcessed();

            Span<byte> ciphertext = output[..plaintext.Length];
            ApplyCtr(plaintext, ciphertext);

            Span<byte> tag = stackalloc byte[DefaultTagSize / 8];
            try
            {
                ComputeTag(_aad.AsSpan(), ciphertext, tag);
                tag.CopyTo(output.Slice(plaintext.Length, DefaultTagSize / 8));

                return required;
            }
            finally
            {
                CryptographyHelper.Clear(tag);
            }
        }
        finally
        {
            _completed = true;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Associated data has already been processed, or the instance has already completed encryption or decryption.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="associatedData" /> length exceeds the SP 800-38D §5.2.1.1 ceiling. Unreachable through the
    /// public int-typed span surface today.
    /// </exception>
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        if (_aadProcessed)
        {
            throw new InvalidOperationException(
                CryptoResourceStrings.Crypt_Invalid_AssociatedDataAlreadyProcessed);
        }

        ValidateAadLength(associatedData.Length);

        _aad = associatedData.IsEmpty ? [] : associatedData.ToArray();
        _aadProcessed = true;
    }

    /// <summary>
    /// Creates a <see cref="GcmModeTransform" /> from a precomputed 128-bit initial counter block. Test-only entry
    /// point exposed via <c>InternalsVisibleTo</c> to support test vectors that publish <c>J0</c> directly.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="initialCounterBlock">The precomputed 16-byte initial counter block, <c>J0</c>.</param>
    /// <returns>A new <see cref="GcmModeTransform" /> initialized with the supplied <c>J0</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="initialCounterBlock" /> is not
    /// exactly 16 bytes.
    /// </exception>
    internal static GcmModeTransform CreateForTesting(IBlockCipher cipher, ReadOnlySpan<byte> initialCounterBlock) =>
        new(cipher, initialCounterBlock, nameof(initialCounterBlock), useInitialCounterBlock: true);

    /// <summary>
    /// Validates that the supplied associated-data length does not exceed the SP 800-38D §5.2.1.1 per-(key,nonce) AAD
    /// ceiling of <c>2⁶⁴</c> bits. Internal so tests can drive the check with synthetic length values that the public
    /// int-typed span surface cannot construct.
    /// </summary>
    /// <param name="length">The candidate associated-data length in bytes.</param>
    /// <exception cref="CryptographicException">
    /// <paramref name="length" /> exceeds <see cref="MaxAadBytes" />.
    /// </exception>
    internal static void ValidateAadLength(long length)
    {
        if (length > MaxAadBytes)
        {
            throw new CryptographicException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_GcmAadLengthExceeded, length, MaxAadBytes));
        }
    }

    /// <summary>
    /// Validates that the supplied plaintext length does not exceed the SP 800-38D §5.2.1.1 per-(key,nonce) plaintext
    /// ceiling of <c>2³⁹ − 256</c> bits. Internal so tests can drive the check with synthetic length values that the
    /// public int-typed span surface cannot construct.
    /// </summary>
    /// <param name="length">The candidate plaintext length in bytes.</param>
    /// <exception cref="CryptographicException">
    /// <paramref name="length" /> exceeds <see cref="MaxPlaintextBytes" />.
    /// </exception>
    internal static void ValidatePlaintextLength(long length)
    {
        if (length > MaxPlaintextBytes)
        {
            throw new CryptographicException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_GcmPlaintextLengthExceeded, length, MaxPlaintextBytes));
        }
    }

    /// <summary>
    /// Increments the 32-bit big-endian counter in the last 4 bytes of <paramref name="counter" /> per NIST SP 800-38D
    /// <c>inc32</c>, and reports whether the increment wrapped past <c>0xFFFFFFFF</c>.
    /// </summary>
    /// <param name="counter">The 16-byte CTR block; its low 32 bits are incremented in place.</param>
    /// <returns>
    /// <see langword="true" /> if the increment wrapped from <c>0xFFFFFFFF</c> back to <c>0x00000000</c>; otherwise
    /// <see langword="false" />. Callers must reject wrap whenever the wrapped counter would be used to encrypt another
    /// block, because <c>nonce ‖ 0x00000001</c> is reserved as <c>J0</c> for the tag.
    /// </returns>
    private static bool IncrementCounter32(Span<byte> counter)
    {
        for (int i = counter.Length - 1; i >= counter.Length - 4; i--)
            if (++counter[i] != 0) return false;
        return true;
    }

    /// <summary>
    /// Applies CTR mode: encrypts successive <c>inc32</c> counter blocks a run at a time and XORs the keystream with
    /// the input. Rejects any message whose length would force the 32-bit counter to wrap into <c>J0</c>'s reserved
    /// value, because reusing <c>E_K(J0)</c> as keystream leaks the GHASH subkey.
    /// </summary>
    /// <param name="input">The input bytes to XOR with the CTR keystream.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    /// <exception cref="CryptographicException">
    /// The plaintext / ciphertext length would step the GCM counter past <c>0xFFFFFFFF</c> while another block remains
    /// to be processed (NIST SP 800-38D §5.2.1.1 - at most <c>2^32 − 2</c> blocks per <c>(key, nonce)</c>).
    /// </exception>
    /// <remarks>
    /// Each run of counters stops after the block that takes the counter to <c>0xFFFFFFFF</c>, so the blocks before a
    /// rejected wrap are written exactly as a block-at-a-time loop writes them before it throws.
    /// </remarks>
    [SkipLocalsInit]
    private void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output)
    {
        const int BlockBytes = BlockSize / 8;
        Span<byte> counters = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> keystream = stackalloc byte[CounterKeystream.BatchBytes];
        byte[] counter = _counter!;
        int used = 0;

        // The first twelve bytes never change; inc32 increments the last four as a big-endian integer.
        ulong fixedLow = BinaryPrimitives.ReadUInt64LittleEndian(counter);
        uint fixedHigh = BinaryPrimitives.ReadUInt32LittleEndian(counter.AsSpan(8));
        uint count = BinaryPrimitives.ReadUInt32BigEndian(counter.AsSpan(12));
        bool wrapped = false;

        try
        {
            int offset = 0;
            while (offset < input.Length)
            {
                int limit = Math.Min(counters.Length, input.Length - offset);
                int filled = 0;
                while (filled < limit && !wrapped)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(counters[filled..], fixedLow);
                    BinaryPrimitives.WriteUInt32LittleEndian(counters.Slice(filled + 8), fixedHigh);
                    BinaryPrimitives.WriteUInt32BigEndian(counters.Slice(filled + 12), count);
                    filled += BlockBytes;
                    wrapped = ++count == 0;
                }

                // Widen the extent to clear before the cipher writes keystream, so a throwing cipher leaves none behind.
                int length = Math.Min(filled, input.Length - offset);
                used = Math.Max(used, filled);
                CounterKeystream.Apply(_cipher, counters[..filled], keystream, input.Slice(offset, length), output.Slice(offset, length));
                offset += length;

                // Reject wrap only when the wrapped counter would actually be consumed by another block; a message that
                // ends exactly at counter 0xFFFFFFFF stays within the GCM contract.
                if (wrapped && offset < input.Length)
                {
                    throw new CryptographicException(
                        CryptoResourceStrings.Crypt_Invalid_GcmCounterWrap);
                }
            }

            BinaryPrimitives.WriteUInt32BigEndian(counter.AsSpan(12), count);
        }
        finally
        {
            CryptographyHelper.Clear(keystream[..used]);
        }
    }

    /// <summary>
    /// Computes the GCM authentication tag <c>T = GHASH_H(AAD ‖ C ‖ len(AAD)‖len(C)) ⊕ E_K(J0)</c> into
    /// <paramref name="destination" />.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="ciphertext">The ciphertext bytes authenticated by the tag.</param>
    /// <param name="destination">The destination span (16 bytes).</param>
    private void ComputeTag(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> ciphertext, Span<byte> destination)
    {
        Span<byte> y = stackalloc byte[BlockSize / 8];
        Span<byte> lengthBlock = stackalloc byte[BlockSize / 8];
        Span<byte> encryptedJ0 = stackalloc byte[BlockSize / 8];

        try
        {
            // GHASH over AAD and ciphertext, each padded with zeros to a block boundary, then the length block.
            y.Clear();
            Ghash.Update(in _hashKey, y, aad);
            Ghash.Update(in _hashKey, y, ciphertext);

            // Length block: [len(AAD)]_64 || [len(C)]_64 in bits, big-endian.
            BinaryPrimitives.WriteUInt64BigEndian(lengthBlock[..8], checked((ulong)aad.Length * 8));
            BinaryPrimitives.WriteUInt64BigEndian(lengthBlock.Slice(8, 8), checked((ulong)ciphertext.Length * 8));
            Ghash.Update(in _hashKey, y, lengthBlock);

            // T = y ⊕ E_K(J0).
            _cipher.Encrypt(_j0!, encryptedJ0);
            CryptographyHelper.Xor(y, encryptedJ0, destination);
        }
        finally
        {
            CryptographyHelper.Clear(encryptedJ0);
            CryptographyHelper.Clear(lengthBlock);
            CryptographyHelper.Clear(y);
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data contribution has been finalized exactly once before payload bytes are processed;
    /// treats an unset AAD as empty.
    /// </summary>
    private void EnsureAssociatedDataProcessed()
    {
        if (!_aadProcessed)
        {
            _aad = Array.Empty<byte>();
            _aadProcessed = true;
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this instance has already encrypted or decrypted a message.
    /// GCM transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                CryptoResourceStrings.Op_Invalid_GcmTransformCompleted);
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
}
