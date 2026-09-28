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
/// transforms. Composes a ChaCha20 or Salsa20 keystream with the one-time Poly1305 MAC in two distinct constructions:
/// the RFC 8439 framing (used by XChaCha20-Poly1305 and the IETF-style XSalsa20-Poly1305) and the NaCl
/// <c>crypto_secretbox</c> framing (used by XSalsa20-Poly1305).
/// </summary>
/// <remarks>
/// <para>
/// Each framing is generic over an <see cref="IKeystreamSource" /> passed by reference, so that the AEADs in this
/// library draw their keystream from a <see cref="ChaCha20Core.Keystream" /> or <see cref="Salsa20Core.Keystream" /> on
/// the stack and a message allocates nothing; an overload taking an <see cref="IStreamCipher" /> engine serves the
/// engines a derived <see cref="Poly1305AeadTransform" /> creates. The authenticator is a <see cref="Poly1305Core" />
/// on the stack in both cases.
/// </para>
/// <para>
/// The keystream passed to every method must be positioned at block counter 0. The first 32 bytes of the counter-0
/// keystream block always form the one-time Poly1305 key; how the remainder of that block and the subsequent blocks are
/// consumed is what distinguishes the two framings:
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
internal static partial class Poly1305AeadCore
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
    /// Encrypts <paramref name="plaintext" /> under the RFC 8439 ChaCha20-Poly1305 / XChaCha20-Poly1305 framing,
    /// drawing the keystream from an engine, and appends the authentication tag.
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
        var keystream = new EngineKeystream(engine);
        return SealRfc8439(ref keystream, associatedData, plaintext, output);
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under the RFC 8439 ChaCha20-Poly1305 / XChaCha20-Poly1305 framing and
    /// appends the authentication tag.
    /// </summary>
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">
    /// A keystream positioned at block counter 0, advanced past every block this call uses.
    /// </param>
    /// <param name="associatedData">The associated data authenticated alongside the ciphertext.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">Receives the ciphertext followed by the <see cref="TagBytes" />-byte tag.</param>
    /// <returns>The number of bytes written: <c>plaintext.Length + <see cref="TagBytes" /></c>.</returns>
    internal static int SealRfc8439<TKeystream>(
        ref TKeystream keystream,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> plaintext,
        Span<byte> output)
        where TKeystream : struct, IKeystreamSource
    {
        ValidateSealBuffers(plaintext, output);

        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];

        try
        {
            DerivePoly1305KeyDiscardingBlock(ref keystream, poly1305Key);

            Span<byte> ciphertext = output[..plaintext.Length];
            XorKeystream(ref keystream, plaintext, ciphertext);

            ComputeRfc8439Tag(poly1305Key, associatedData, ciphertext, output.Slice(plaintext.Length, TagBytes));

            return plaintext.Length + TagBytes;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(poly1305Key);
        }
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> under the RFC 8439 framing, drawing the keystream
    /// from an engine.
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
        var keystream = new EngineKeystream(engine);
        return OpenRfc8439(ref keystream, associatedData, ciphertextWithTag, output);
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> under the RFC 8439 framing.
    /// </summary>
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">
    /// A keystream positioned at block counter 0, advanced past every block this call uses.
    /// </param>
    /// <param name="associatedData">The associated data that must match what was supplied at encryption time.</param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its <see cref="TagBytes" />-byte tag.</param>
    /// <param name="output">Receives the recovered plaintext.</param>
    /// <returns>The number of plaintext bytes written.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    internal static int OpenRfc8439<TKeystream>(
        ref TKeystream keystream,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> ciphertextWithTag,
        Span<byte> output)
        where TKeystream : struct, IKeystreamSource
    {
        ValidateOpenBuffers(ciphertextWithTag, output);

        int ciphertextLength = ciphertextWithTag.Length - TagBytes;
        ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..ciphertextLength];
        ReadOnlySpan<byte> receivedTag = ciphertextWithTag[ciphertextLength..];

        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];
        Span<byte> expectedTag = stackalloc byte[TagBytes];

        try
        {
            DerivePoly1305KeyDiscardingBlock(ref keystream, poly1305Key);
            ComputeRfc8439Tag(poly1305Key, associatedData, ciphertext, expectedTag);

            if (!CryptographicOperations.FixedTimeEquals(receivedTag, expectedTag))
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);

            XorKeystream(ref keystream, ciphertext, output[..ciphertextLength]);

            return ciphertextLength;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(poly1305Key);
            CryptographicOperations.ZeroMemory(expectedTag);
        }
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under the NaCl <c>crypto_secretbox</c> (XSalsa20-Poly1305) framing,
    /// drawing the keystream from an engine, and appends the authentication tag.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0. Consumed in full by this call.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">Receives the ciphertext followed by the <see cref="TagBytes" />-byte tag.</param>
    /// <returns>The number of bytes written: <c>plaintext.Length + <see cref="TagBytes" /></c>.</returns>
    internal static int SealSecretbox(IStreamCipher engine, ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        var keystream = new EngineKeystream(engine);
        return SealSecretbox(ref keystream, plaintext, output);
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> under the NaCl <c>crypto_secretbox</c> (XSalsa20-Poly1305) framing and
    /// appends the authentication tag.
    /// </summary>
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">
    /// A keystream positioned at block counter 0, advanced past every block this call uses.
    /// </param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">Receives the ciphertext followed by the <see cref="TagBytes" />-byte tag.</param>
    /// <returns>The number of bytes written: <c>plaintext.Length + <see cref="TagBytes" /></c>.</returns>
    internal static int SealSecretbox<TKeystream>(ref TKeystream keystream, ReadOnlySpan<byte> plaintext, Span<byte> output)
        where TKeystream : struct, IKeystreamSource
    {
        ValidateSealBuffers(plaintext, output);

        Span<byte> block0 = stackalloc byte[KeystreamBlockBytes];
        Span<byte> poly1305Key = stackalloc byte[Poly1305KeyBytes];

        try
        {
            keystream.NextBlock(block0);
            block0[..Poly1305KeyBytes].CopyTo(poly1305Key);

            Span<byte> ciphertext = output[..plaintext.Length];
            EncryptSecretboxBody(ref keystream, block0, plaintext, ciphertext);

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
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> under the NaCl <c>crypto_secretbox</c> framing,
    /// drawing the keystream from an engine.
    /// </summary>
    /// <param name="engine">A keystream engine positioned at block counter 0. Consumed in full by this call.</param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its <see cref="TagBytes" />-byte tag.</param>
    /// <param name="output">Receives the recovered plaintext.</param>
    /// <returns>The number of plaintext bytes written.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    internal static int OpenSecretbox(IStreamCipher engine, ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        var keystream = new EngineKeystream(engine);
        return OpenSecretbox(ref keystream, ciphertextWithTag, output);
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> under the NaCl <c>crypto_secretbox</c> framing.
    /// </summary>
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">
    /// A keystream positioned at block counter 0, advanced past every block this call uses.
    /// </param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its <see cref="TagBytes" />-byte tag.</param>
    /// <param name="output">Receives the recovered plaintext.</param>
    /// <returns>The number of plaintext bytes written.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    internal static int OpenSecretbox<TKeystream>(ref TKeystream keystream, ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
        where TKeystream : struct, IKeystreamSource
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
            keystream.NextBlock(block0);
            block0[..Poly1305KeyBytes].CopyTo(poly1305Key);

            ComputePoly1305(poly1305Key, ciphertext, expectedTag);

            if (!CryptographicOperations.FixedTimeEquals(receivedTag, expectedTag))
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);

            EncryptSecretboxBody(ref keystream, block0, ciphertext, output[..ciphertextLength]);

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
    /// discards the remainder — the RFC 8439 key-derivation step that leaves the keystream positioned at counter 1.
    /// </summary>
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">A keystream positioned at block counter 0.</param>
    /// <param name="poly1305Key">A 32-byte span that receives the one-time Poly1305 key.</param>
    private static void DerivePoly1305KeyDiscardingBlock<TKeystream>(ref TKeystream keystream, Span<byte> poly1305Key)
        where TKeystream : struct, IKeystreamSource
    {
        Span<byte> block0 = stackalloc byte[KeystreamBlockBytes];

        try
        {
            keystream.NextBlock(block0);
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
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">A keystream positioned at block counter 1 (its counter-0 block already read).</param>
    /// <param name="block0">The already-read counter-0 keystream block.</param>
    /// <param name="input">The plaintext (when sealing) or ciphertext (when opening).</param>
    /// <param name="output">Receives the XOR of <paramref name="input" /> with the keystream.</param>
    private static void EncryptSecretboxBody<TKeystream>(
        ref TKeystream keystream,
        ReadOnlySpan<byte> block0,
        ReadOnlySpan<byte> input,
        Span<byte> output)
        where TKeystream : struct, IKeystreamSource
    {
        int head = Math.Min(SecretboxKeystreamOffset, input.Length);
        CryptographyHelper.Xor(input[..head], block0.Slice(SecretboxKeystreamOffset, head), output[..head]);

        if (input.Length > SecretboxKeystreamOffset)
            XorKeystream(ref keystream, input[SecretboxKeystreamOffset..], output[SecretboxKeystreamOffset..]);
    }

    /// <summary>
    /// XORs <paramref name="input" /> against the successive keystream blocks of <paramref name="keystream" />, writing
    /// the result to <paramref name="output" />.
    /// </summary>
    /// <typeparam name="TKeystream">The type of the keystream.</typeparam>
    /// <param name="keystream">The keystream to advance.</param>
    /// <param name="input">The data to combine with the keystream.</param>
    /// <param name="output">Receives the XOR result; must be at least <c>input.Length</c> bytes.</param>
    /// <remarks>
    /// The whole blocks go to <see cref="IKeystreamSource.XorBlocks" /> in one call; a partial last block takes one
    /// more keystream block, of which the unused tail is discarded.
    /// </remarks>
    private static void XorKeystream<TKeystream>(ref TKeystream keystream, ReadOnlySpan<byte> input, Span<byte> output)
        where TKeystream : struct, IKeystreamSource
    {
        int whole = input.Length / KeystreamBlockBytes * KeystreamBlockBytes;
        if (whole > 0)
            keystream.XorBlocks(input[..whole], output);

        if (whole == input.Length)
            return;

        Span<byte> block = stackalloc byte[KeystreamBlockBytes];

        try
        {
            keystream.NextBlock(block);

            int tail = input.Length - whole;
            CryptographyHelper.Xor(input[whole..], block[..tail], output.Slice(whole, tail));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(block);
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
        // The authenticator lives on the stack and reads the caller's buffers in place; padding each segment to 16
        // bytes happens inside it, so nothing is copied or allocated per message.
        Poly1305Core mac = default;

        try
        {
            mac.Initialize(poly1305Key);
            mac.UpdatePadded(associatedData);
            mac.UpdatePadded(ciphertext);

            // Final 16-byte little-endian length block completes the 16-aligned MAC input.
            Span<byte> lengths = stackalloc byte[Poly1305Core.BlockBytes];
            BinaryPrimitives.WriteUInt64LittleEndian(lengths, (ulong)associatedData.Length);
            BinaryPrimitives.WriteUInt64LittleEndian(lengths.Slice(sizeof(ulong)), (ulong)ciphertext.Length);
            mac.Update(lengths);

            mac.Finish(tag);
        }
        finally
        {
            mac.Clear();
        }
    }

    /// <summary>
    /// Computes a one-time Poly1305 tag over <paramref name="data" /> using <paramref name="poly1305Key" />.
    /// </summary>
    /// <param name="poly1305Key">The 32-byte one-time Poly1305 key.</param>
    /// <param name="data">The message authenticated by the MAC.</param>
    /// <param name="tag">A 16-byte span that receives the computed tag.</param>
    private static void ComputePoly1305(ReadOnlySpan<byte> poly1305Key, ReadOnlySpan<byte> data, Span<byte> tag)
    {
        // The authenticator lives on the stack and reads the caller's buffer in place.
        Poly1305Core mac = default;

        try
        {
            mac.Initialize(poly1305Key);
            mac.Update(data);
            mac.Finish(tag);
        }
        finally
        {
            mac.Clear();
        }
    }

    /// <summary>
    /// Validates that <paramref name="output" /> can hold the ciphertext plus tag for a sealing operation.
    /// </summary>
    /// <param name="plaintext">The plaintext to be encrypted.</param>
    /// <param name="output">The destination buffer.</param>
    /// <exception cref="ArgumentException"><paramref name="output" /> is too small.</exception>
    private static void ValidateSealBuffers(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        int required = checked(plaintext.Length + TagBytes);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, required);
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

        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, ciphertextWithTag.Length - TagBytes);
    }
}
