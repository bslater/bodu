// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SealedFilePipeline.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines.Scenarios;

/// <summary>
/// Composes the stream extensions into an encrypt-then-MAC sealed file: HKDF splits one master key into an
/// encryption key and a MAC key, <c>EncryptAsync</c> streams the plaintext into the container, keyed BLAKE2b
/// authenticates the IV and ciphertext through <c>AppendData</c> / <c>AppendDataAsync</c>, and the reader checks the
/// tag with <c>VerifyHashAsync</c> before <c>TryCreateDecryptor</c> and <c>TransformAsync</c> decrypt anything.
/// </summary>
/// <remarks>
/// The container layout is <c>tag (32 B) | IV (16 B) | ciphertext</c>. The tag comes first so the reader can verify
/// everything after it in one pass over the stream, and the IV sits inside the authenticated region so it cannot be
/// swapped either.
/// </remarks>
public static class SealedFilePipeline
{
    /// <summary>The length of the BLAKE2b-256 tag, in bytes.</summary>
    private const int TagLength = 32;

    /// <summary>The length of the Twofish CBC IV, in bytes.</summary>
    private const int IvLength = 16;

    /// <summary>The fixed master key; HKDF derives the two working keys from it.</summary>
    private static readonly byte[] MasterKey = SamplePayload.Fill(32, 0x70);

    /// <summary>
    /// Seals the payload into a container, opens it, then shows a tampered container and a bad key being refused.
    /// </summary>
    /// <returns>A task that completes when the scenario has printed its results.</returns>
    public static async Task RunAsync()
    {
        SampleConsole.Scenario(
            "Sealing a large file: encrypt-then-MAC over streams",
            what: "Derives separate encryption and MAC keys from one master key with HKDF, seals the 200 KiB payload into a tag | IV | ciphertext container with EncryptAsync and a keyed BLAKE2b tag, opens it again, then opens a copy with one ciphertext bit flipped and finally tries to build a cipher from a misconfigured 20-byte key.",
            why: "For a message that fits in memory an AEAD is the right tool. A multi-gigabyte backup or export does not fit, and the stream extensions compose into the standard large-file shape instead: encrypt as you stream, MAC the IV and ciphertext with an independent key, and on the way back verify the whole tag before decrypting a single byte, so a tampered file never reaches the padding check or the application. TryCreateDecryptor turns bad key material from configuration into a false result rather than an exception thrown deep in the pipeline.",
            expect: "The container is 48 bytes longer than the padded ciphertext (tag plus IV), the tag verifies, and the payload comes back exactly. The tampered copy fails the tag check, so nothing is decrypted. The 20-byte key is refused by TryCreateEncryptor, because Twofish accepts only 16, 24, or 32 bytes.");

        var payload = SamplePayload.Create();

        // Two independent keys from one secret: the info string binds each key to its single purpose, so the
        // cipher key is never also used as a MAC key.
        var encryptionKey = Hkdf.DeriveKey(HashAlgorithmName.SHA256, MasterKey, 32, info: "sealed-file v1 encryption"u8);
        var macKey = Hkdf.DeriveKey(HashAlgorithmName.SHA256, MasterKey, 32, info: "sealed-file v1 mac"u8);

        // Fixed for a reproducible transcript; a real writer draws a fresh random IV for every file.
        var iv = SamplePayload.Fill(IvLength, 0x50);

        using var container = new MemoryStream();
        using (var plaintext = new MemoryStream(payload))
            await SealAsync(plaintext, container, encryptionKey, macKey, iv).ConfigureAwait(false);

        var sealedBytes = container.ToArray();
        Console.WriteLine($"  sealed       : {payload.Length} B plaintext -> {sealedBytes.Length} B container (tag {Hex.ToShortHex(sealedBytes.AsSpan(0, TagLength))})");

        using (var recovered = new MemoryStream())
        {
            var opened = await TryOpenAsync(new MemoryStream(sealedBytes), recovered, encryptionKey, macKey).ConfigureAwait(false);
            Console.WriteLine($"  opened       : tag verifies {opened}, recovers payload {recovered.ToArray().AsSpan().SequenceEqual(payload)}");
        }

        // Flip one bit in the middle of the ciphertext. The tag check fails, so decryption is never attempted.
        var tampered = (byte[])sealedBytes.Clone();
        tampered[TagLength + IvLength + 100_000] ^= 0x01;
        using (var recovered = new MemoryStream())
        {
            var opened = await TryOpenAsync(new MemoryStream(tampered), recovered, encryptionKey, macKey).ConfigureAwait(false);
            Console.WriteLine($"  tampered     : tag verifies {opened}, bytes decrypted {recovered.Length}");
        }

        // Key material from configuration is input like any other. TryCreateEncryptor reports a key Twofish cannot
        // use as false, where CreateEncryptor would throw.
        using var twofish = new Twofish { BlockMode = CipherModeKind.CBC, BlockPadding = PaddingModeKind.PKCS7 };
        var misconfiguredKey = SamplePayload.Fill(20, 0x01);
        var created = twofish.TryCreateEncryptor(misconfiguredKey, iv, out var refused);
        refused?.Dispose();
        Console.WriteLine($"  20-byte key  : TryCreateEncryptor -> {created}, transform is null: {refused is null}");

        Console.WriteLine();
    }

