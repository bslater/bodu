// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignaturesEd25519.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.AsymmetricKeys.Scenarios;

/// <summary>
/// Signs and verifies a message with Ed25519. A signer imports a fixed 32-byte seed and signs; a verifier
/// imports only the derived public key and accepts the signature, then rejects it after the message is
/// tampered with. Ed25519 key generation and signing are deterministic, so the public key and signature are
/// reproducible from the fixed seed.
/// </summary>
public static class SignaturesEd25519
{
    // A fixed 32-byte seed (0x00..0x1f). Ed25519 derives the key pair deterministically from the seed.
    private static readonly byte[] Seed = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

    private static readonly byte[] Message = Encoding.ASCII.GetBytes("transfer 100 units to account 42");

    /// <summary>
    /// Signs the message, verifies it with the public key, then shows a tampered message failing verification.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Ed25519 sign / verify (fixed seed) ---");
        Console.WriteLine();

        // The signer imports the fixed seed and produces the 64-byte signature.
        using var signer = Ed25519.Create();
        signer.ImportPrivateKey(Seed);
        var publicKey = signer.ExportPublicKey();
        var signature = signer.SignData(Message);

        Console.WriteLine($"  public key : {Hex.ToHex(publicKey)}");
        Console.WriteLine($"  signature  : {Hex.ToHex(signature)}");
        Console.WriteLine();

        // The verifier holds only the public key — the private seed never leaves the signer.
        using var verifier = Ed25519.Create();
        verifier.ImportPublicKey(publicKey);

        var genuine = verifier.VerifyData(Message, signature);

        // Flip one message byte; the signature no longer matches, so verification fails.
        var tampered = (byte[])Message.Clone();
        tampered[0] ^= 0x01;
        var forged = verifier.VerifyData(tampered, signature);

        Console.WriteLine($"  verify (genuine message)  = {genuine}");
        Console.WriteLine($"  verify (tampered message) = {forged}");

        Console.WriteLine();
    }
}
