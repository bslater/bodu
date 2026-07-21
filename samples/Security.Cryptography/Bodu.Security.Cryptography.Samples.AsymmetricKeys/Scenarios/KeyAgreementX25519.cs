// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyAgreementX25519.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.AsymmetricKeys.Scenarios;

/// <summary>
/// Performs an X25519 Diffie-Hellman key agreement using the RFC 7748 §6.1 test vectors: two parties import
/// fixed private keys, exchange public keys, and independently derive the same shared secret. Because the
/// private keys are the published vectors, both the public keys and the shared secret are reproducible and
/// cross-check against the RFC.
/// </summary>
public static class KeyAgreementX25519
{
    // RFC 7748 §6.1 — Alice's and Bob's private scalars, and the expected public keys and shared secret.
    private const string AlicePrivateHex = "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a";
    private const string BobPrivateHex = "5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb";
    private const string ExpectedAlicePublicHex = "8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a";
    private const string ExpectedBobPublicHex = "de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f";
    private const string ExpectedSharedHex = "4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742";

    /// <summary>
    /// Imports the fixed private keys, exchanges public keys, and derives the shared secret on both sides.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- X25519 key agreement (RFC 7748 6.1 vectors) ---");
        Console.WriteLine();

        // Each party imports its fixed private scalar rather than generating a random key pair.
        using var alice = X25519.Create();
        alice.ImportPrivateKey(Hex.FromHex(AlicePrivateHex));

        using var bob = X25519.Create();
        bob.ImportPrivateKey(Hex.FromHex(BobPrivateHex));

        // The public keys are the clamped scalar-multiplications of the base point; they match the RFC.
        var alicePublic = alice.ExportPublicKey();
        var bobPublic = bob.ExportPublicKey();
        Console.WriteLine($"  Alice public : {Hex.ToHex(alicePublic)}  (matches RFC: {Hex.ToHex(alicePublic) == ExpectedAlicePublicHex})");
        Console.WriteLine($"  Bob public   : {Hex.ToHex(bobPublic)}  (matches RFC: {Hex.ToHex(bobPublic) == ExpectedBobPublicHex})");
        Console.WriteLine();

        // Each party feeds the other's public key to derive the identical shared secret.
        var aliceShared = alice.DeriveSharedSecret(bobPublic);
        var bobShared = bob.DeriveSharedSecret(alicePublic);

        Console.WriteLine($"  Alice derives: {Hex.ToHex(aliceShared)}");
        Console.WriteLine($"  Bob derives  : {Hex.ToHex(bobShared)}");
        Console.WriteLine($"  secrets agree: {aliceShared.AsSpan().SequenceEqual(bobShared)}");
        Console.WriteLine($"  matches RFC  : {Hex.ToHex(aliceShared) == ExpectedSharedHex}");

        Console.WriteLine();
    }
}
