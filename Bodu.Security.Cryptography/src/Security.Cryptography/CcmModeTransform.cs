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
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow - a CTR-style keystream produces ciphertext and a MAC over nonce, associated data, and ciphertext produces the tag. In CCM the MAC is CBC-MAC."/>
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
/// <description>Nonce (Nlen): 12 bytes - first 12 bytes of the IV.</description>
/// </item>
/// <item>
/// <description>Length field (q): 3 bytes - messages up to 2^24 − 1 bytes.</description>
/// </item>
/// <item>
/// <description>Tag (T): 16 bytes.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Formatting follows NIST SP 800-38C Section 6.3. Flag byte B0: bit 6 = Adata, bits 5-3 = M' = (T−2)/2 = 7, bits 2-0 =
/// L' = q−1 = 2. Counter block A_i: byte 0 = 0x02, bytes 1-12 = nonce, bytes 13-15 = counter (big-endian). AAD length
/// is encoded as a 2-byte big-endian prefix (supports up to 65 279 bytes).
/// </para>
/// <para>
/// <strong>When to use CCM.</strong> Pick CCM when interoperability with constrained-environment standards is
/// required - IEEE 802.15.4 / Zigbee, Bluetooth Mesh, IPsec ESP, and TLS 1.2 with the AES-CCM cipher suites all use it.
/// CCM is two-pass over the message (CBC-MAC then CTR), so it is slower than <see cref="GcmModeTransform" /> on
/// commodity hardware, but it has no Galois-field arithmetic and is easier to implement correctly on minimal
/// microcontrollers. For new general-purpose AEAD on x86/ARM hosts prefer GCM; for nonce-misuse resistance prefer
/// <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />.
/// </para>
/// <para>
/// <strong>Nonce uniqueness is required.</strong> CCM is not nonce-misuse resistant. Reusing a <c>(key, nonce)</c> pair
/// across two messages reuses the CTR keystream and lets an attacker XOR the two ciphertexts to recover
/// <c>P1 XOR P2</c>; CBC-MAC chains from the same starting state are also exposed, which weakens authentication.
/// Callers must guarantee that every <c>(key, nonce)</c> pair is used at most once - typically via a per-message
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

    /// <summary>The block size, in bytes, of the cipher, of the CBC-MAC, and of a counter block.</summary>
    private const int BlockBytes = 16;

    /// <summary>The maximum message length, in bytes, encodable in the 3-byte length field (<c>q = 3</c>): <c>2²⁴ − 1</c>. Internal so tests can validate the constant.</summary>
    /// <remarks>
    /// The B0 length field occupies only bytes 13-15 and the CTR counter is likewise 3 bytes wide. A longer message
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

        // The CBC-MAC, then the tag mask S0 = E(A0).
        Span<byte> scratch = stackalloc byte[2 * BlockBytes];

        try
        {
            Span<byte> mac = scratch[..BlockBytes];
            Span<byte> mask = scratch.Slice(BlockBytes, BlockBytes);

            ComputeCbcMac(_aad.AsSpan(), plaintext, mac);
            EncryptCounterBlock(0, mask);
            ApplyCtr(plaintext, output[..plaintext.Length]);
            CryptographyHelper.Xor(mac, mask, output.Slice(plaintext.Length, TagSizeBits / 8));

            return required;
        }
        finally
        {
            CryptographyHelper.Clear(scratch);
            _completed = true;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <strong>Authentication pattern: write-then-clear.</strong> CCM recomputes its CBC-MAC over the decrypted
    /// plaintext, so the CTR decryption stream is applied to <paramref name="output" /> first and the tag is compared
    /// in constant time afterwards. On any failure - an authentication mismatch or an exception from the underlying
    /// cipher mid-transform - the plaintext region of <paramref name="output" /> is zeroed before the exception
    /// propagates, so unverified plaintext never escapes. See <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for
    /// the library-wide failure contract.
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

        // The received tag, the CBC-MAC, and the expected tag.
        Span<byte> scratch = stackalloc byte[3 * BlockBytes];

        try
        {
            Span<byte> tagCopy = scratch[..BlockBytes];
            Span<byte> mac = scratch.Slice(BlockBytes, BlockBytes);
            Span<byte> expectedTag = scratch.Slice(2 * BlockBytes, BlockBytes);

            // Copy the tag first: the plaintext may be written over the buffer holding it.
            receivedTag.CopyTo(tagCopy);

            ApplyCtr(ciphertext, output[..plaintextLength]);
            ComputeCbcMac(_aad.AsSpan(), output[..plaintextLength], mac);
            EncryptCounterBlock(0, expectedTag);
            CryptographyHelper.Xor(mac, expectedTag, expectedTag);

            if (!CryptographicOperations.FixedTimeEquals(expectedTag, tagCopy))
            {
                CryptographicOperations.ZeroMemory(output[..plaintextLength]);
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }

            return plaintextLength;
        }
        catch
        {
            // Zero the plaintext region on any failure - a tag mismatch or a fault from the underlying
            // cipher mid-transform - so the unverified plaintext this write-then-clear mode has already
            // written never leaks.
            CryptographyHelper.Clear(output[..plaintextLength]);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(scratch);
            _completed = true;
        }
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
    /// <param name="mac">Receives the 16-byte CBC-MAC; cleared if the cipher throws.</param>
    /// <remarks>
    /// The formatted input is never assembled: B0, the block that starts the associated-data encoding, and each
    /// zero-padded final block are built on the stack, and the whole blocks in between are chained straight from the
    /// caller's spans.
    /// </remarks>
    private void ComputeCbcMac(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext, Span<byte> mac)
    {
        Span<byte> block = stackalloc byte[BlockBytes];
        mac.Clear();

        try
        {
            // Block B0: flags || nonce || [len(P)]_3, big-endian (q = 3).
            bool hasAad = aad.Length > 0;
            block[0] = hasAad ? BaseB0WithAad : BaseB0NoAad;
            _nonce.CopyTo(block[1..]);

            uint len = (uint)plaintext.Length;
            block[13] = (byte)(len >> 16);
            block[14] = (byte)(len >> 8);
            block[15] = (byte)len;

            CbcChain.Mac(_cipher, block, mac);

            // AAD: 2-byte length prefix then AAD bytes, zero-padded to block boundary. The first block holds the length
            // and up to fourteen bytes of AAD.
            if (hasAad)
            {
                if (aad.Length >= 0xFF00)
                {
                    throw new NotSupportedException(
                        string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Op_NotSupported_AadTooLongForLengthEncoding, 0xFF00, 2));
                }

                int head = Math.Min(aad.Length, BlockBytes - 2);
                block.Clear();
                block[0] = (byte)(aad.Length >> 8);
                block[1] = (byte)aad.Length;
                aad[..head].CopyTo(block[2..]);
                CbcChain.Mac(_cipher, block, mac);

                MacZeroPadded(aad[head..], block, mac);
            }

            // Plaintext blocks (zero-padded last block).
            MacZeroPadded(plaintext, block, mac);
        }
        catch
        {
            // Zero the partially computed MAC accumulator when the underlying cipher faults mid-MAC,
            // aligned with EaxModeTransform and SivModeTransform.
            CryptographyHelper.Clear(mac);
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(block);
        }
    }

    /// <summary>
    /// Folds <paramref name="data" /> into the CBC-MAC, zero-padding its final partial block.
    /// </summary>
    /// <param name="data">The data to fold in.</param>
    /// <param name="block">A 16-byte scratch block for the padded final block.</param>
    /// <param name="mac">The CBC-MAC state; updated in place.</param>
    private void MacZeroPadded(ReadOnlySpan<byte> data, Span<byte> block, Span<byte> mac)
    {
        int whole = data.Length & ~(BlockBytes - 1);
        CbcChain.Mac(_cipher, data[..whole], mac);

        if (whole < data.Length)
        {
            block.Clear();
            data[whole..].CopyTo(block);
            CbcChain.Mac(_cipher, block, mac);
        }
    }

    /// <summary>
    /// Encrypts counter block <c>A_i = flags || nonce || [i]_3</c>.
    /// </summary>
    /// <param name="counterIndex">The counter-block index.</param>
    /// <param name="destination">Receives the 16-byte encrypted counter block.</param>
    private void EncryptCounterBlock(int counterIndex, Span<byte> destination)
    {
        Span<byte> counter = stackalloc byte[BlockBytes];
        WriteCounterBlock(counter, (uint)counterIndex);
        _cipher.Encrypt(counter, destination);
    }

    /// <summary>
    /// Applies CTR-mode encryption from counter-block index 1, writing <c>input XOR keystream</c> into
    /// <paramref name="output" />, with the counter blocks encrypted a run at a time.
    /// </summary>
    /// <param name="input">The plaintext (or ciphertext) bytes to XOR with the keystream.</param>
    /// <param name="output">The destination span; must be at least <paramref name="input" />.Length bytes.</param>
    /// <remarks>
    /// The payload is at most <see cref="MaxPlaintextBytes" /> bytes, so the 24-bit counter never wraps.
    /// </remarks>
    [SkipLocalsInit]
    private void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> counters = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> keystream = stackalloc byte[CounterKeystream.BatchBytes];
        uint index = 1;
        int used = 0;

        try
        {
            int offset = 0;
            while (offset < input.Length)
            {
                int length = Math.Min(counters.Length, input.Length - offset);
                int filled = (length + BlockBytes - 1) & ~(BlockBytes - 1);
                for (int position = 0; position < filled; position += BlockBytes)
                    WriteCounterBlock(counters.Slice(position, BlockBytes), index++);

                // Widen the extent to clear before the cipher writes keystream, so a throwing cipher leaves none behind.
                used = Math.Max(used, filled);
                CounterKeystream.Apply(_cipher, counters[..filled], keystream, input.Slice(offset, length), output.Slice(offset, length));
                offset += length;
            }
        }
        finally
        {
            CryptographyHelper.Clear(keystream[..used]);
        }
    }

    /// <summary>
    /// Writes counter block <c>A_i = flags || nonce || [i]_3</c>.
    /// </summary>
    /// <param name="destination">The 16-byte destination.</param>
    /// <param name="index">The counter-block index.</param>
    private void WriteCounterBlock(Span<byte> destination, uint index)
    {
        destination[0] = CounterFlagByte;
        _nonce.CopyTo(destination[1..]);
        destination[13] = (byte)(index >> 16);
        destination[14] = (byte)(index >> 8);
        destination[15] = (byte)index;
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
