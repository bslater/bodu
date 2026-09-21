// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiMessageContext.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.HybridEncryption.Scenarios;

/// <summary>
/// Demonstrates <see cref="HpkeSender" /> and <see cref="HpkeReceiver" />, the stateful contexts that send several
/// messages under one encapsulation — one public-key operation amortised across a whole stream.
/// </summary>
public static class MultiMessageContext
{
    /// <summary>
    /// Sends a sequence of messages through one context pair, then shows that the sequence is order-bound.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Multi-message contexts (HpkeSender / HpkeReceiver) ---");

        using var recipient = Parties.CreateRecipient();
        var recipientPublicKey = recipient.ExportPublicKey();

        var suite = HpkeSuite.X25519_HkdfSha256_Aes256Gcm;
        var info = Parties.Utf8(Parties.Info);

        string[] messages =
        [
            "frame 1: session opened",
            "frame 2: 128 rows staged",
            "frame 3: commit accepted",
            "frame 4: session closed",
        ];

        // Setup* performs the KEM step once and hands back the encapsulation to transmit. Everything after this is
        // symmetric, so a long stream costs one public-key operation rather than one per message.
        using var sender = HpkeSender.SetupBase(suite, recipientPublicKey, info, out var encapsulation);
        Console.WriteLine($"  suite         : KEM={suite.Kem}, KDF={suite.Kdf}, AEAD={suite.Aead}");
        Console.WriteLine($"  encapsulation : {encapsulation.Length} bytes, sent once for {messages.Length} messages");

        // Each Seal advances the context's internal sequence number, so the same plaintext sealed twice produces
        // different ciphertext - the nonce is derived from that counter rather than reused.
        var frames = messages.Select(m => sender.Seal(Parties.Utf8("frame-aad"), Parties.Utf8(m))).ToArray();
        Console.WriteLine($"  sealed        : {frames.Length} frames, sizes {string.Join(", ", frames.Select(f => f.Length))}");

        // The receiver derives the same key schedule from the single encapsulation and opens the frames in order.
        using var receiver = HpkeReceiver.SetupBase(suite, recipient, encapsulation, info);
        var opened = frames.Select(f => Parties.Text(receiver.Open(Parties.Utf8("frame-aad"), f))).ToArray();

        Console.WriteLine($"  all round-trip: {opened.SequenceEqual(messages)}");
        foreach (var (text, index) in opened.Select((t, i) => (t, i)))
            Console.WriteLine($"    [{index}] \"{text}\"");

        // Sealing identical plaintext twice under one context gives different ciphertext, because the sequence number
        // moved. This is what makes a stream safe without the caller managing nonces.
        var repeatA = sender.Seal([], Parties.Utf8("identical"));
        var repeatB = sender.Seal([], Parties.Utf8("identical"));
        Console.WriteLine($"  same plaintext twice differs: {Hex.ToHex(repeatA) != Hex.ToHex(repeatB)} (the sequence number advanced)");

        // And the stream is order-bound: a receiver at sequence n cannot open a frame sealed at n+1, so a dropped or
        // reordered frame is detected rather than silently accepted.
        using var freshReceiver = HpkeReceiver.SetupBase(suite, recipient, encapsulation, info);
        Console.WriteLine($"  skipping frame 0: {SingleShot.Fails(() => freshReceiver.Open(Parties.Utf8("frame-aad"), frames[1]))}");

        // A replayed frame fails for the same reason: the receiver has moved past that sequence number.
        using var replayReceiver = HpkeReceiver.SetupBase(suite, recipient, encapsulation, info);
        _ = replayReceiver.Open(Parties.Utf8("frame-aad"), frames[0]);
        Console.WriteLine($"  replaying frame 0: {SingleShot.Fails(() => replayReceiver.Open(Parties.Utf8("frame-aad"), frames[0]))}");

        // The authenticated modes have the same context shape - only the Setup call differs, so switching a stream
        // from anonymous to sender-authenticated changes one line.
        using var authSenderKey = Parties.CreateSender();
        using var authSender = HpkeSender.SetupAuth(suite, recipientPublicKey, info, authSenderKey, out var authEncapsulation);
        var authFrame = authSender.Seal([], Parties.Utf8("authenticated stream"));

        using var authReceiver = HpkeReceiver.SetupAuth(suite, recipient, authEncapsulation, info, authSenderKey.ExportPublicKey());
        Console.WriteLine($"  auth-mode context : {Parties.Text(authReceiver.Open([], authFrame)) == "authenticated stream"}");

        Console.WriteLine();
    }
}
