// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncAndBclInterop.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines.Scenarios;

/// <summary>
/// Runs the awaitable cipher extensions - <c>EncryptAsync</c>, <c>DecryptAsync</c>, and
/// <c>ICryptoTransform.TransformAsync</c> - and shows that they are defined over the BCL abstractions, so they apply
/// unchanged to the framework's own <see cref="Aes" /> and agree byte for byte with its one-shot API.
/// </summary>
public static class AsyncAndBclInterop
{
    /// <summary>
    /// Encrypts asynchronously, demonstrates cancellation, and cross-checks the extensions against <see cref="Aes" />.
    /// </summary>
    /// <returns>A task that completes when the scenario has printed its results.</returns>
    public static async Task RunAsync()
    {
        SampleConsole.Scenario(
            "Async pipelines and the BCL's own Aes",
            what: "Encrypts and decrypts the 200 KiB payload with EncryptAsync / DecryptAsync, tries again with an already-cancelled token, then drives a System.Security.Cryptography Aes instance through the same Encrypt(Stream, Stream) extension, through TransformAsync over its CreateEncryptor() transform, and through Transform(ReadOnlySpan<byte>), comparing all three with Aes.EncryptCbc.",
            why: "Server code reads and writes streams asynchronously, and a request that is abandoned should stop encrypting rather than run to the end. The extensions hang off SymmetricAlgorithm and ICryptoTransform rather than off Bodu types, so one helper surface serves the framework ciphers and the library's alike, and agreement with the framework's own one-shot call is the proof that no padding or chaining detail differs.",
            expect: "The async ciphertext equals the synchronous one and decrypts back to the payload. The cancelled call throws TaskCanceledException, and whatever reached the target is incomplete and must be discarded. All three Aes routes print True against EncryptCbc.");

        var payload = SamplePayload.Create();

        using var twofish = new Twofish
        {
            Key = SamplePayload.Key,
            IV = SamplePayload.Iv,
            BlockMode = CipherModeKind.CBC,
            BlockPadding = PaddingModeKind.PKCS7,
        };

        // The awaitable pair. In an application the token would come from the request or host.
        using var cts = new CancellationTokenSource();
        using var plaintextIn = new MemoryStream(payload);
        using var ciphertextOut = new MemoryStream();
        await twofish.EncryptAsync(plaintextIn, ciphertextOut, cts.Token).ConfigureAwait(false);
        var ciphertext = ciphertextOut.ToArray();

        using var ciphertextIn = new MemoryStream(ciphertext);
        using var recoveredOut = new MemoryStream();
        await twofish.DecryptAsync(ciphertextIn, recoveredOut, cts.Token).ConfigureAwait(false);

        Console.WriteLine("  Twofish-256 CBC/PKCS7, awaited");
        Console.WriteLine($"    EncryptAsync == Encrypt(byte[]) : {ciphertext.AsSpan().SequenceEqual(twofish.Encrypt(payload))}");
        Console.WriteLine($"    DecryptAsync recovers payload   : {recoveredOut.ToArray().AsSpan().SequenceEqual(payload)}");

        // A token that has already fired stops the pipeline at its first read. Treat the target as garbage after any
        // cancellation: the extensions do not roll back what they have written, so a partial output must be discarded.
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var abandonedIn = new MemoryStream(payload);
        using var abandonedOut = new MemoryStream();
        string outcome;
        try
        {
            await twofish.EncryptAsync(abandonedIn, abandonedOut, cancelled.Token).ConfigureAwait(false);
            outcome = "completed";
        }
        catch (OperationCanceledException ex)
        {
            outcome = ex.GetType().Name;
        }

        Console.WriteLine($"    cancelled token                 : {outcome} (discard the partial target)");
        Console.WriteLine();

        // The same extensions over the framework's Aes. Its defaults are CBC with PKCS#7 padding.
        using var aes = Aes.Create();
        aes.Key = SamplePayload.Key;
        aes.IV = SamplePayload.Iv;
        var reference = aes.EncryptCbc(payload, SamplePayload.Iv, PaddingMode.PKCS7);

        using var aesIn = new MemoryStream(payload);
        using var aesOut = new MemoryStream();
        aes.Encrypt(aesIn, aesOut);

        // TransformAsync works on any ICryptoTransform, including one the BCL created; it flushes the final
        // (padded) block itself and leaves both streams open.
        using var transformIn = new MemoryStream(payload);
        using var transformOut = new MemoryStream();
        using (var encryptor = aes.CreateEncryptor())
            await encryptor.TransformAsync(transformIn, transformOut, bufferSize: 16 * 1024, cts.Token).ConfigureAwait(false);

        // Transform(ReadOnlySpan<byte>) sizes the output for you and finalizes in one call.
        byte[] viaSpan;
        using (var encryptor = aes.CreateEncryptor())
            viaSpan = encryptor.Transform(payload.AsSpan());

        Console.WriteLine($"  System.Security.Cryptography.Aes-256 CBC/PKCS7 (EncryptCbc: {reference.Length} B, ct {Hex.ToShortHex(reference)})");
        Console.WriteLine($"    Encrypt(Stream, Stream)         : {aesOut.ToArray().AsSpan().SequenceEqual(reference)}");
        Console.WriteLine($"    TransformAsync(Stream, Stream)  : {transformOut.ToArray().AsSpan().SequenceEqual(reference)}");
        Console.WriteLine($"    Transform(ReadOnlySpan<byte>)   : {viaSpan.AsSpan().SequenceEqual(reference)}");

        Console.WriteLine();
    }
}
