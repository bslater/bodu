// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignaturesMlDsa.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.AsymmetricKeys.Scenarios;

/// <summary>
/// Signs and verifies a message with ML-DSA (FIPS 204) at all three parameter sets. A signer generates a
/// key pair and signs; a verifier holding only the public key accepts the genuine signature and rejects it
/// after the message is tampered with.
/// </summary>
/// <remarks>
/// Key generation (and hedged signing) draw fresh randomness, so the signature bytes differ every run. Only
/// the verification outcomes are deterministic, so the scenario prints the accept / reject booleans and the
/// fixed signature size, never the signature bytes.
/// </remarks>
public static class SignaturesMlDsa
{
    private static readonly byte[] Message = Encoding.ASCII.GetBytes("release firmware build 2026.07");

    /// <summary>
    /// Signs and verifies at each parameter set and prints the genuine / tampered verification outcomes.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "ML-DSA sign / verify (FIPS 204)",
            what: "At each of the three parameter sets: generates a key pair, signs a message, then verifies the signature against the genuine message and against a tampered one.",
            why: "ML-DSA is the post-quantum counterpart to Ed25519, and the same sign/verify shape applies - what changes is the size. Keys are generated per run, so, as with ML-KEM, only the booleans and the byte sizes are deterministic and the signature hex is deliberately not printed.",
            expect: "verify(genuine)=True and verify(tampered)=False at all three parameter sets, with signatures of 2420, 3309 and 4627 bytes at 44, 65 and 87 - two to four kilobytes per signature is the cost of post-quantum security, next to Ed25519's 64 bytes.");

        RunParameterSet("ML-DSA-44", () => MLDsa44.Create());
        RunParameterSet("ML-DSA-65", () => MLDsa65.Create());
        RunParameterSet("ML-DSA-87", () => MLDsa87.Create());

        Console.WriteLine();
    }

    /// <summary>
    /// Exercises one ML-DSA parameter set end to end.
    /// </summary>
    /// <param name="label">A short human-readable label for the parameter set.</param>
    /// <param name="factory">Creates a fresh instance of the parameter set.</param>
    private static void RunParameterSet(string label, Func<MLDsa> factory)
    {
        // The signer generates a key pair and signs the message.
        using var signer = factory();
        signer.GenerateKey();
        var signature = signer.SignData(Message);

        // The verifier holds only the public key.
        using var verifier = factory();
        verifier.ImportPublicKey(signer.ExportPublicKey());

        var genuine = verifier.VerifyData(Message, signature);

        // Flip one message byte; the signature must no longer verify.
        var tampered = (byte[])Message.Clone();
        tampered[0] ^= 0x01;
        var forged = verifier.VerifyData(tampered, signature);

        Console.WriteLine($"  {label,-10}: verify(genuine)={genuine}  verify(tampered)={forged}  (signature {signature.Length}B)");
    }
}
