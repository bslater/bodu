// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Counter with CBC-MAC (CCM) mode to an underlying <see cref="IBlockCipher" />, providing authenticated
/// encryption with associated data (AEAD) per NIST SP 800-38C.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — a CTR-style keystream produces ciphertext and a MAC over nonce, associated data, and ciphertext produces the tag. In CCM the MAC is CBC-MAC."/>
/// </para>
/// <para>
/// CCM is the <b>CTR + CBC-MAC</b> instantiation of the generic AEAD shape above: the top pipeline is the plain CTR
/// keystream generator (panel labeled <em>Keystream Generator (CTR)</em>), and the bottom pipeline is a CBC-MAC chain
/// over the formatted nonce, associated data, and ciphertext that produces the tag.
/// </para>
/// <para>
/// Fixed parameters (matching the most common deployment profile):
/// <list type="bullet">
/// <item>
/// <description>Nonce (Nlen): 12 bytes — first 12 bytes of the IV.</description>
/// </item>
/// <item>
/// <description>Length field (q): 3 bytes — messages up to 2^24 − 1 bytes.</description>
/// </item>
/// <item>
/// <description>Tag (T): 16 bytes.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Formatting follows NIST SP 800-38C Section 6.3. Flag byte B0: bit 6 = Adata, bits 5–3 = M' = (T−2)/2 = 7, bits 2–0 =
/// L' = q−1 = 2. Counter block A_i: byte 0 = 0x02, bytes 1–12 = nonce, bytes 13–15 = counter (big-endian). AAD length
/// is encoded as a 2-byte big-endian prefix (supports up to 65 279 bytes).
/// </para>
/// <para>
/// <strong>When to use CCM.</strong> Pick CCM when interoperability with constrained-environment standards is required
/// — IEEE 802.15.4 / Zigbee, Bluetooth Mesh, IPsec ESP, and TLS 1.2 with the AES-CCM cipher suites all use it. CCM is
/// two-pass over the message (CBC-MAC then CTR), so it is slower than <see cref="GcmModeTransform" /> on commodity
/// hardware, but it has no Galois-field arithmetic and is easier to implement correctly on minimal microcontrollers.
/// For new general-purpose AEAD on x86/ARM hosts prefer GCM; for nonce-misuse resistance prefer
/// <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />.
/// </para>
/// <para>
/// <strong>Nonce uniqueness is required.</strong> CCM is not nonce-misuse resistant. Reusing a <c>(key, nonce)</c> pair
/// across two messages reuses the CTR keystream and lets an attacker XOR the two ciphertexts to recover
/// <c>P1 XOR P2</c>; CBC-MAC chains from the same starting state are also exposed, which weakens authentication.
/// Callers must guarantee that every <c>(key, nonce)</c> pair is used at most once — typically via a per-message
/// counter or a fresh random 96-bit value drawn from a CSPRNG. If nonce uniqueness cannot be guaranteed prefer
/// <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />.
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
/// byte[] iv = BuildCcmIv(nonce); // 12-byte nonce in the first 12 bytes of the IV
/// using IAeadBlockCipherModeTransform ccm = new CcmModeTransform(cipher, iv);
/// byte[] sealed_ = ccm.Encrypt(plaintext, associatedData: header);
/// using IAeadBlockCipherModeTransform dec = new CcmModeTransform(cipher, iv);
/// byte[] recovered = dec.Decrypt(sealed_, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#ccm--a-two-pass-alternative">CCM walk-through in the
/// AEAD-modes guide</seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class CcmModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>Length of the CCM nonce is 96 bits (12 bytes). Byte length is derived inline via <see cref="NonceSizeBits" /> / 8.</summary>
    private const int NonceSizeBits = 96;

    /// <summary>Length of the CCM authentication tag is 128 bits (16 bytes). Byte length is derived inline via <see cref="TagSizeBits" /> / 8.</summary>
    private const int TagSizeBits = 128;

    /// <summary>The maximum message length, in bytes, encodable in the 3-byte length field (<c>q = 3</c>): <c>2²⁴ − 1</c>. Internal so tests can validate the constant.</summary>
    /// <remarks>
    /// The B0 length field occupies only bytes 13–15 and the CTR counter is likewise 3 bytes wide. A longer message
    /// would silently truncate the encoded length (corrupting the CBC-MAC) and wrap the counter (reusing keystream), so
    /// a message at or beyond this ceiling must be rejected rather than transformed.
    /// </remarks>
    internal const int MaxPlaintextBytes = (1 << 24) - 1;

    /// <summary>The first byte of every CTR counter block A_i.</summary>
    /// <remarks>
    /// Encodes <c>L' = q - 1 = 2</c>.
    /// </remarks>
    private const byte CounterFlagByte = 0x02; // L' = q-1 = 2

    /// <summary>The base value of the CBC-MAC flag byte B0 when no associated data is present.</summary>
    /// <remarks>
    /// Bit layout <c>0_111_010</c>: Adata = 0, M' = 7, L' = 2.
    /// </remarks>
    private const byte BaseB0NoAad = 0x3A;  // 0_111_010

    /// <summary>The base value of the CBC-MAC flag byte B0 when associated data is present.</summary>
    /// <remarks>
    /// Bit layout <c>1_111_010</c>: Adata = 1, M' = 7, L' = 2.
    /// </remarks>
    private const byte BaseB0WithAad = 0x7A;  // 1_111_010

    /// <summary>The underlying block cipher used for CTR encryption and the CBC-MAC chain.</summary>
    private readonly IBlockCipher _cipher;

    /// <summary>The 12-byte CCM nonce derived from the supplied initialization vector.</summary>
    private readonly byte[] _nonce;

    /// <summary>The associated authenticated data captured for the MAC, or <see langword="null" /> until set.</summary>
    private byte[]? _aad;

    /// <summary>Indicates whether the associated data has been captured.</summary>
    private bool _aadProcessed;

    /// <summary>Indicates whether this single-use transform has already processed a message.</summary>
    private bool _completed;

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CcmModeTransform" /> class. The first 12 bytes of
    /// <paramref name="iv" /> are used as the CCM nonce.
    /// </summary>
    /// <param name="cipher">The block cipher used to perform the underlying block encryption operations.</param>
    /// <param name="iv">
    /// The initialization vector from which the CCM nonce is derived. The value must be exactly one cipher block in
    /// length; only the first 12 bytes are copied and used as the nonce.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="iv" /> length does not equal the cipher block size.
    /// </exception>
    public CcmModeTransform(IBlockCipher cipher, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(cipher);
        CryptographyThrowHelper.ThrowIfIvLengthInvalid(iv, cipher.BlockSize);
        _cipher = cipher;

        _nonce = new byte[NonceSizeBits / 8];
        iv.AsSpan(0, NonceSizeBits / 8).CopyTo(_nonce);
    }

    /// <inheritdoc />
    /// <value>Length of the CCM authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <inheritdoc />
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        ThrowIfDisposed();

        CryptographyThrowHelper.ThrowIfAssociatedDataAlreadyProcessed(_aadProcessed);

        _aad = associatedData.ToArray();
        _aadProcessed = true;
    }

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        ValidatePlaintextLength(plaintext.Length);

        int required = plaintext.Length + (TagSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, required);

        EnsureAadProcessed();

        byte[] mac = ComputeCbcMac(_aad.AsSpan(), plaintext);
        byte[] encTag = XorWithCtrBlock(mac, counterIndex: 0);

        EncryptCtr(plaintext, output[..plaintext.Length], startIndex: 1);
        encTag.AsSpan(0, TagSizeBits / 8).CopyTo(output[plaintext.Length..]);
        _completed = true;
        return required;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <strong>Authentication pattern: verify-before-release.</strong> The CBC-MAC tag is recomputed and compared in
    /// constant time before the CTR decryption stream is applied to <paramref name="output" />; no plaintext byte is
    /// ever written when authentication fails. See <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for the
    /// library-wide failure contract.
    /// </remarks>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        CryptographyThrowHelper.ThrowIfCiphertextTooShort(ciphertextWithTag, TagSizeBits / 8);

        int plaintextLength = ciphertextWithTag.Length - (TagSizeBits / 8);
        ValidatePlaintextLength(plaintextLength);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, plaintextLength);

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag[plaintextLength..];

        EncryptCtr(ciphertext, output[..plaintextLength], startIndex: 1);

        byte[] mac = ComputeCbcMac(_aad.AsSpan(), output[..plaintextLength]);
        byte[] encTag = XorWithCtrBlock(mac, counterIndex: 0);

        if (!CryptographicOperations.FixedTimeEquals(encTag.AsSpan(0, TagSizeBits / 8), receivedTag))
        {
            CryptographicOperations.ZeroMemory(output[..plaintextLength]);
            _completed = true;
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
        }

        _completed = true;
        return plaintextLength;
    }

    /// <summary>
    /// Releases the resources used by this instance and clears retained nonce and associated-data state from memory.
    /// </summary>
    /// <remarks>
    /// The supplied <see cref="IBlockCipher" /> is not disposed by this type. Ownership remains with the caller.
    /// </remarks>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or decrypted a message.
    /// CCM transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptographyThrowHelper.ThrowIfAlreadyCompleted(_completed);

    /// <summary>
    /// Validates that a message length fits the 3-byte CCM length field, rejecting a longer message that would silently
    /// truncate the CBC-MAC length encoding and wrap the CTR counter.
    /// </summary>
    /// <param name="length">The plaintext (or ciphertext) length, in bytes.</param>
    /// <exception cref="CryptographicException">The length exceeds <see cref="MaxPlaintextBytes" />.</exception>
    internal static void ValidatePlaintextLength(long length)
    {
        if (length > MaxPlaintextBytes)
        {
            throw new CryptographicException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_CcmPlaintextLengthExceeded, length, MaxPlaintextBytes));
        }
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release managed resources; <see langword="false" /> to release unmanaged resources
    /// only.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CryptographyHelper.Clear(_nonce);
            CryptographyHelper.ClearAndNullify(ref _aad);

            _aadProcessed = false;
        }

        _disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data (AAD) MAC contribution has been finalized exactly once before payload bytes are
    /// processed; no-op on subsequent invocations.
    /// </summary>
    private void EnsureAadProcessed()
    {
        if (!_aadProcessed)
        {
            _aad = [];
            _aadProcessed = true;
        }
    }

    /// <summary>
    /// Computes CBC-MAC over the NIST SP 800-38C formatted input (B0 + AAD encoding + plaintext).
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="plaintext">The plaintext bytes whose MAC is being computed.</param>
    /// <returns>The computed CBC-MAC tag, truncated to the configured tag length.</returns>
    private byte[] ComputeCbcMac(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext)
    {
        int blockSize = _cipher.BlockSize / 8;
        byte[] mac = new byte[blockSize];

        // Block B0.
        bool hasAad = aad.Length > 0;
        byte[] b0 = new byte[blockSize];
        b0[0] = hasAad ? BaseB0WithAad : BaseB0NoAad;
        _nonce.CopyTo(b0, 1);

        // Message length in last 3 bytes (big-endian, q=3).
        uint len = (uint)plaintext.Length;
        b0[15] = (byte)len;
        b0[14] = (byte)(len >> 8);
        b0[13] = (byte)(len >> 16);
        CbcMacUpdate(mac, b0);

        // AAD: 2-byte length prefix then AAD bytes, zero-padded to block boundary.
        if (hasAad)
        {
            if (aad.Length >= 0xFF00)
            {
                throw new NotSupportedException(
                    string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_NotSupported_AadTooLongForLengthEncoding, 0xFF00, 2));
            }

            // Encode: 2-byte length + aad + zero-padding to block multiple.
            int encodedLen = 2 + aad.Length;
            int padded = ((encodedLen + blockSize - 1) / blockSize) * blockSize;
            byte[] aadEncoded = new byte[padded];
            aadEncoded[0] = (byte)(aad.Length >> 8);
            aadEncoded[1] = (byte)aad.Length;
            aad.CopyTo(aadEncoded.AsSpan(2));
            for (int i = 0; i < aadEncoded.Length; i += blockSize)
                CbcMacUpdate(mac, aadEncoded.AsSpan(i, blockSize));
        }

        // Plaintext blocks (zero-padded last block).
        for (int i = 0; i < plaintext.Length; i += blockSize)
        {
            byte[] block = new byte[blockSize];
            plaintext.Slice(i, Math.Min(blockSize, plaintext.Length - i)).CopyTo(block);
            CbcMacUpdate(mac, block);
        }

        return mac;
    }

    /// <summary>
    /// XORs <paramref name="block" /> into <paramref name="mac" /> and runs a single AES block through the underlying
    /// cipher to advance the CBC-MAC state.
    /// </summary>
    /// <param name="mac">The CBC-MAC accumulator (16 bytes); updated in place.</param>
    /// <param name="block">The next input block; must be 16 bytes.</param>
    private void CbcMacUpdate(byte[] mac, ReadOnlySpan<byte> block)
    {
        Span<byte> xored = stackalloc byte[mac.Length];
        for (int i = 0; i < mac.Length; i++) xored[i] = (byte)(mac[i] ^ block[i]);
        _cipher.Encrypt(xored, mac);
    }

    /// <summary>
    /// Builds counter block A_i (flags | nonce | counter), encrypts it, and XORs with input.
    /// </summary>
    /// <param name="input">The input bytes to XOR with the keystream.</param>
    /// <param name="counterIndex">The CTR block index that produces the keystream block.</param>
    /// <returns>A fresh array holding <c>input XOR keystream</c>.</returns>
    private byte[] XorWithCtrBlock(ReadOnlySpan<byte> input, int counterIndex)
    {
        int blockSize = _cipher.BlockSize / 8;
        byte[] ctr = new byte[blockSize];

        ctr[0] = CounterFlagByte;

        _nonce.CopyTo(ctr, 1);

        ctr[15] = (byte)counterIndex;
        ctr[14] = (byte)(counterIndex >> 8);
        ctr[13] = (byte)(counterIndex >> 16);

        byte[] ks = new byte[blockSize];
        _cipher.Encrypt(ctr, ks);

        for (int i = 0; i < Math.Min(input.Length, blockSize); i++)
            ks[i] ^= input[i];

        return ks;
    }

    /// <summary>
    /// Applies CTR-mode encryption starting from counter-block index <paramref name="startIndex" />, writing
    /// <c>input XOR keystream</c> into <paramref name="output" />.
    /// </summary>
    /// <param name="input">The plaintext (or ciphertext) bytes to XOR with the keystream.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    /// <param name="startIndex">The starting counter-block index in the CTR sequence.</param>
    private void EncryptCtr(ReadOnlySpan<byte> input, Span<byte> output, int startIndex)
    {
        int blockSize = _cipher.BlockSize / 8;
        Span<byte> ks = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            int idx = startIndex + (offset / blockSize);
            byte[] ctr = new byte[blockSize];

            ctr[0] = CounterFlagByte;

            _nonce.CopyTo(ctr, 1);

            ctr[15] = (byte)idx;
            ctr[14] = (byte)(idx >> 8);
            ctr[13] = (byte)(idx >> 16);

            _cipher.Encrypt(ctr, ks);

            int rem = Math.Min(blockSize, input.Length - offset);

            for (int i = 0; i < rem; i++)
                output[offset + i] = (byte)(input[offset + i] ^ ks[i]);
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
