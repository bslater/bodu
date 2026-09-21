// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IssueAndValidate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Samples.CustomCheckDigit.Scenarios;

/// <summary>
/// Demonstrates the custom scheme end to end: issue SKUs by computing the check digit, validate
/// intact and mistyped values, and use the streaming <c>Append</c> surface inherited from
/// <c>CheckDigitAlgorithm</c>.
/// </summary>
public static class IssueAndValidate
{
    /// <summary>
    /// Issues, validates, and streams through the custom SKU scheme.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "SKU-731 - issuing and validating with a custom scheme",
            what: "Issues three SKUs by appending the computed check digit, validates an intact SKU against a "
                + "wrong-check-digit and a mistyped-payload variant, and reads a digit from a payload fed in two "
                + "fragments.",
            why: "A check digit turns a transcription error into a local, immediate failure. Without one, a "
                + "mistyped SKU is just a different SKU - the system happily looks it up, finds nothing or finds "
                + "the wrong thing, and the error surfaces somewhere far from where it was made. The point of "
                + "implementing the library contract rather than a loose helper is that issuing and validating "
                + "then provably share one arithmetic definition: the digit a validator recomputes cannot drift "
                + "from the digit the issuer emitted.",
            expect: "Every issued SKU validates. Both corrupted variants are rejected - one altered digit is "
                + "enough, with no catalogue, database or network lookup involved. The streamed payload yields "
                + "the same digit as the one-shot call, because the base class carries position across Append "
                + "calls rather than restarting the weight cycle per fragment.");

        // Issue: append the computed check digit to each new SKU payload.
        foreach (var payload in new[] { "123456789", "000451", "998877" })
        {
            var check = SkuCheckDigit.Compute(payload);
            Console.WriteLine($"  payload {payload,-9} -> SKU {payload}{check}");
        }

        Console.WriteLine("  (000451 keeps its leading zeros: the weights are positional, so a zero still shifts every later digit's weight)");

        // Validate: intact passes, a typo or wrong check digit fails.
        var sku = "1234567893";
        Console.WriteLine($"  IsValid('{sku}')  = {SkuCheckDigit.IsValid(sku)}"
            + "  (expected True - validating is the same arithmetic the issuer ran, checked against the digit it emitted)");
        Console.WriteLine($"  IsValid('1234567892') = {SkuCheckDigit.IsValid("1234567892")}"
            + "  (expected False - the check digit was altered, so it no longer brings the weighted sum to a multiple of ten)");
        Console.WriteLine($"  IsValid('1235567893') = {SkuCheckDigit.IsValid("1235567893")}"
            + "  (expected False - one mistyped payload digit, caught locally without any lookup)");

        // The streaming surface comes from the base contract: fragments, then the digit.
        var streaming = new SkuCheckDigit();
        streaming.Append("12345");
        streaming.Append("6789");
        Console.WriteLine($"  streamed 12345|6789 -> check '{streaming.GetCurrentCheckDigit()}' via {streaming.AlgorithmName}"
            + "  (expected '3', matching the one-shot digit above - the fragment boundary is not observable)");

        Console.WriteLine();
    }
}
