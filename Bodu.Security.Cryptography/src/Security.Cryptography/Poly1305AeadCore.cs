// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the Poly1305-based authenticated-encryption framing shared by the ChaCha20- and Salsa20-family AEAD
/// transforms. Composes an <see cref="IStreamCipher" /> keystream engine with the one-time <see cref="Poly1305" /> MAC
/// in two distinct constructions: the RFC 8439 framing (used by XChaCha20-Poly1305 and the IETF-style
/// XSalsa20-Poly1305) and the NaCl <c>crypto_secretbox</c> framing (used by XSalsa20-Poly1305).
/// </summary>
/// <remarks>
/// <para>
/// The engine passed to every method must be freshly constructed and positioned at block counter 0. The first 32 bytes
/// of the counter-0 keystream block always form the one-time Poly1305 key; how the remainder of that block and the
/// subsequent blocks are consumed is what distinguishes the two framings:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>RFC 8439</b> (<see cref="SealRfc8439" /> / <see cref="OpenRfc8439" />): the rest of the counter-0 block is
/// discarded and the message is encrypted with keystream blocks starting at counter 1. The MAC is computed over
/// <c>AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>secretbox</b> (<see cref="SealSecretbox" /> / <see cref="OpenSecretbox" />): the trailing 32 bytes of the
/// counter-0 block encrypt the first 32 message bytes, and the message continues into the counter-1 block. There is no
/// associated data; the MAC is computed over the ciphertext alone.
/// </description>
/// </item>
/// </list>
/// <para>
/// Both decryption paths are <em>verify-before-release</em>: the tag is recomputed over the received ciphertext and
/// compared in constant time before any plaintext byte is written, so a failed authentication leaves <c>output</c>
/// untouched.
/// </para>
/// </remarks>
internal static class Poly1305AeadCore
{
    /// <summary>Length of the Poly1305 authentication tag, in bytes (128 bits).</summary>
    internal const int TagBytes = 16;

    /// <summary>Length of the one-time Poly1305 key, in bytes (256 bits).</summary>
    private const int Poly1305KeyBytes = 32;

    /// <summary>Keystream block length, in bytes, of the ChaCha20 and Salsa20 engines (512 bits).</summary>
    private const int KeystreamBlockBytes = 64;

    /// <summary>Offset, in bytes, into the counter-0 keystream block at which the secretbox message keystream begins. Equal to the Poly1305 key length because the key occupies the leading 32 bytes of that block.</summary>
    private const int SecretboxKeystreamOffset = Poly1305KeyBytes;

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under the RFC 8439 ChaCha20-Poly1305 / XChaCha20-Poly1305 framing and
    /// appends the authentication tag.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0. Consumed in full by this call.</param>
    /// <param name="associatedData">The associated data authenticated alongside the ciphertext.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">Receives the ciphertext followed by the <see cref="TagBytes" />-byte tag.</param>
    /// <returns>The number of bytes written: <c>plaintext.Length + <see cref="TagBytes" /></c>.</returns>
    internal static int SealRfc8439(
        IStreamCipher engine,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> plaintext,
        Span<byte> output)
    {
        ValidateSealBuffers(plaintext, output);

        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];

