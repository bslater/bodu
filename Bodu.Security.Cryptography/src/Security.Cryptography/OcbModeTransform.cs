// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies Offset CodeBook mode version 3 (OCB3) to an underlying <see cref="IBlockCipher" />, providing single-pass
/// authenticated encryption with associated data per RFC 7253.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow - OCB3 realizes both the keystream and the MAC pipelines as a single offset-driven pass over each block."/>
/// </para>
/// <para>
/// OCB3 collapses the two pipelines of the generic AEAD shape above into a <em>single pass</em>: the keystream and the
/// MAC chain share the same per-block offset Δ<sub>i</sub>, so each block is touched by the cipher exactly once. In the
/// diagram, this corresponds to merging the top and bottom arrows that reach the MAC - the ciphertext output is
/// simultaneously the next input to the authentication accumulator.
/// </para>
/// <para>
/// The nonce is derived from the first 12 bytes of the IV supplied to the constructor. The tag size defaults to 128
/// bits (16 bytes / TAGLEN = 128) and may be set to any positive multiple of 8 bits between 8 and the cipher block size
/// via the <c>tagSize</c> constructor parameter. Supported RFC 7253 values are 64, 96, and 128 bits (8, 12, and 16
/// bytes).
/// </para>
/// <para>
/// Offset initialization uses the RFC 7253 §2.4 K_top stretch:
/// <code>
///<![CDATA[
///   Nonce  = num2str(TAGLEN mod 128, 7) || zeros(120-bitlen(N)) || 1 || N
///   bottom = str2num(Nonce[123..128])
///   K_top  = ENCIPHER(K, Nonce[1..122] || zeros(6))
///   Stretch = K_top || (K_top[1..64] XOR K_top[9..72])   -- adjacent-byte XOR
///   Offset_0 = Stretch[1+bottom..128+bottom]
///]]>
/// </code>
/// </para>
/// <para>
/// The L array uses GF(2^128) doubling with polynomial x^128 + x^7 + x^2 + x + 1 (big-endian).
/// </para>
/// <para>
/// <strong>When to use OCB3.</strong> Pick OCB3 when you want a single-pass AEAD mode without GCM's
/// catastrophic-on-nonce-reuse profile - OCB still requires nonces to be unique per key, but the failure mode is
/// graceful (only that one message's confidentiality is lost; the GHASH-key-leak amplification does not apply). OCB
/// historically had patent encumbrances that limited adoption; the patents have since been placed into the public
/// domain, but <see cref="GcmModeTransform" /> remains the more widely deployed choice in practice. For nonce-misuse
/// resistance prefer <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />; for constrained
/// environments prefer <see cref="CcmModeTransform" />.
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
/// byte[] iv = BuildOcbIv(nonce); // first 12 bytes of the IV are the nonce
/// using IAeadBlockCipherModeTransform ocb = new OcbModeTransform(cipher, iv, tagSize: 128);
///
/// byte[] sealed_ = ocb.Encrypt(plaintext, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/aead-modes.html#ocb3--single-pass-rfc-7253">OCB3 walk-through in the
/// AEAD-modes guide</seealso> <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class OcbModeTransform
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>Length of the OCB cipher block is 128 bits (16 bytes). Byte length derived inline via <see cref="BlockSizeBits" /> / 8.</summary>
    private const int BlockSizeBits = 128;

    /// <summary>Length of the OCB nonce is 96 bits (12 bytes). Byte length derived inline via <see cref="NonceSizeBits" /> / 8.</summary>
    private const int NonceSizeBits = 96;

    /// <summary>The OCB block size, in bytes.</summary>
    private const int BlockBytes = BlockSizeBits / 8;

    /// <summary>The number of precomputed L values, sufficient for up to 2^32 blocks.</summary>
    private const int MaxLValues = 32;

    /// <summary>The underlying block cipher used for OCB3 encipher operations.</summary>
    private readonly IBlockCipher _cipher;

    /// <summary>The 12-byte OCB3 nonce derived from the supplied initialization vector.</summary>
    private readonly byte[] _nonce;

    /// <summary>The authentication-tag length, in bytes (between 1 and the cipher block size).</summary>
    private readonly int _tagLen;

    /// <summary>The key-dependent values, one block each: <c>L_* = E(0^128)</c>, <c>L_$ = double(L_*)</c>, then <c>L[0] = double(L_$)</c> to <c>L[31]</c>, where <c>L[i] = double(L[i-1])</c>.</summary>
    private readonly byte[] _lTable;

    /// <summary>The buffered associated authenticated data, or <see langword="null" /> until processed.</summary>
    private byte[]? _aad;

    /// <summary>Indicates whether the associated data has been processed.</summary>
    private bool _aadProcessed;

    /// <summary>Indicates whether this transform has already completed an encrypt or decrypt operation.</summary>
    private bool _completed;

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcbModeTransform" /> class.
    /// </summary>
    /// <param name="cipher">The block cipher. Must have a 128-bit (16-byte) block size.</param>
    /// <param name="iv">
    /// The initialization vector. The first 12 bytes are used as the OCB3 nonce. Must equal the cipher block size. A
    /// defensive copy is taken.
    /// </param>
    /// <param name="tagSize">
    /// The authentication-tag size, in bits, of the OCB3 tag. Must be a positive multiple of 8 between 8 bits (1 byte)
    /// and the cipher block size. RFC 7253 defines recommended values of 64, 96, and 128 bits (8, 12, and 16 bytes).
    /// Defaults to 128 bits (16 bytes).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cipher" /> does not have a 128-bit (16-byte) block size, <paramref name="iv" /> length does not
    /// equal the cipher block size, or <paramref name="tagSize" /> is outside the range [8 bits, cipher block size] or
    /// is not a positive multiple of 8.
    /// </exception>
    public OcbModeTransform(IBlockCipher cipher, byte[] iv, int tagSize = 128)
    {
        if (cipher is null) throw new ArgumentNullException(nameof(cipher));
        CryptographyThrowHelper.ThrowIfIvLengthInvalid(iv, cipher.BlockSize);

        CryptographyThrowHelper.ThrowIfBlockSizeNotEqualTo(cipher, BlockSizeBits, "OCB", nameof(cipher));

        if (tagSize < 8 || tagSize > cipher.BlockSize || tagSize % 8 != 0)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_OcbTagSize, tagSize, cipher.BlockSize),
                nameof(tagSize));
        }

        _cipher = cipher;

        _nonce = new byte[NonceSizeBits / 8];
        iv.AsSpan(0, NonceSizeBits / 8).CopyTo(_nonce);

        _tagLen = tagSize / 8;

        // RFC 7253 §2.1 - Key-dependent constants derived once per key: L_* = ENCIPHER(K, zeros(128)), and each later
        // block of the table the double of the one before it - L_$, then L[0] to L[31].
        _lTable = new byte[(MaxLValues + 2) * BlockBytes];
        Span<byte> table = _lTable;
        Span<byte> zeroBlock = stackalloc byte[BlockBytes];
        zeroBlock.Clear();
        cipher.Encrypt(zeroBlock, table[..BlockBytes]);

        for (int i = 1; i < MaxLValues + 2; i++)
            GaloisField128.Double(table.Slice((i - 1) * BlockBytes, BlockBytes), table.Slice(i * BlockBytes, BlockBytes));
    }

    /// <inheritdoc />
    /// <value>The configured OCB authentication-tag size, in bits. Defaults to 128 bits (16 bytes).</value>
    public int TagSize => _tagLen * 8;

    /// <summary>
    /// Gets <c>L_*</c>, the encryption of the zero block.
    /// </summary>
    private ReadOnlySpan<byte> LStar =>
        _lTable.AsSpan(0, BlockBytes);

    /// <summary>
    /// Gets <c>L_$ = double(L_*)</c>.
    /// </summary>
    private ReadOnlySpan<byte> LDollar =>
        _lTable.AsSpan(BlockBytes, BlockBytes);

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

        int required = plaintext.Length + _tagLen;
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, required);

        EnsureAadProcessed();

        // Scratch: the offset, the checksum, a pad block, the padded final block, and the tag.
        Span<byte> scratch = stackalloc byte[5 * BlockBytes];

        try
        {
            Span<byte> offset = scratch[..BlockBytes];
            Span<byte> checksum = scratch.Slice(BlockBytes, BlockBytes);
            Span<byte> pad = scratch.Slice(2 * BlockBytes, BlockBytes);
            Span<byte> padded = scratch.Slice(3 * BlockBytes, BlockBytes);
            Span<byte> tag = scratch.Slice(4 * BlockBytes, BlockBytes);

            ComputeInitialOffset(offset);
            checksum.Clear();

            int full = plaintext.Length & ~(BlockBytes - 1);
            ProcessFullBlocks(plaintext[..full], output[..full], encrypt: true, offset, checksum);

            // P_*: Offset_* = Offset_m XOR L_*, C_* = P_* XOR ENCIPHER(K, Offset_*), Checksum_* = Checksum_m XOR
            // (P_* || 1 || 0...). P_* is read into the checksum before C_* is written, so exact aliasing is safe.
            int remainder = plaintext.Length - full;
            if (remainder > 0)
            {
                CryptographyHelper.Xor(offset, LStar, offset);
                _cipher.Encrypt(offset, pad);

                padded.Clear();
                plaintext[full..].CopyTo(padded);
                padded[remainder] = 0x80;
                CryptographyHelper.Xor(checksum, padded, checksum);

                CryptographyHelper.Xor(plaintext[full..], pad[..remainder], output.Slice(full, remainder));
            }

            // Tag = ENCIPHER(K, Checksum_* XOR Offset_* XOR L_$) XOR HASH(K, A).
            ComputeTag(checksum, offset, pad, tag);
            tag[.._tagLen].CopyTo(output[plaintext.Length..]);

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
    /// <strong>Authentication pattern: write-then-clear.</strong> OCB3 recomputes its checksum over the decrypted
    /// plaintext, so the per-block decryption is written into <paramref name="output" /> first and the tag is compared
    /// in constant time afterwards. On any failure - an authentication mismatch or an exception from the underlying
    /// cipher mid-transform - the plaintext region of <paramref name="output" /> is zeroed before the exception
    /// propagates, so unverified plaintext never escapes. See <see cref="IAeadBlockCipherModeTransform.Decrypt" /> for
    /// the library-wide failure contract.
    /// </remarks>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();

        CryptographyThrowHelper.ThrowIfCiphertextTooShort(ciphertextWithTag, _tagLen);

        int plaintextLength = ciphertextWithTag.Length - _tagLen;
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, plaintextLength);

        EnsureAadProcessed();

        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..plaintextLength];
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag[plaintextLength..];

        // Scratch: the offset, the checksum, a pad block, the padded final block, the tag, and the received tag.
        Span<byte> scratch = stackalloc byte[6 * BlockBytes];

        try
        {
            Span<byte> offset = scratch[..BlockBytes];
            Span<byte> checksum = scratch.Slice(BlockBytes, BlockBytes);
            Span<byte> pad = scratch.Slice(2 * BlockBytes, BlockBytes);
            Span<byte> padded = scratch.Slice(3 * BlockBytes, BlockBytes);
            Span<byte> tag = scratch.Slice(4 * BlockBytes, BlockBytes);
            Span<byte> tagCopy = scratch.Slice(5 * BlockBytes, _tagLen);

            // Copy the tag first: the plaintext may be written over the buffer holding it.
            receivedTag.CopyTo(tagCopy);

            ComputeInitialOffset(offset);
            checksum.Clear();

            int full = plaintextLength & ~(BlockBytes - 1);
            ProcessFullBlocks(ciphertext[..full], output[..full], encrypt: false, offset, checksum);

            int remainder = plaintextLength - full;
            if (remainder > 0)
            {
                CryptographyHelper.Xor(offset, LStar, offset);
                _cipher.Encrypt(offset, pad);
                CryptographyHelper.Xor(ciphertext[full..], pad[..remainder], output.Slice(full, remainder));

                padded.Clear();
                output.Slice(full, remainder).CopyTo(padded);
                padded[remainder] = 0x80;
                CryptographyHelper.Xor(checksum, padded, checksum);
            }

            ComputeTag(checksum, offset, pad, tag);
            if (!CryptographicOperations.FixedTimeEquals(tag[.._tagLen], tagCopy))
            {
                CryptographyHelper.Clear(output[..plaintextLength]);
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
    /// Throws <see cref="InvalidOperationException" /> if this transform has already encrypted or decrypted a message.
    /// OCB transforms are single-use; create a fresh instance per message.
    /// </summary>
    private void ThrowIfCompleted() =>
        CryptographyThrowHelper.ThrowIfAlreadyCompleted(_completed);

    /// <summary>
    /// Releases the resources used by this instance and clears retained nonce, OCB offset constants, and
    /// associated-data state from memory.
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
            CryptographyHelper.Clear(_lTable);

            CryptographyHelper.ClearAndNullify(ref _aad);

            _aadProcessed = false;
        }

        _disposed = true;
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the associated-data authentication contribution has been initialized before payload processing.
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
    /// Computes the initial offset Offset_0 using the RFC 7253 §2.4 K_top stretch. Supports a 12-byte nonce.
    /// </summary>
    /// <param name="offset">Receives the 16-byte <c>Offset_0</c>.</param>
    /// <remarks>
    /// <c>Stretch = Ktop || (Ktop[1..64] XOR Ktop[9..72])</c> is held as three 64-bit words, and <c>Offset_0</c> is its
    /// 128 bits from bit <c>bottom</c>; <c>bottom</c> comes from the public nonce.
    /// </remarks>
    private void ComputeInitialOffset(Span<byte> offset)
    {
        Span<byte> nonceWord = stackalloc byte[BlockBytes];
        Span<byte> ktop = stackalloc byte[BlockBytes];

        try
        {
            // Nonce = num2str(TAGLEN mod 128, 7) || zeros(120 - bitlen(N)) || 1 || N.
            nonceWord.Clear();
            nonceWord[0] = (byte)(((_tagLen * 8) % 128) << 1);
            nonceWord[3] = 0x01;
            _nonce.CopyTo(nonceWord[4..]);

            int bottom = nonceWord[BlockBytes - 1] & 0x3F;
            nonceWord[BlockBytes - 1] &= 0xC0;
            _cipher.Encrypt(nonceWord, ktop);

            ulong k0 = BinaryPrimitives.ReadUInt64BigEndian(ktop);
            ulong k1 = BinaryPrimitives.ReadUInt64BigEndian(ktop.Slice(8));
            ulong stretchTail = k0 ^ ((k0 << 8) | (k1 >> 56));

            ulong high = bottom == 0 ? k0 : (k0 << bottom) | (k1 >> (64 - bottom));
            ulong low = bottom == 0 ? k1 : (k1 << bottom) | (stretchTail >> (64 - bottom));
            BinaryPrimitives.WriteUInt64BigEndian(offset, high);
            BinaryPrimitives.WriteUInt64BigEndian(offset.Slice(8), low);
        }
        finally
        {
            CryptographyHelper.Clear(ktop);
            CryptographyHelper.Clear(nonceWord);
        }
    }

    /// <summary>
    /// Encrypts or decrypts the whole blocks of a message a run at a time - the offsets for up to 4 KiB first, then one
    /// multi-block cipher call between two XORs with them - and folds the plaintext blocks into the checksum.
    /// </summary>
    /// <param name="input">The whole blocks to transform.</param>
    /// <param name="output">The destination; may be the same memory as <paramref name="input" />.</param>
    /// <param name="encrypt"><see langword="true" /> to encrypt; <see langword="false" /> to decrypt.</param>
    /// <param name="offset"><c>Offset_0</c> on entry; <c>Offset_m</c> on return.</param>
    /// <param name="checksum">The checksum; the plaintext blocks are XORed in.</param>
    [SkipLocalsInit]
    private void ProcessFullBlocks(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt, Span<byte> offset, Span<byte> checksum)
    {
        Span<byte> offsets = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> work = stackalloc byte[CounterKeystream.BatchBytes];
        int index = 1;
        int used = 0;

        try
        {
            int position = 0;
            while (position < input.Length)
            {
                int length = Math.Min(offsets.Length, input.Length - position);
                ReadOnlySpan<byte> source = input.Slice(position, length);
                Span<byte> run = output.Slice(position, length);
                FillOffsets(offsets[..length], ref index, offset);
                used = Math.Max(used, length);

                // Fold the plaintext in before the run's output can overwrite it.
                if (encrypt)
                    XorBlocksInto(source, checksum);

                CryptographyHelper.Xor(source, offsets[..length], work[..length]);
                if (encrypt)
                    _cipher.EncryptBlocks(work[..length], run);
                else
                    _cipher.DecryptBlocks(work[..length], run);

                CryptographyHelper.Xor(run, offsets[..length], run);
                if (!encrypt)
                    XorBlocksInto(run, checksum);

                position += length;
            }
        }
        finally
        {
            CryptographyHelper.Clear(offsets[..used]);
            CryptographyHelper.Clear(work[..used]);
        }
    }

    /// <summary>
    /// Computes the tag, <c>ENCIPHER(K, Checksum XOR Offset XOR L_$) XOR HASH(K, A)</c>.
    /// </summary>
    /// <param name="checksum">The final checksum.</param>
    /// <param name="offset">The final offset.</param>
    /// <param name="scratch">A 16-byte scratch block.</param>
    /// <param name="tag">Receives the full 16-byte tag.</param>
    private void ComputeTag(ReadOnlySpan<byte> checksum, ReadOnlySpan<byte> offset, Span<byte> scratch, Span<byte> tag)
    {
        CryptographyHelper.Xor(checksum, offset, scratch);
        CryptographyHelper.Xor(scratch, LDollar, scratch);
        _cipher.Encrypt(scratch, tag);

        ComputeHash(_aad!, scratch);
        CryptographyHelper.Xor(tag, scratch, tag);
    }

    /// <summary>
    /// Computes HASH(K, A), the OCB3 authentication of associated data per RFC 7253, a run of whole blocks at a time.
    /// </summary>
    /// <param name="aad">The associated authenticated data.</param>
    /// <param name="sum">Receives the 16-byte HASH value; cleared if the cipher throws.</param>
    [SkipLocalsInit]
    private void ComputeHash(ReadOnlySpan<byte> aad, Span<byte> sum)
    {
        sum.Clear();
        if (aad.IsEmpty)
            return;

        Span<byte> offsets = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> work = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> offset = stackalloc byte[BlockBytes];
        offset.Clear();
        int index = 1;
        int used = 0;

        try
        {
            // Sum = XOR of ENCIPHER(K, A_i XOR Offset_i) over the whole blocks. The encryptions land in the offsets
            // buffer, which the XOR has finished with.
            int full = aad.Length & ~(BlockBytes - 1);
            int position = 0;
            while (position < full)
            {
                int length = Math.Min(offsets.Length, full - position);
                FillOffsets(offsets[..length], ref index, offset);
                used = Math.Max(used, length);

                CryptographyHelper.Xor(aad.Slice(position, length), offsets[..length], work[..length]);
                _cipher.EncryptBlocks(work[..length], offsets[..length]);
                XorBlocksInto(offsets[..length], sum);

                position += length;
            }

            // A_*: Sum XOR= ENCIPHER(K, (A_* || 1 || 0...) XOR Offset_m XOR L_*).
            int remainder = aad.Length - full;
            if (remainder > 0)
            {
                used = Math.Max(used, 2 * BlockBytes);
                Span<byte> padded = work[..BlockBytes];
                padded.Clear();
                aad[full..].CopyTo(padded);
                padded[remainder] = 0x80;

                CryptographyHelper.Xor(offset, LStar, offset);
                CryptographyHelper.Xor(padded, offset, padded);
                _cipher.Encrypt(padded, offsets[..BlockBytes]);
                CryptographyHelper.Xor(sum, offsets[..BlockBytes], sum);
            }
        }
        catch
        {
            sum.Clear();
            throw;
        }
        finally
        {
            CryptographyHelper.Clear(offset);
            CryptographyHelper.Clear(offsets[..used]);
            CryptographyHelper.Clear(work[..used]);
        }
    }

    /// <summary>
    /// Returns <c>L[i]</c>, the <paramref name="i" />-th double of <c>L_$</c>.
    /// </summary>
    /// <param name="i">The index, from 0 to 31: the number of trailing zeros of a block index.</param>
    /// <returns>The value.</returns>
    private ReadOnlySpan<byte> L(int i) =>
        _lTable.AsSpan((i + 2) * BlockBytes, BlockBytes);

    /// <summary>
    /// Writes consecutive offsets, <c>Offset_i = Offset_{i-1} XOR L_{ntz(i)}</c>, one per block of
    /// <paramref name="offsets" />.
    /// </summary>
    /// <param name="offsets">Receives the offsets, a whole number of blocks.</param>
    /// <param name="index">The 1-based index of the first block; advanced past the last.</param>
    /// <param name="offset">The offset before the first block on entry; the last offset written on return.</param>
    /// <remarks>
    /// The table is indexed by the number of trailing zeros of the public block index, not by data.
    /// </remarks>
    private void FillOffsets(Span<byte> offsets, ref int index, Span<byte> offset)
    {
        Vector128<byte> current = Vector128.Create((ReadOnlySpan<byte>)offset);
        for (int position = 0; position < offsets.Length; position += BlockBytes)
        {
            current ^= Vector128.Create(L(BitOperations.TrailingZeroCount(index++)));
            current.CopyTo(offsets.Slice(position));
        }

        current.CopyTo(offset);
    }

    /// <summary>
    /// XORs every 16-byte block of <paramref name="blocks" /> into <paramref name="accumulator" />.
    /// </summary>
    /// <param name="blocks">The blocks, a whole number of them.</param>
    /// <param name="accumulator">The 16-byte accumulator; updated in place.</param>
    private static void XorBlocksInto(ReadOnlySpan<byte> blocks, Span<byte> accumulator)
    {
        Vector128<byte> sum = Vector128.Create((ReadOnlySpan<byte>)accumulator);
        ref byte source = ref MemoryMarshal.GetReference(blocks);
        for (int position = 0; position < blocks.Length; position += BlockBytes)
            sum ^= Vector128.LoadUnsafe(ref source, (nuint)position);

        sum.CopyTo(accumulator);
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
