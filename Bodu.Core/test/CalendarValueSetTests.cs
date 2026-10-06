// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

/// <summary>
/// Contains unit tests for <see cref="CalendarValueSet" />, the bit handling and text forms the calendar value sets
/// share.
/// </summary>
/// <remarks>
/// The sets' own contracts run these helpers over domains of 7 to 60 values. The tests here add what no set reaches: a
/// domain of the full 64 values a bitmap can hold, where a shift by 64 bits wraps to a shift by none, and the letter
/// masks at either first position.
/// </remarks>
[TestClass]
public sealed partial class CalendarValueSetTests
{
    /// <summary>The seed of the random bitmaps, so that a failure reproduces.</summary>
    private const int Seed = 20261006;

    /// <summary>
    /// Returns bitmaps over a 64-value domain: the edges, then random bitmaps from a fixed seed.
    /// </summary>
    /// <returns>The sample bitmaps.</returns>
    private static IEnumerable<ulong> SampleBits()
    {
        yield return 0;
        yield return ulong.MaxValue;
        yield return 1;
        yield return 1UL << 63;
        yield return (1UL << 63) | 1;
        yield return 0x5555_5555_5555_5555UL;
        yield return 0xAAAA_AAAA_AAAA_AAAAUL;

        var random = new Random(Seed);
        for (int i = 0; i < 64; i++)
            yield return (ulong)random.NextInt64() ^ ((ulong)random.Next() << 63);
    }
}
