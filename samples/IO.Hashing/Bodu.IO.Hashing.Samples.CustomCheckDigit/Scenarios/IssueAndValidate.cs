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
        Console.WriteLine("--- SKU-731: issue and validate ---");

        // Issue: append the computed check digit to each new SKU payload.
        foreach (var payload in new[] { "123456789", "000451", "998877" })
        {
            var check = SkuCheckDigit.Compute(payload);
            Console.WriteLine($"  payload {payload,-9} -> SKU {payload}{check}");
        }

        // Validate: intact passes, a typo or wrong check digit fails.
        var sku = "1234567893";
        Console.WriteLine($"  IsValid('{sku}')  = {SkuCheckDigit.IsValid(sku)}");
        Console.WriteLine($"  IsValid('1234567892') = {SkuCheckDigit.IsValid("1234567892")} (wrong check digit)");
        Console.WriteLine($"  IsValid('1235567893') = {SkuCheckDigit.IsValid("1235567893")} (mistyped payload digit)");

        // The streaming surface comes from the base contract: fragments, then the digit.
        var streaming = new SkuCheckDigit();
        streaming.Append("12345");
        streaming.Append("6789");
        Console.WriteLine($"  streamed 12345|6789 -> check '{streaming.GetCurrentCheckDigit()}' ({streaming.AlgorithmName})");

        Console.WriteLine();
    }
}
