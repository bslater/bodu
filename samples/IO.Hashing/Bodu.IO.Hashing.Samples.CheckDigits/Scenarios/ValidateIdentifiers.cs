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
        Console.WriteLine("--- IsValid across identifier domains ---");

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

            Console.WriteLine($"  {scheme}: '{value}' -> {isValid(value)},  typo '{typo}' -> {isValid(typo)}");
        }

        Console.WriteLine();
    }
}
