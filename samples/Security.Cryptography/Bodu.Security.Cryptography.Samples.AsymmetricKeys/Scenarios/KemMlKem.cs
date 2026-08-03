// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KemMlKem.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Samples.AsymmetricKeys.Scenarios;

/// <summary>
/// Runs the ML-KEM (FIPS 203) key-encapsulation mechanism at all three parameter sets. A receiver generates
/// a key pair; a sender encapsulates against the public encapsulation key to produce a ciphertext plus a
/// shared secret, which the receiver recovers by decapsulating.
/// </summary>
/// <remarks>
/// Key generation and encapsulation draw fresh randomness, so the ciphertext and shared-secret bytes differ
/// every run. Only the derived facts are deterministic — the two sides agree, and ML-KEM's implicit
/// rejection means a tampered ciphertext decapsulates to a <em>different</em> secret rather than throwing —
/// so the scenario prints those booleans (and the fixed sizes), never the secret bytes.
/// </remarks>
public static class KemMlKem
{
    /// <summary>
    /// Encapsulates and decapsulates at each parameter set and prints the agreement and rejection facts.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- ML-KEM key encapsulation (FIPS 203) ---");
        Console.WriteLine();

        RunParameterSet("ML-KEM-512", () => MLKem512.Create());
        RunParameterSet("ML-KEM-768", () => MLKem768.Create());
        RunParameterSet("ML-KEM-1024", () => MLKem1024.Create());

        Console.WriteLine();
    }

    /// <summary>
    /// Exercises one ML-KEM parameter set end to end.
    /// </summary>
    /// <param name="label">A short human-readable label for the parameter set.</param>
    /// <param name="factory">Creates a fresh instance of the parameter set.</param>
    private static void RunParameterSet(string label, Func<MLKem> factory)
    {
        // The receiver generates a key pair and publishes only its encapsulation (public) key.
        using var receiver = factory();
        receiver.GenerateKey();

        // The sender needs only the public encapsulation key — importing it here models receiving it over the wire.
        using var sender = factory();
        sender.ImportEncapsulationKey(receiver.ExportEncapsulationKey());

        // The sender produces a ciphertext bound to a fresh shared secret; the receiver recovers the secret.
        var (ciphertext, senderSecret) = sender.Encapsulate();
        var receiverSecret = receiver.Decapsulate(ciphertext);
        var agree = senderSecret.AsSpan().SequenceEqual(receiverSecret);

        // Implicit rejection: a corrupted ciphertext yields a different secret, never the sender's.
        var tampered = (byte[])ciphertext.Clone();
        tampered[0] ^= 0xff;
        var rejectedSecret = receiver.Decapsulate(tampered);
        var rejects = !rejectedSecret.AsSpan().SequenceEqual(senderSecret);

        Console.WriteLine($"  {label,-12}: secrets agree={agree}  (secret {senderSecret.Length}B, ciphertext {ciphertext.Length}B)");
        Console.WriteLine($"      tampered ciphertext -> different secret (implicit rejection): {rejects}");
    }
}
