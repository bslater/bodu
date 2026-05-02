// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Galois/Counter Mode (GCM) to a 128-bit block cipher, providing single-pass authenticated
/// encryption with associated data (AEAD) per NIST SP 800-38D.
/// </summary>
/// <remarks>
/// <para>
/// GCM combines counter-mode encryption with GHASH authentication over <c>GF(2¹²⁸)</c>:
/// </para>
/// <list type="bullet">
///   <item><description>Hash subkey: <c>H = E_K(0¹²⁸)</c>.</description></item>
///   <item><description>Initial counter <c>J0 = nonce ‖ 0x00000001</c>; payload counter starts at <c>J0 + 1</c>.</description></item>
///   <item><description>Ciphertext: <c>C_i = P_i ⊕ E_K(counter_i)</c>, counter incremented per block.</description></item>
///   <item><description>Tag: <c>T = GHASH_H(AAD ‖ C ‖ len(AAD)‖len(C)) ⊕ E_K(J0)</c>.</description></item>
/// </list>
/// <para>
/// GF(2¹²⁸) multiplication uses the irreducible polynomial <c>x¹²⁸ + x⁷ + x² + x + 1</c> with
/// big-endian bit ordering and the reduction constant <c>0xE1</c> in the most-significant byte.
/// </para>
/// <para>
/// <strong>Nonce length.</strong> This implementation accepts only the 96-bit (12-byte) nonce form, which
/// is what every interoperable GCM consumer uses (TLS 1.2/1.3, IPsec ESP, SSH, QUIC). The
/// SP 800-38D §7.1 GHASH-based derivation for other nonce lengths is intentionally not supported.
/// </para>
/// <para>
/// <strong>Lifecycle.</strong> Each instance encrypts or decrypts exactly one message. A second call to
/// <see cref="Encrypt" /> or <see cref="Decrypt" /> throws <see cref="InvalidOperationException" />.
/// The instance must be disposed when finished; <see cref="Dispose" /> clears the GHASH subkey,
/// initial counter, running counter, and cached associated data. The supplied
/// <see cref="IBlockCipher" /> is not disposed by this type — ownership remains with the caller.
/// </para>
/// <para>
/// <strong>When to use GCM.</strong> The default modern AEAD mode — single-pass, parallelisable, and
/// hardware-accelerated on AES-NI / PCLMULQDQ. The cost is fragility under nonce reuse: a single
/// repeated <c>(key, nonce)</c> pair leaks the GHASH subkey and forfeits authentication forever.
/// For nonce-misuse resistance prefer <see cref="GcmSivModeTransform" /> or
/// <see cref="SivModeTransform" />; for constrained environments prefer <see cref="CcmModeTransform" />;
/// for a single-pass alternative without GCM's failure profile prefer <see cref="OcbModeTransform" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IBlockCipher cipher = new AesBlockCipher(key);
/// using IAeadBlockCipherModeTransform enc = new GcmModeTransform(cipher, nonce);
/// byte[] sealed_   = enc.Encrypt(plaintext, associatedData: header);
///
/// using IAeadBlockCipherModeTransform dec = new GcmModeTransform(cipher, nonce);
/// byte[] recovered = dec.Decrypt(sealed_, associatedData: header);
/// </code>
/// </example>
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-38D">NIST SP 800-38D (GCM/GMAC)</seealso>
/// <seealso cref="AesBlockCipher" />
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
public sealed class GcmModeTransform
    : IAeadBlockCipherModeTransform
    , IDisposable
{
    /// <summary>The fixed GCM block size in bytes (128 bits).</summary>
    private const int BlockSizeBytes = 16;

    /// <summary>The required GCM nonce size in bytes (96 bits).</summary>
    private const int NonceSizeBytes = 12;

    /// <summary>The GCM authentication tag size in bytes (128 bits).</summary>
    private const int DefaultTagSize = 16;

    private readonly IBlockCipher _cipher;
    private byte[]? _h;          // GHASH subkey H = E_K(0¹²⁸)
    private byte[]? _j0;         // initial counter J0 (base for the tag)
    private byte[]? _counter;    // running CTR counter (incremented per block)
    private byte[]? _aad;
    private bool _aadProcessed;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="GcmModeTransform" /> class with a 96-bit GCM nonce.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonce">The 96-bit (12-byte) nonce. Must be unique per key.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="nonce" /> is not
    /// exactly <see cref="NonceSizeBytes" /> bytes.
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
    /// Initialises a new instance of the <see cref="GcmModeTransform" /> class with a 96-bit GCM nonce.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonce">The 96-bit (12-byte) nonce. Must be unique per key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="nonce" /> is not
    /// exactly <see cref="NonceSizeBytes" /> bytes.
    /// </exception>
    public GcmModeTransform(IBlockCipher cipher, ReadOnlySpan<byte> nonce)
        : this(cipher, nonce, nameof(nonce), useInitialCounterBlock: false)
    {
    }

    /// <summary>Unified private constructor; either derives J0 from a 12-byte nonce or uses a precomputed J0 directly.</summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="nonceOrJ0">Either a 12-byte nonce or a 16-byte precomputed J0, depending on <paramref name="useInitialCounterBlock" />.</param>
    /// <param name="parameterName">The name of the parameter from the calling overload, used in <see cref="ArgumentException" /> messages.</param>
    /// <param name="useInitialCounterBlock">When <see langword="true" />, treats <paramref name="nonceOrJ0" /> as a precomputed J0 block; otherwise as a 12-byte nonce.</param>
    private GcmModeTransform(
        IBlockCipher cipher,
        ReadOnlySpan<byte> nonceOrJ0,
        string parameterName,
        bool useInitialCounterBlock)
    {
        this._cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));

        if (cipher.BlockSize != BlockSizeBytes)
            throw new ArgumentException(
                $"GCM requires a block cipher with a {BlockSizeBytes}-byte block size.",
                nameof(cipher));

        if (useInitialCounterBlock)
        {
            if (nonceOrJ0.Length != BlockSizeBytes)
                throw new ArgumentException(
                    $"The initial counter block must be exactly {BlockSizeBytes} bytes.",
                    parameterName);
        }
        else
        {
            if (nonceOrJ0.Length != NonceSizeBytes)
                throw new ArgumentException(
                    $"The GCM nonce must be exactly {NonceSizeBytes} bytes.",
                    parameterName);
        }

        this._h = new byte[BlockSizeBytes];
        this._j0 = new byte[BlockSizeBytes];
        this._counter = new byte[BlockSizeBytes];

        // H = E_K(0¹²⁸).
        Span<byte> zeroBlock = stackalloc byte[BlockSizeBytes];
        this._cipher.Encrypt(zeroBlock, this._h);

        // Build J0.
        if (useInitialCounterBlock)
        {
            nonceOrJ0.CopyTo(this._j0);
        }
        else
        {
            // J0 = nonce || 0x00000001.
            nonceOrJ0.CopyTo(this._j0);
            this._j0[15] = 0x01;
        }

        // CTR counter starts at inc32(J0); J0 itself is reserved for the tag.
        this._j0.CopyTo(this._counter, 0);
        IncrementCounter32(this._counter);
    }

    /// <inheritdoc />
    public int TagSize => DefaultTagSize;

    /// <summary>
    /// Creates a <see cref="GcmModeTransform" /> from a precomputed 128-bit initial counter block.
    /// Test-only entry point exposed via <c>InternalsVisibleTo</c> to support test vectors that publish
    /// <c>J0</c> directly.
    /// </summary>
    /// <param name="cipher">The 128-bit block cipher used by GCM.</param>
    /// <param name="initialCounterBlock">The precomputed 16-byte initial counter block, <c>J0</c>.</param>
    /// <returns>A new <see cref="GcmModeTransform" /> initialised with the supplied <c>J0</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 16-byte block size, or <paramref name="initialCounterBlock" />
    /// is not exactly 16 bytes.
    /// </exception>
    internal static GcmModeTransform CreateForTesting(IBlockCipher cipher, ReadOnlySpan<byte> initialCounterBlock) =>
        new GcmModeTransform(cipher, initialCounterBlock, nameof(initialCounterBlock), useInitialCounterBlock: true);

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Associated data has already been processed, or the instance has already completed encryption or decryption.
    /// </exception>
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        if (this._aadProcessed)
            throw new InvalidOperationException(
                CryptoResourceStrings.CryptographicException_AssociatedDataAlreadyProcessed);

        this._aad = associatedData.IsEmpty ? Array.Empty<byte>() : associatedData.ToArray();
        this._aadProcessed = true;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The instance has already encrypted or decrypted a message.</exception>
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        int required = checked(plaintext.Length + DefaultTagSize);
        if (output.Length < required)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_OutputBufferTooSmall, required),
                nameof(output));

        try
        {
            this.EnsureAssociatedDataProcessed();

            Span<byte> ciphertext = output.Slice(0, plaintext.Length);
            this.ApplyCtr(plaintext, ciphertext);

            Span<byte> tag = stackalloc byte[DefaultTagSize];
            try
            {
                this.ComputeTag(this._aad.AsSpan(), ciphertext, tag);
                tag.CopyTo(output.Slice(plaintext.Length, DefaultTagSize));

                return required;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(tag);
            }
        }
        finally
        {
            this._completed = true;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The instance has already encrypted or decrypted a message.</exception>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        if (ciphertextWithTag.Length < DefaultTagSize)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_CiphertextTooShort, DefaultTagSize),
                nameof(ciphertextWithTag));

        int plaintextLength = ciphertextWithTag.Length - DefaultTagSize;
        if (output.Length < plaintextLength)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_OutputBufferTooSmall, plaintextLength),
                nameof(output));

        try
        {
            this.EnsureAssociatedDataProcessed();

            ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength, DefaultTagSize);

            Span<byte> expectedTag = stackalloc byte[DefaultTagSize];
            try
            {
                this.ComputeTag(this._aad.AsSpan(), ciphertext, expectedTag);

                // Verify before producing plaintext. On failure, output is untouched — no wipe needed.
                if (!CryptographicOperations.FixedTimeEquals(expectedTag, receivedTag))
                    throw new CryptographicException(
                        CryptoResourceStrings.CryptographicException_AuthenticationTagMismatch);

                this.ApplyCtr(ciphertext, output.Slice(0, plaintextLength));

                return plaintextLength;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expectedTag);
            }
        }
        finally
        {
            this._completed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by this instance and clears the GHASH subkey, initial counter,
    /// running counter, and cached associated data from memory. Idempotent. Does not dispose the
    /// supplied <see cref="IBlockCipher" /> — ownership remains with the caller.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed) return;

        CryptoHelpers.ClearAndNullify(ref this._h);
        CryptoHelpers.ClearAndNullify(ref this._j0);
        CryptoHelpers.ClearAndNullify(ref this._counter);
        CryptoHelpers.ClearAndNullify(ref this._aad);

        this._completed = true;
        this._disposed = true;

        GC.SuppressFinalize(this);
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data contribution has been finalised exactly once before payload bytes are
    /// processed; treats an unset AAD as empty.
    /// </summary>
    private void EnsureAssociatedDataProcessed()
    {
        if (!this._aadProcessed)
        {
            this._aad = Array.Empty<byte>();
            this._aadProcessed = true;
        }
    }

    /// <summary>
    /// Applies CTR mode: for each block, computes <c>keystream = E_K(counter)</c>, XORs with input,
    /// increments the counter.
    /// </summary>
    /// <param name="input">The input bytes to XOR with the CTR keystream.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    private void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> keystream = stackalloc byte[BlockSizeBytes];
        try
        {
            byte[] counter = this._counter!;

            for (int offset = 0; offset < input.Length; offset += BlockSizeBytes)
            {
                this._cipher.Encrypt(counter, keystream);
                IncrementCounter32(counter);

                int remaining = Math.Min(BlockSizeBytes, input.Length - offset);
                for (int i = 0; i < remaining; i++)
                    output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keystream);
        }
    }

    /// <summary>
    /// Computes the GCM authentication tag <c>T = GHASH_H(AAD ‖ C ‖ len(AAD)‖len(C)) ⊕ E_K(J0)</c>
    /// into <paramref name="destination" />.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="ciphertext">The ciphertext bytes authenticated by the tag.</param>
    /// <param name="destination">The destination span (16 bytes).</param>
    private void ComputeTag(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> ciphertext, Span<byte> destination)
    {
        Span<byte> y = stackalloc byte[BlockSizeBytes];
        Span<byte> lengthBlock = stackalloc byte[BlockSizeBytes];
        Span<byte> encryptedJ0 = stackalloc byte[BlockSizeBytes];

        try
        {
            ReadOnlySpan<byte> h = this._h!;

            GhashUpdate(y, h, aad);
            GhashUpdate(y, h, ciphertext);

            // Length block: [len(AAD)]_64 || [len(C)]_64 in bits, big-endian.
            BinaryPrimitives.WriteUInt64BigEndian(lengthBlock.Slice(0, 8), checked((ulong)aad.Length * 8));
            BinaryPrimitives.WriteUInt64BigEndian(lengthBlock.Slice(8, 8), checked((ulong)ciphertext.Length * 8));
            GhashBlock(y, h, lengthBlock);

            // T = y ⊕ E_K(J0).
            this._cipher.Encrypt(this._j0!, encryptedJ0);
            for (int i = 0; i < BlockSizeBytes; i++)
                destination[i] = (byte)(y[i] ^ encryptedJ0[i]);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encryptedJ0);
            CryptographicOperations.ZeroMemory(lengthBlock);
            CryptographicOperations.ZeroMemory(y);
        }
    }

    /// <summary>Feeds <paramref name="data" /> into the GHASH accumulator <paramref name="y" /> block by block.</summary>
    /// <param name="y">The running GHASH accumulator (16 bytes); updated in place.</param>
    /// <param name="h">The GHASH subkey.</param>
    /// <param name="data">The input bytes to fold into the GHASH state.</param>
    private static void GhashUpdate(Span<byte> y, ReadOnlySpan<byte> h, ReadOnlySpan<byte> data)
    {
        Span<byte> block = stackalloc byte[BlockSizeBytes];
        try
        {
            for (int offset = 0; offset < data.Length; offset += BlockSizeBytes)
            {
                block.Clear();

                int remaining = Math.Min(BlockSizeBytes, data.Length - offset);
                data.Slice(offset, remaining).CopyTo(block);

                GhashBlock(y, h, block);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block);
        }
    }

    /// <summary>Processes one 16-byte block through GHASH: <c>y = (y ⊕ block) · H</c>.</summary>
    /// <param name="y">The running GHASH accumulator (16 bytes); updated in place.</param>
    /// <param name="h">The GHASH subkey.</param>
    /// <param name="block">A single 16-byte block to fold into <paramref name="y" />.</param>
    private static void GhashBlock(Span<byte> y, ReadOnlySpan<byte> h, ReadOnlySpan<byte> block)
    {
        for (int i = 0; i < BlockSizeBytes; i++)
            y[i] ^= block[i];

        GhashMultiply(y, h, y);
    }

    /// <summary>
    /// Multiplies <paramref name="x" /> by <paramref name="h" /> in GF(2¹²⁸) using the GCM irreducible
    /// polynomial <c>x¹²⁸ + x⁷ + x² + x + 1</c>, with big-endian bit ordering. Result is written into
    /// <paramref name="result" /> (may alias <paramref name="x" />).
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The hash subkey <c>H</c> (16 bytes).</param>
    /// <param name="result">The destination span (16 bytes); receives <c>x · H</c>.</param>
    private static void GhashMultiply(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        Span<byte> z = stackalloc byte[BlockSizeBytes];
        Span<byte> v = stackalloc byte[BlockSizeBytes];

        try
        {
            h.CopyTo(v);

            for (int i = 0; i < 128; i++)
            {
                if ((x[i >> 3] & (0x80 >> (i & 7))) != 0)
                    for (int j = 0; j < BlockSizeBytes; j++) z[j] ^= v[j];

                bool lsb = (v[15] & 0x01) != 0;

                for (int j = 15; j > 0; j--)
                    v[j] = (byte)((v[j] >> 1) | ((v[j - 1] & 0x01) << 7));
                v[0] >>= 1;

                // Reduce by R = 0xE1 || 0…0 (representing x⁷ + x² + x + 1) when the shifted-out bit is set.
                if (lsb) v[0] ^= 0xE1;
            }

            z.CopyTo(result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(v);
            CryptographicOperations.ZeroMemory(z);
        }
    }

    /// <summary>
    /// Increments the 32-bit big-endian counter in the last 4 bytes of <paramref name="counter" /> per
    /// NIST SP 800-38D <c>inc32</c>.
    /// </summary>
    /// <param name="counter">The 16-byte CTR block; its low 32 bits are incremented in place.</param>
    private static void IncrementCounter32(Span<byte> counter)
    {
        for (int i = counter.Length - 1; i >= counter.Length - 4; i--)
            if (++counter[i] != 0) break;
    }

    /// <summary>Throws <see cref="ObjectDisposedException" /> if this instance has been disposed.</summary>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(this._disposed, this);

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this instance has already encrypted or decrypted
    /// a message. GCM transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted()
    {
        if (this._completed)
            throw new InvalidOperationException(
                "This GCM transform has already completed and cannot be reused. Create a new instance per message.");
    }
}
