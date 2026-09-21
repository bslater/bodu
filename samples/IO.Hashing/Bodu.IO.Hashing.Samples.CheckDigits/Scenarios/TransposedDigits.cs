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
        SampleConsole.Scenario(
            "Error classes - Luhn vs Damm vs Verhoeff on transpositions",
            what: "Issues the same payload under three schemes, then swaps each adjacent pair of digits in turn " +
                  "and records whether the check digit catches it.",
            why: "All three catch any single wrong digit, so on that test they look equivalent. Transposition is " +
                  "where they separate, and it matters because swapping two adjacent digits is the second most " +
                  "common human typing error. Luhn - the scheme on every payment card - provably cannot detect " +
                  "the 09 to 90 swap, a documented gap in the algorithm rather than a bug. Damm and Verhoeff " +
                  "catch every adjacent transposition, which is why a new identifier scheme should not default to " +
                  "Luhn out of familiarity.",
            expect: "The Luhn row shows an M at exactly one position - the 09/90 swap it is known to miss - and " +
                    "dots everywhere else. Damm and Verhoeff show dots throughout: no adjacent transposition gets " +
                    "past either.");

        // Build one valid identifier per scheme from the same payload.
        var payload = "1234567890";
        var luhnFull = payload + Luhn.Compute(payload);
        var dammFull = payload + Damm.Compute(payload);
        var verhoeffFull = payload + Verhoeff.Compute(payload);

        Console.WriteLine($"  payload {payload}: Luhn={luhnFull}, Damm={dammFull}, Verhoeff={verhoeffFull}  (one payload, three schemes - each appends a different check digit)");
        Console.WriteLine();
        Console.WriteLine("  swap adjacent digits at each position; '.' = error detected, 'M' = MISSED:");

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

            Console.WriteLine($"  {name} {full}: [{new string(marks.ToArray())}]  (one slot per adjacent pair: . caught it, M let it through)");
        }

        Console.WriteLine();
        Console.WriteLine("  Luhn misses the 09<->90 swap - a documented gap in the algorithm, not a bug here; Damm and Verhoeff detect every adjacent transposition, which is why a NEW scheme should not default to Luhn out of familiarity.");

        Console.WriteLine();
    }
}
