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
        Console.WriteLine("--- ML-DSA sign / verify (FIPS 204) ---");
        Console.WriteLine();

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
