// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetAddressing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Samples.BitSets.Scenarios;

/// <summary>
/// Demonstrates <see cref="BitSet" /> as a packed, growable set of non-negative integers: single-bit and range
/// mutation, the three distinct size properties (<see cref="BitSet.Cardinality" />, <see cref="BitSet.Length" />,
/// <see cref="BitSet.Capacity" />), the scanning primitives, and enumeration.
/// </summary>
/// <remarks>
/// The three size properties are the part worth slowing down for. They answer different questions, they routinely
/// disagree, and picking the wrong one produces code that works on small inputs and quietly misbehaves later — so
/// this scenario is built to make them disagree and then show why each value is the right answer to its own question.
/// </remarks>
public static class BitSetAddressing
{
    /// <summary>
    /// Sets individual bits and ranges, then reports how the size properties and the scan primitives respond.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "BitSet - bit addressing, sizes, and scanning",
            what: "Sets individual bits, a half-open range and one index far beyond the initial capacity, then reads " +
                  "back the three size properties, walks the set and clear bits, and clears the highest bit.",
            why: "Cardinality, Length and Capacity sound interchangeable and are not: they are the population " +
                 "count, the logical extent, and the allocation. Reaching for the wrong one is a real bug - sizing " +
                 "a loop by Capacity walks bits that were never set, and treating Length as a count silently " +
                 "over-reports the moment the set has a gap. NextSetBit matters for the same practical reason: it " +
                 "skips whole empty words, so iterating a sparse set costs words rather than indices.",
            expect: "The three sizes read 9, 101 and 128 for the same nine bits - a count, a highest-index-plus-one, " +
                    "and a word-rounded allocation. Clearing bit 100 drops Length to 20 and leaves Capacity at 128, " +
                    "because capacity never shrinks on its own.");

        // The initial capacity is a hint only - the set grows on demand, so an index beyond it is never an error.
        var bits = new BitSet(initialCapacityBits: 64);

        // Set/Clear/Flip address one bit; the indexer is the same operation in expression form.
        bits.Set(2);
        bits.Set(3);
        bits[5] = true;
        bits.Set(7, true);

        // Range overloads take [fromInclusive, toExclusive) - the half-open convention the BCL uses throughout.
        bits.Set(16, 20);

        // Setting a bit past the initial capacity grows the backing store transparently.
        bits.Set(100);

        Console.WriteLine($"  set bits      : {string.Join(", ", bits)}  (expected 2, 3, 5, 7, 16..19, 100 - enumeration yields set indices in ascending order)");

        // The three sizes answer three different questions and routinely disagree:
        //   Cardinality = how many bits are set (the population count).
        //   Length      = highest set index + 1 (the Java BitSet.length() contract) - logical content only.
        //   Capacity    = how many bits are currently allocated - storage, rounded up to whole 64-bit words.
        Console.WriteLine($"  cardinality   : {bits.Cardinality}  (expected 9 - how many bits are set; the only one of the three that is a count)");
        Console.WriteLine($"  length        : {bits.Length}  (expected 101 - highest set index + 1, so the gaps below 100 still count toward it)");
        Console.WriteLine($"  capacity      : {bits.Capacity}  (expected 128 - storage rounded to whole 64-bit words, not content)");
        Console.WriteLine($"  ToString()    : {bits}  (the debug view reports content, not allocation)");

        // NextSetBit walks the set bits and returns -1 once none remain; this is the canonical iteration idiom
        // and it skips the gaps in O(words) rather than testing every index.
        var walk = new List<int>();
        for (var i = bits.NextSetBit(0); i >= 0; i = bits.NextSetBit(i + 1)) walk.Add(i);
        Console.WriteLine($"  NextSetBit    : {string.Join(", ", walk)} then -1  (expected the same nine indices - -1 is the terminator, which is why the loop condition is >= 0)");

        // NextClearBit never returns -1: every bit at or beyond Capacity is conceptually clear, so the result can
        // legitimately exceed both Length and Capacity. Here the first gap after index 2 is index 4.
        Console.WriteLine($"  NextClearBit(2) : {bits.NextClearBit(2)}  (expected 4 - bits 2 and 3 are set, so the first gap at or after 2 is 4)");
        Console.WriteLine($"  NextClearBit(16): {bits.NextClearBit(16)}  (expected 20 - scans past the 16..19 run; unlike NextSetBit this never returns -1, since every bit beyond Capacity is clear)");

        // Clearing the highest set bit shrinks Length immediately - but not Capacity, which never shrinks
        // implicitly. This is the clearest demonstration that the two are unrelated.
        bits.Clear(100);
        Console.WriteLine($"  after Clear(100): length {bits.Length}, capacity {bits.Capacity}  (expected 20 and 128 - Length tracks content and falls to the next-highest bit; Capacity is an allocation and never shrinks implicitly)");

        // Flip inverts; the range overload inverts a half-open window. Flipping 2..6 turns 2,3,5 off and 4 on.
        bits.Flip(2, 6);
        Console.WriteLine($"  after Flip(2,6) : {string.Join(", ", bits)}  (expected 4, 7, 16..19 - 2, 3 and 5 were on and go off, 4 was off and comes on; 6 is excluded by the half-open range)");

        // Clear() with no argument empties the set; Length drops to 0 while the allocation survives.
        bits.Clear();
        Console.WriteLine($"  after Clear()   : empty={bits.IsEmpty}, length={bits.Length}, capacity={bits.Capacity}  (expected True, 0, 128 - emptying is a content operation; the buffer stays for reuse)");

        Console.WriteLine();
    }
}