        try
        {
            DerivePoly1305KeyDiscardingBlock(engine, poly1305Key);

            Span<byte> ciphertext = output[..plaintext.Length];
            XorKeystream(engine, plaintext, ciphertext);

            ComputeRfc8439Tag(poly1305Key, associatedData, ciphertext, output.Slice(plaintext.Length, TagBytes));

            return plaintext.Length + TagBytes;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(poly1305Key);
        }
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> under the RFC 8439 framing.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0. Consumed in full by this call.</param>
    /// <param name="associatedData">The associated data that must match what was supplied at encryption time.</param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its <see cref="TagBytes" />-byte tag.</param>
    /// <param name="output">Receives the recovered plaintext.</param>
    /// <returns>The number of plaintext bytes written.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    internal static int OpenRfc8439(
        IStreamCipher engine,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> ciphertextWithTag,
        Span<byte> output)
    {
        ValidateOpenBuffers(ciphertextWithTag, output);

        int ciphertextLength = ciphertextWithTag.Length - TagBytes;
        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..ciphertextLength];
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag[ciphertextLength..];

        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];
        Span<byte> expectedTag = stackalloc byte[TagBytes];

        try
        {
            DerivePoly1305KeyDiscardingBlock(engine, poly1305Key);
            ComputeRfc8439Tag(poly1305Key, associatedData, ciphertext, expectedTag);

            if (!CryptographicOperations.FixedTimeEquals(receivedTag, expectedTag))
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);

            XorKeystream(engine, ciphertext, output[..ciphertextLength]);

            return ciphertextLength;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(poly1305Key);
            CryptographicOperations.ZeroMemory(expectedTag);
        }
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under the NaCl <c>crypto_secretbox</c> (XSalsa20-Poly1305) framing and
    /// appends the authentication tag.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0. Consumed in full by this call.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">Receives the ciphertext followed by the <see cref="TagBytes" />-byte tag.</param>
    /// <returns>The number of bytes written: <c>plaintext.Length + <see cref="TagBytes" /></c>.</returns>
    internal static int SealSecretbox(IStreamCipher engine, ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        ValidateSealBuffers(plaintext, output);

        Span<byte> block0 = stackalloc byte[KeystreamBlockBytes];
        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];

        try
        {
            engine.NextKeystreamBlock(block0);
            block0[..Poly1305KeyBytes].CopyTo(poly1305Key);

            Span<byte> ciphertext = output[..plaintext.Length];
            EncryptSecretboxBody(engine, block0, plaintext, ciphertext);

            ComputePoly1305(poly1305Key, ciphertext, output.Slice(plaintext.Length, TagBytes));

            return plaintext.Length + TagBytes;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block0);
            CryptographicOperations.ZeroMemory(poly1305Key);
        }
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> under the NaCl <c>crypto_secretbox</c> framing.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0. Consumed in full by this call.</param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its <see cref="TagBytes" />-byte tag.</param>
    /// <param name="output">Receives the recovered plaintext.</param>
    /// <returns>The number of plaintext bytes written.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    internal static int OpenSecretbox(IStreamCipher engine, ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        ValidateOpenBuffers(ciphertextWithTag, output);

        int ciphertextLength = ciphertextWithTag.Length - TagBytes;
        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..ciphertextLength];
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag[ciphertextLength..];

        Span<byte> block0 = stackalloc byte[KeystreamBlockBytes];
        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];
        Span<byte> expectedTag = stackalloc byte[TagBytes];

        try
        {
            engine.NextKeystreamBlock(block0);
            block0[..Poly1305KeyBytes].CopyTo(poly1305Key);

            ComputePoly1305(poly1305Key, ciphertext, expectedTag);

            if (!CryptographicOperations.FixedTimeEquals(receivedTag, expectedTag))
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);

            EncryptSecretboxBody(engine, block0, ciphertext, output[..ciphertextLength]);

            return ciphertextLength;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block0);
            CryptographicOperations.ZeroMemory(poly1305Key);
            CryptographicOperations.ZeroMemory(expectedTag);
        }
    }

    /// <summary>
    /// Reads the counter-0 keystream block, copies its leading 32 bytes into <paramref name="poly1305Key" />, and
    /// discards the remainder — the RFC 8439 key-derivation step that leaves the engine positioned at counter 1.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0.</param>
    /// <param name="poly1305Key">A 32-byte span that receives the one-time Poly1305 key.</param>
    private static void DerivePoly1305KeyDiscardingBlock(IStreamCipher engine, Span<byte> poly1305Key)
    {
        Span<byte> block0 = stackalloc byte[KeystreamBlockBytes];

        try
        {
            engine.NextKeystreamBlock(block0);
            block0[..Poly1305KeyBytes].CopyTo(poly1305Key);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block0);
        }
    }

    /// <summary>
    /// XORs the secretbox message against the keystream, using the trailing 32 bytes of the counter-0 block for the
    /// first 32 message bytes and full counter-1+ blocks for the remainder.
    /// </summary>
    /// <param name="engine">
    /// A keystream engine positioned at block counter 1 (its counter-0 block already read).
    /// </param>
    /// <param name="block0">The already-read counter-0 keystream block.</param>
    /// <param name="input">The plaintext (when sealing) or ciphertext (when opening).</param>
    /// <param name="output">Receives the XOR of <paramref name="input" /> with the keystream.</param>
    private static void EncryptSecretboxBody(
        IStreamCipher engine,
        ReadOnlySpan<byte> block0,
        ReadOnlySpan<byte> input,
        Span<byte> output)
    {
        int head = Math.Min(SecretboxKeystreamOffset, input.Length);
        for (int i = 0; i < head; i++)
            output[i] = (byte)(input[i] ^ block0[SecretboxKeystreamOffset + i]);

        if (input.Length > SecretboxKeystreamOffset)
            XorKeystream(engine, input[SecretboxKeystreamOffset..], output[SecretboxKeystreamOffset..]);
    }

    /// <summary>
    /// XORs <paramref name="input" /> against successive full keystream blocks drawn from <paramref name="engine" />,
    /// writing the result to <paramref name="output" />.
    /// </summary>
    /// <param name="engine">The keystream engine to advance.</param>
    /// <param name="input">The data to combine with the keystream.</param>
    /// <param name="output">Receives the XOR result; must be at least <c>input.Length</c> bytes.</param>
    private static void XorKeystream(IStreamCipher engine, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> keystream = stackalloc byte[KeystreamBlockBytes];

        try
        {
            int offset = 0;
            while (offset < input.Length)
            {
                engine.NextKeystreamBlock(keystream);

                int count = Math.Min(KeystreamBlockBytes, input.Length - offset);
                for (int i = 0; i < count; i++)
                    output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);

                offset += count;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keystream);
        }
    }

    /// <summary>
    /// Computes the RFC 8439 authentication tag over
    /// <c>AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)</c>.
    /// </summary>
    /// <param name="poly1305Key">The one-time Poly1305 key.</param>
    /// <param name="associatedData">The associated data.</param>
    /// <param name="ciphertext">The ciphertext to authenticate.</param>
    /// <param name="tag">A 16-byte span that receives the computed tag.</param>
    private static void ComputeRfc8439Tag(
        ReadOnlySpan<byte> poly1305Key,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> tag)
    {
        // Stream the MAC input through Poly1305 in bounded chunks rather than materialising the entire
        // AAD || pad || ciphertext || pad || lengths buffer. The block-hash base buffers residual bytes across
        // TransformBlock calls, so the ProcessBlock sequence is identical to a single-shot hash over the same
        // bytes — but authenticating a large message no longer needs a second message-sized allocation.
        const int chunkBytes = 4096;

        byte[] keyBuffer = poly1305Key.ToArray();
        byte[] chunk = ArrayPool<byte>.Shared.Rent(chunkBytes);

        try
        {
            using var poly1305 = new Poly1305 { Key = keyBuffer };

            FeedData(poly1305, associatedData, chunk);
            FeedZeros(poly1305, PaddingTo16(associatedData.Length), chunk);
            FeedData(poly1305, ciphertext, chunk);
            FeedZeros(poly1305, PaddingTo16(ciphertext.Length), chunk);

            // Final 16-byte little-endian length block completes the 16-aligned MAC input.
            BinaryPrimitives.WriteUInt64LittleEndian(chunk.AsSpan(0, sizeof(ulong)), (ulong)associatedData.Length);
            BinaryPrimitives.WriteUInt64LittleEndian(chunk.AsSpan(sizeof(ulong), sizeof(ulong)), (ulong)ciphertext.Length);
            poly1305.TransformFinalBlock(chunk, 0, sizeof(ulong) * 2);

            byte[] hash = poly1305.Hash
                ?? throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_HashAlgorithmDidNotProduceValue);
            hash.CopyTo(tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chunk.AsSpan(0, chunkBytes));
            ArrayPool<byte>.Shared.Return(chunk);
            CryptographicOperations.ZeroMemory(keyBuffer);
        }
    }

    /// <summary>
    /// Feeds <paramref name="data" /> into the running <paramref name="poly1305" /> MAC in chunk-sized segments.
    /// </summary>
    /// <param name="poly1305">The MAC accumulating the input.</param>
    /// <param name="data">The data to authenticate.</param>
    /// <param name="chunk">A scratch buffer used to bridge spans to the byte-array transform API.</param>
    private static void FeedData(Poly1305 poly1305, ReadOnlySpan<byte> data, byte[] chunk)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            int count = Math.Min(chunk.Length, data.Length - offset);
            data.Slice(offset, count).CopyTo(chunk);
            poly1305.TransformBlock(chunk, 0, count, null, 0);
            offset += count;
        }
    }

    /// <summary>
    /// Feeds <paramref name="count" /> zero bytes (RFC 8439 <c>pad16</c>) into the running MAC.
    /// </summary>
    /// <param name="poly1305">The MAC accumulating the input.</param>
    /// <param name="count">The number of zero bytes to feed; in the range 0–15.</param>
    /// <param name="chunk">
    /// A scratch buffer, the first <paramref name="count" /> bytes of which are zeroed and fed.
    /// </param>
    private static void FeedZeros(Poly1305 poly1305, int count, byte[] chunk)
    {
        if (count == 0)
            return;

        Array.Clear(chunk, 0, count);
        poly1305.TransformBlock(chunk, 0, count, null, 0);
    }

    /// <summary>
    /// Computes a one-time Poly1305 tag over <paramref name="data" /> using <paramref name="poly1305Key" />.
    /// </summary>
    /// <param name="poly1305Key">The 32-byte one-time Poly1305 key.</param>
    /// <param name="data">The message authenticated by the MAC.</param>
    /// <param name="tag">A 16-byte span that receives the computed tag.</param>
    /// <exception cref="CryptographicException">The MAC failed to produce a tag.</exception>
    private static void ComputePoly1305(ReadOnlySpan<byte> poly1305Key, ReadOnlySpan<byte> data, Span<byte> tag)
    {
        byte[] keyBuffer = poly1305Key.ToArray();

        try
        {
            using var poly1305 = new Poly1305 { Key = keyBuffer };
            if (!poly1305.TryComputeHash(data, tag, out _))
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_HashAlgorithmDidNotProduceValue);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBuffer);
        }
    }

    /// <summary>
    /// Returns the number of zero bytes required to pad <paramref name="length" /> up to the next multiple of 16.
    /// </summary>
    /// <param name="length">The unpadded length, in bytes.</param>
    /// <returns>A value in the range 0–15.</returns>
    private static int PaddingTo16(int length) =>
        (16 - (length & 15)) & 15;

    /// <summary>
    /// Validates that <paramref name="output" /> can hold the ciphertext plus tag for a sealing operation.
    /// </summary>
    /// <param name="plaintext">The plaintext to be encrypted.</param>
    /// <param name="output">The destination buffer.</param>
    /// <exception cref="ArgumentException"><paramref name="output" /> is too small.</exception>
    private static void ValidateSealBuffers(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        int required = checked(plaintext.Length + TagBytes);
        if (output.Length < required)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, required),
                nameof(output));
        }
    }

    /// <summary>
    /// Validates that <paramref name="ciphertextWithTag" /> is at least tag-sized and that <paramref name="output" />
    /// can hold the recovered plaintext for an opening operation.
    /// </summary>
    /// <param name="ciphertextWithTag">The ciphertext followed by its tag.</param>
    /// <param name="output">The destination buffer.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextWithTag" /> is shorter than the tag, or <paramref name="output" /> is too small.
    /// </exception>
    private static void ValidateOpenBuffers(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        if (ciphertextWithTag.Length < TagBytes)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, TagBytes),
                nameof(ciphertextWithTag));
        }

        if (output.Length < ciphertextWithTag.Length - TagBytes)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, ciphertextWithTag.Length - TagBytes),
                nameof(output));
        }
    }
}
