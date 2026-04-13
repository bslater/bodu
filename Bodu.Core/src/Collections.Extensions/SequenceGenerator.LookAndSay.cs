// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.LookAndSay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates terms in the Look-and-Say sequence starting from "1".
    /// </summary>
    /// <param name="count">The number of terms to generate. Must be positive.</param>
    /// <returns>A sequence of strings, each representing a term in the Look-and-Say sequence.</returns>
    /// <remarks>Each term describes the digits of the previous one, e.g.: 1, 11, 21, 1211, 111221, ...</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 1.</exception>
    public static IEnumerable<string> LookAndSay(int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 1);

        string current = "1";
        for (int i = 0; i < count; i++)
        {
            yield return current;

            StringBuilder next = new StringBuilder();
            int j = 0;
            while (j < current.Length)
            {
                char digit = current[j];
                int runLength = 1;
                while (j + runLength < current.Length && current[j + runLength] == digit)
                    runLength++;
                next.Append(runLength).Append(digit);
                j += runLength;
            }

            current = next.ToString();
        }
    }
}