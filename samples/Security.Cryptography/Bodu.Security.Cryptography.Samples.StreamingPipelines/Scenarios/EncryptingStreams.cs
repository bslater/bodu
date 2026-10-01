// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncryptingStreams.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines.Scenarios;

/// <summary>
/// Encrypts and decrypts a 200 KiB payload stream to stream with the synchronous extension overloads: Twofish in
/// CBC mode through <c>SymmetricAlgorithmExtensions</c>, and XChaCha20 through
/// <c>SymmetricStreamAlgorithmExtensions</c>. Shows that neither the read buffer size nor the choice between the
/// stream and array overloads can change a byte of the ciphertext, and that a stream-cipher instance refuses to reuse
/// its nonce for a second message.
/// </summary>
public static class EncryptingStreams
{
    /// <summary>
    /// Round-trips the payload through both ciphers stream to stream and compares the alternative routes.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Encrypting streams (Twofish CBC and XChaCha20)",
            what: "Encrypts a 200 KiB payload from one stream into another with Encrypt(Stream, Stream), decrypts it back with Decrypt(Stream, Stream), and compares the ciphertext against the byte[] overload and against a run with a 4 KiB buffer instead of the 80 KiB default. Repeats the stream round trip with the XChaCha20 stream cipher, and asks the same XChaCha20 instance to encrypt a second message.",
            why: "A file, an HTTP body, or a blob download should not have to fit in memory to be encrypted. The stream overloads create the transform from the algorithm's current Key, IV, and mode, pump the source through it in bounded reads, and return how many bytes they read, so the caller keeps ownership of both streams. That only works if the read size is invisible in the output, which is what the comparisons check. A stream cipher adds one more rule: a key and nonce pair must never encrypt two messages, because both would be XORed with the same keystream, and the instance enforces it.",
            expect: "Twofish reads all 204800 bytes and writes 204816, because PKCS#7 adds a whole block when the input is already block-aligned. XChaCha20 reads and writes 204800 bytes, because a stream cipher never pads, and refuses the second message with a CryptographicException. Both comparisons print True and both decryptions recover the payload exactly.");

        var payload = SamplePayload.Create();

        // Twofish through the SymmetricAlgorithm extensions. The transform is built from Key, IV, BlockMode and
        // BlockPadding at the moment of the call, so configure the instance first and reuse it for every call.
        using var twofish = new Twofish
        {
            Key = SamplePayload.Key,
            IV = SamplePayload.Iv,
            BlockMode = CipherModeKind.CBC,
            BlockPadding = PaddingModeKind.PKCS7,
        };

        using var plaintextIn = new MemoryStream(payload);
        using var ciphertextOut = new MemoryStream();
        var bytesRead = twofish.Encrypt(plaintextIn, ciphertextOut);
        var ciphertext = ciphertextOut.ToArray();

        // The same message through the array overload, and through the stream overload with a 4 KiB buffer.
        var viaArray = twofish.Encrypt(payload);

        using var smallBufferIn = new MemoryStream(payload);
        using var smallBufferOut = new MemoryStream();
        twofish.Encrypt(smallBufferIn, smallBufferOut, bufferSize: 4096);

        // Decrypt(Stream, Stream) returns the number of ciphertext bytes it read.
        using var ciphertextIn = new MemoryStream(ciphertext);
        using var recoveredOut = new MemoryStream();
        var ciphertextRead = twofish.Decrypt(ciphertextIn, recoveredOut);

        Console.WriteLine("  Twofish-256 CBC/PKCS7");
        Console.WriteLine($"    Encrypt(Stream, Stream) : read {bytesRead} B, wrote {ciphertext.Length} B, ct {Hex.ToShortHex(ciphertext)}");
        Console.WriteLine($"    == Encrypt(byte[])      : {viaArray.AsSpan().SequenceEqual(ciphertext)}");
        Console.WriteLine($"    == 4 KiB buffer         : {smallBufferOut.ToArray().AsSpan().SequenceEqual(ciphertext)}");
        Console.WriteLine($"    Decrypt(Stream, Stream) : read {ciphertextRead} B, recovers payload: {recoveredOut.ToArray().AsSpan().SequenceEqual(payload)}");
        Console.WriteLine();

        // XChaCha20 is a SymmetricStreamAlgorithm, not a SymmetricAlgorithm, so the same call shape binds to
        // SymmetricStreamAlgorithmExtensions. The keystream is XORed over the data, so the length never changes.
        using var encryptor = new XChaCha20 { Key = SamplePayload.Key, Nonce = SamplePayload.Nonce };

        using var streamPlainIn = new MemoryStream(payload);
        using var streamCipherOut = new MemoryStream();
        var streamRead = encryptor.Encrypt(streamPlainIn, streamCipherOut);
        var streamCiphertext = streamCipherOut.ToArray();

        // A stream-cipher instance latches its nonce as used once it has issued a transform, so a second message
        // under the same key and nonce - which would reuse the keystream - is refused rather than encrypted.
        string secondMessage;
        try
        {
            encryptor.Encrypt(payload);
            secondMessage = "encrypted";
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            secondMessage = "refused (CryptographicException: nonce already used)";
        }

        // The decryptor is a separate instance holding the same key and nonce, as the receiving side would be.
        using var decryptor = new XChaCha20 { Key = SamplePayload.Key, Nonce = SamplePayload.Nonce };
        using var streamCipherIn = new MemoryStream(streamCiphertext);
        using var streamPlainOut = new MemoryStream();
        decryptor.Decrypt(streamCipherIn, streamPlainOut);

        Console.WriteLine("  XChaCha20");
        Console.WriteLine($"    Encrypt(Stream, Stream) : read {streamRead} B, wrote {streamCiphertext.Length} B, ct {Hex.ToShortHex(streamCiphertext)}");
        Console.WriteLine($"    2nd Encrypt, same nonce : {secondMessage}");
        Console.WriteLine($"    Decrypt(Stream, Stream) : recovers payload: {streamPlainOut.ToArray().AsSpan().SequenceEqual(payload)}");

        Console.WriteLine();
    }
}
