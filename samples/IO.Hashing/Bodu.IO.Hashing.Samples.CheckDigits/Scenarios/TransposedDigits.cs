// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TransposedDigits.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Samples.CheckDigits.Scenarios;

/// <summary>
/// Demonstrates why multiple schemes exist: check-digit algorithms differ in the error classes
/// they detect. Luhn catches every single-digit error but misses some adjacent transpositions
/// (the classic <c>09 ↔ 90</c>), while Damm and Verhoeff detect all single-digit errors AND all
/// adjacent transpositions — the property they were invented for.
/// </summary>
public static class TransposedDigits
{
    /// <summary>
    /// Compares Luhn, Damm, and Verhoeff against transposition errors.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Error classes: Luhn vs Damm vs Verhoeff on transpositions ---");

        // Build one valid identifier per scheme from the same payload.
        var payload = "1234567890";
        var luhnFull = payload + Luhn.Compute(payload);
        var dammFull = payload + Damm.Compute(payload);
        var verhoeffFull = payload + Verhoeff.Compute(payload);

        Console.WriteLine($"payload {payload}: Luhn={luhnFull}, Damm={dammFull}, Verhoeff={verhoeffFull}");
        Console.WriteLine();
        Console.WriteLine("swap adjacent digits at each position; '.' = error detected, 'M' = MISSED:");

        foreach (var (name, full, isValid) in new (string, string, Func<string, bool>)[]
        {
            ("Luhn    ", luhnFull, v => Luhn.IsValid(v)),
            ("Damm    ", dammFull, v => Damm.IsValid(v)),
            ("Verhoeff", verhoeffFull, v => Verhoeff.IsValid(v)),
        })
        {
            var marks = new List<char>();
            for (var i = 0; i < full.Length - 1; i++)
            {
                if (full[i] == full[i + 1])
                {
                    marks.Add(' '); // swapping equal digits changes nothing - not an error case
                    continue;
                }

                var chars = full.ToCharArray();
                (chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
                marks.Add(isValid(new string(chars)) ? 'M' : '.');
            }

            Console.WriteLine($"  {name} {full}: [{new string(marks.ToArray())}]");
        }

        Console.WriteLine();
        Console.WriteLine("Luhn misses the 09<->90 swap; Damm and Verhoeff detect every adjacent transposition.");

        Console.WriteLine();
    }
}
