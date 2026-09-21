// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OneTimePasswords.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Generates and verifies HOTP (RFC 4226) and TOTP (RFC 6238) codes with the canonical RFC test key and
/// fixed counters / timestamps, so the printed codes are the published reference values and reproduce every
/// run.
/// </summary>
public static class OneTimePasswords
{
    // The 20-byte ASCII secret "12345678901234567890" from the RFC 4226 / RFC 6238 test vectors.
    private static readonly byte[] Secret = Encoding.ASCII.GetBytes("12345678901234567890");

    // A fixed instant (59 seconds past the Unix epoch) pins the TOTP time-step to a known value.
    private static readonly DateTimeOffset FixedInstant = DateTimeOffset.FromUnixTimeSeconds(59);

    /// <summary>
    /// Generates HOTP codes for the first few counters and one TOTP code, verifying each.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "HOTP / TOTP (fixed key, fixed counter/time)",
            what: "Generates the HOTP codes at counters 0, 1 and 2 from the RFC 4226 test key and verifies each, then generates a TOTP code at a fixed instant and re-verifies that same code 30 seconds later.",
            why: "HOTP is counter-based and TOTP is the same construction over a time step, which is why a TOTP code expires. Both the key and the clock are passed in here rather than read from the environment, so the codes are reproducible; production code uses the real clock and usually accepts a small window of neighbouring steps.",
            expect: "755224, 287082 and 359152 - the first three rows of RFC 4226 appendix D - each verifying True. The TOTP code verifies at its own instant and then prints False one time step later with window 0, which is the expiry working rather than a failure.");

        // HOTP is counter-based: each counter yields a distinct code (the RFC 4226 Appendix D vectors).
        Console.WriteLine("  HOTP (RFC 4226 test key):");
        for (long counter = 0; counter < 3; counter++)
        {
            var code = Hotp.GenerateCode(Secret, counter);
            var verified = Hotp.VerifyCode(Secret, code, counter);
            Console.WriteLine($"    counter {counter} -> {code}  (verify: {verified})");
        }

        Console.WriteLine();

        // TOTP is time-based: the same secret plus a fixed instant produces a stable code.
        var totp = Totp.GenerateCode(Secret, FixedInstant);
        var totpVerified = Totp.VerifyCode(Secret, totp, FixedInstant);
        Console.WriteLine($"  TOTP at t=+59s -> {totp}  (verify: {totpVerified})");

        // A code checked against a different instant (one step later) should not verify.
        var laterInstant = FixedInstant.AddSeconds(30);

        // window widens acceptance to n adjacent time-steps to absorb clock skew; 0 demands the exact step.
        var staleVerified = Totp.VerifyCode(Secret, totp, laterInstant, window: 0);
        Console.WriteLine($"  same code at t=+89s (window 0) -> verify: {staleVerified}");

        Console.WriteLine();
    }
}
