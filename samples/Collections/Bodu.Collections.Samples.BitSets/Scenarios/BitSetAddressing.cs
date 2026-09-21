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
public static class BitSetAddressing
{
    /// <summary>
    /// Sets individual bits and ranges, then reports how the size properties and the scan primitives respond.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- BitSet: bit addressing, sizes, and scanning ---");

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

        Console.WriteLine($"  set bits      : {string.Join(", ", bits)}");

        // The three sizes answer three different questions and routinely disagree:
        //   Cardinality = how many bits are set (the population count).
        //   Length      = highest set index + 1 (the Java BitSet.length() contract) - logical content only.
        //   Capacity    = how many bits are currently allocated - storage, rounded up to whole 64-bit words.
        Console.WriteLine($"  cardinality   : {bits.Cardinality} (number of set bits)");
        Console.WriteLine($"  length        : {bits.Length} (highest set index + 1)");
        Console.WriteLine($"  capacity      : {bits.Capacity} (allocated bits, whole words)");
        Console.WriteLine($"  ToString()    : {bits}");

        // NextSetBit walks the set bits and returns -1 once none remain; this is the canonical iteration idiom
        // and it skips the gaps in O(words) rather than testing every index.
        var walk = new List<int>();
        for (var i = bits.NextSetBit(0); i >= 0; i = bits.NextSetBit(i + 1)) walk.Add(i);
        Console.WriteLine($"  NextSetBit    : {string.Join(", ", walk)} then -1");

        // NextClearBit never returns -1: every bit at or beyond Capacity is conceptually clear, so the result can
        // legitimately exceed both Length and Capacity. Here the first gap after index 2 is index 4.
        Console.WriteLine($"  NextClearBit(2): {bits.NextClearBit(2)} (first gap at or after 2)");
        Console.WriteLine($"  NextClearBit(16): {bits.NextClearBit(16)} (scans past the 16..19 run)");

        // Clearing the highest set bit shrinks Length immediately - but not Capacity, which never shrinks
        // implicitly. This is the clearest demonstration that the two are unrelated.
        bits.Clear(100);
        Console.WriteLine($"  after Clear(100) -> length {bits.Length}, capacity {bits.Capacity} (capacity is retained)");

        // Flip inverts; the range overload inverts a half-open window. Flipping 2..6 turns 2,3,5 off and 4 on.
        bits.Flip(2, 6);
        Console.WriteLine($"  after Flip(2, 6): {string.Join(", ", bits)}");

        // Clear() with no argument empties the set; Length drops to 0 while the allocation survives.
        bits.Clear();
        Console.WriteLine($"  after Clear()  : empty={bits.IsEmpty}, length={bits.Length}, capacity={bits.Capacity}");

        Console.WriteLine();
    }
}
