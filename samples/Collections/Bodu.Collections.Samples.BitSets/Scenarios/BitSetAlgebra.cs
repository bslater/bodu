// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetAlgebra.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Samples.BitSets.Scenarios;

/// <summary>
/// Demonstrates the <see cref="BitSet" /> set-algebra surface — <see cref="BitSet.And" />, <see cref="BitSet.Or" />,
/// <see cref="BitSet.Xor" />, <see cref="BitSet.AndNot" />, and the allocation-free
/// <see cref="BitSet.Intersects" /> test — plus value equality over logical content.
/// </summary>
public static class BitSetAlgebra
{
    /// <summary>
    /// Applies each bulk operator to a copy of a base set so the operands stay comparable, then shows equality.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- BitSet: set algebra and equality ---");

        // Two overlapping sets: {1,2,3,4,5} and {4,5,6,7}. The intersection is {4,5}.
        var left = Build(1, 2, 3, 4, 5);
        var right = Build(4, 5, 6, 7);

        Console.WriteLine($"  left           : {Render(left)}");
        Console.WriteLine($"  right          : {Render(right)}");

        // Intersects answers "do these share any bit?" without allocating an intermediate set - cheaper than
        // materializing an And result and asking whether it is empty.
        Console.WriteLine($"  Intersects     : {left.Intersects(right)}");

        // The four bulk operators mutate the receiver in place, so each one runs against a fresh copy taken
        // through the copy constructor. That constructor clones logical content, not the operand's capacity.
        var and = new BitSet(left);
        and.And(right);
        Console.WriteLine($"  left AND right : {Render(and)} (intersection)");

        var or = new BitSet(left);
        or.Or(right);
        Console.WriteLine($"  left OR right  : {Render(or)} (union)");

        var xor = new BitSet(left);
        xor.Xor(right);
        Console.WriteLine($"  left XOR right : {Render(xor)} (symmetric difference)");

        // AndNot is relative complement - "left with right's bits removed" - and is the operation a naive
        // And(Not(right)) cannot express, because a growable bit set has no finite universe to complement against.
        var andNot = new BitSet(left);
        andNot.AndNot(right);
        Console.WriteLine($"  left ANDNOT rt : {Render(andNot)} (relative complement)");

        // Equality compares logical content only: a set built with a large capacity equals a small-capacity set
        // holding the same bits, and the hash codes agree so BitSet works as a dictionary key.
        var roomy = new BitSet(initialCapacityBits: 512);
        roomy.Set(4);
        roomy.Set(5);

        Console.WriteLine($"  roomy          : {Render(roomy)}");
        Console.WriteLine($"  and == roomy   : {and == roomy} (capacity {and.Capacity} vs {roomy.Capacity} is irrelevant)");
        Console.WriteLine($"  hashes agree   : {and.GetHashCode() == roomy.GetHashCode()}");

        Console.WriteLine();
    }

    /// <summary>
    /// Creates a <see cref="BitSet" /> with the specified indices set.
    /// </summary>
    /// <param name="indices">The bit indices to set.</param>
    /// <returns>A new set containing exactly <paramref name="indices" />.</returns>
    private static BitSet Build(params int[] indices)
    {
        var set = new BitSet();
        foreach (var index in indices) set.Set(index);
        return set;
    }

    /// <summary>
    /// Renders a set as its set-bit indices in ascending order.
    /// </summary>
    /// <param name="set">The set to render.</param>
    /// <returns>A brace-delimited list of set-bit indices.</returns>
    private static string Render(BitSet set) =>
        $"{{{string.Join(", ", set)}}}";
}
