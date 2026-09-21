// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValidateIdentifiers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Samples.CheckDigits.Scenarios;

/// <summary>
/// Demonstrates validation across identifier domains: every scheme exposes the same static
/// <c>IsValid</c> shape, so a form-validation layer treats an IBAN, an ISBN, a barcode, a card
/// number, and a routing number identically — and a single mistyped character flips each to
/// invalid.
/// </summary>
public static class ValidateIdentifiers
{
    /// <summary>
    /// Validates one well-known identifier per domain, intact and with one character corrupted.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "IsValid across identifier domains",
            what: "Validates a known-good identifier from six schemes - IBAN, ISBN-10, ISBN-13, EAN-13, a Luhn " +
                  "card number and an ABA routing number - then alters exactly one digit in each and validates again.",
            why: "A check digit is arithmetic the issuer folded into the identifier so a recipient can reject a " +
                 "mistyped one before it reaches a system that would act on it. Validating locally turns a failed " +
                 "payment, a wrong book or a misrouted transfer into an input-validation error - free, instant, " +
                 "and with no lookup. Each scheme uses different arithmetic, which is why one IsValid per domain " +
                 "exists rather than a single generic check.",
            expect: "Every genuine identifier validates and every single-digit alteration is rejected. Detecting " +
                    "any single wrong digit is the weakest guarantee all six schemes make - the next scenario " +
                    "shows where they start to differ.");

        // (scheme, example, validator) - all examples are well-known published test values.
        var rows = new (string Scheme, string Value, Func<string, bool> IsValid)[]
        {
            ("IBAN (mod 97-10)", "GB82WEST12345698765432", v => Iban.IsValid(v)),
            ("ISBN-10        ", "0306406152", v => Isbn10.IsValid(v)),
            ("ISBN-13        ", "9780306406157", v => Isbn13.IsValid(v)),
            ("EAN-13 barcode ", "4006381333931", v => Ean13.IsValid(v)),
            ("Card (Luhn)    ", "79927398713", v => Luhn.IsValid(v)),
            ("ABA routing    ", "011000015", v => AbaRoutingNumber.IsValid(v)),
        };

        foreach (var (scheme, value, isValid) in rows)
        {
            // Corrupt one interior character (a realistic typo).
            var corrupted = value.ToCharArray();
            corrupted[4] = corrupted[4] == '9' ? '0' : (char)(corrupted[4] + 1);
            var typo = new string(corrupted);

            Console.WriteLine($"  {scheme}: \u0027{value}\u0027 -> {isValid(value)},  typo \u0027{typo}\u0027 -> {isValid(typo)}  (expected True then False - one altered digit, caught locally without any lookup)");
        }

        Console.WriteLine();
    }
}