    /// <summary>
    /// Writes <paramref name="plaintext" /> into <paramref name="container" /> as <c>tag | IV | ciphertext</c>.
    /// </summary>
    /// <param name="plaintext">The stream to seal, read to its end.</param>
    /// <param name="container">The seekable stream that receives the container.</param>
    /// <param name="encryptionKey">The Twofish key.</param>
    /// <param name="macKey">The keyed BLAKE2b key.</param>
    /// <param name="iv">The CBC IV for this container.</param>
    /// <returns>A task that completes when the container has been written.</returns>
    private static async Task SealAsync(Stream plaintext, Stream container, byte[] encryptionKey, byte[] macKey, byte[] iv)
    {
        // Reserve the tag slot, write the IV, then stream the ciphertext in after it.
        container.Write(new byte[TagLength]);
        container.Write(iv);

        using (var twofish = new Twofish { Key = encryptionKey, IV = iv, BlockMode = CipherModeKind.CBC, BlockPadding = PaddingModeKind.PKCS7 })
            await twofish.EncryptAsync(plaintext, container).ConfigureAwait(false);

        // MAC the IV from memory and the ciphertext from the stream into one tag, then fill in the reserved slot.
        using var mac = new Blake2b(256) { Key = macKey };
        mac.AppendData(iv);
        container.Position = TagLength + IvLength;
        await mac.AppendDataAsync(container).ConfigureAwait(false);
        mac.TransformFinalBlock([], 0, 0);

        container.Position = 0;
        container.Write(mac.Hash!);
    }

    /// <summary>
    /// Verifies a container's tag and, only if it is authentic, decrypts its ciphertext into <paramref name="output" />.
    /// </summary>
    /// <param name="container">The container stream; disposed when the call completes.</param>
    /// <param name="output">The stream that receives the plaintext.</param>
    /// <param name="encryptionKey">The Twofish key.</param>
    /// <param name="macKey">The keyed BLAKE2b key.</param>
    /// <returns><see langword="true" /> when the tag verified and the payload was decrypted; otherwise <see langword="false" />.</returns>
    private static async Task<bool> TryOpenAsync(Stream container, Stream output, byte[] encryptionKey, byte[] macKey)
    {
        await using (container.ConfigureAwait(false))
        {
            var tag = new byte[TagLength];
            var iv = new byte[IvLength];
            container.ReadExactly(tag);
            container.ReadExactly(iv);

            // Verify everything after the tag - the IV and the ciphertext - in one constant-time check.
            container.Position = TagLength;
            using (var mac = new Blake2b(256) { Key = macKey })
            {
                if (!await mac.VerifyHashAsync(container, tag).ConfigureAwait(false))
                    return false;
            }

            // Only now build a decryptor. The IV came from the file, so creation is attempted, not assumed.
            using var twofish = new Twofish { BlockMode = CipherModeKind.CBC, BlockPadding = PaddingModeKind.PKCS7 };
            if (!twofish.TryCreateDecryptor(encryptionKey, iv, out var decryptor))
                return false;

            using (decryptor)
            {
                container.Position = TagLength + IvLength;
                await decryptor!.TransformAsync(container, output, bufferSize: 64 * 1024).ConfigureAwait(false);
            }

            return true;
        }
    }
}
