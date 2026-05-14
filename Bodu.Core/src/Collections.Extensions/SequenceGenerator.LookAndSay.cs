// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.LookAndSay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields successive terms of Conway's Look-and-Say sequence, starting from the seed term <c>"1"</c>.
    /// </summary>
    /// <param name="count">The total number of terms to emit, including the seed. Must be at least <c>1</c>.</param>
    /// <returns>
    /// A lazily evaluated, finite sequence of <see cref="string"/> values where the first element is <c>"1"</c> and each subsequent
    /// element is the run-length-encoded description of the digits of the previous element.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than <c>1</c>.</exception>
    /// <remarks>
    /// <para>
    /// Use this generator when demonstrating self-describing integer sequences, prototyping digit-run encoders, or as a worked
    /// example of how a single seed expands into Conway's "audioactive" sequence. The seed is fixed at <c>"1"</c>; if a different
    /// seed is required, take the first term as a starting string and apply the run-length transform manually.
    /// </para>
    /// <para>
    /// Each term grows in length roughly by Conway's constant (<c>≈ 1.303577…</c>) per iteration, so the strings produced for
    /// large <paramref name="count"/> values grow quickly and may dominate allocation cost. Iteration is deferred — argument
    /// validation runs on first <c>MoveNext</c> — and a single <see cref="StringBuilder"/> instance is reused per term.
    /// </para>
    /// <para>The result is deterministic (the seed is fixed) and the iterator carries no shared mutable state.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// foreach (string term in SequenceGenerator.LookAndSay(6))
    ///     Console.WriteLine(term);
    /// // => 1
    /// // => 11
    /// // => 21
    /// // => 1211
    /// // => 111221
    /// // => 312211
    /// </code>
    /// </example>
    public static IEnumerable<string> LookAndSay(int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 1);

        var current = "1";
        for (var i = 0; i < count; i++)
        {
            yield return current;

            var next = new StringBuilder();
            var j = 0;
            while (j < current.Length)
            {
                var digit = current[j];
                var runLength = 1;
                while (j + runLength < current.Length && current[j + runLength] == digit)
                    runLength++;
                next.Append(runLength).Append(digit);
                j += runLength;
            }

            current = next.ToString();
        }
    }
}
