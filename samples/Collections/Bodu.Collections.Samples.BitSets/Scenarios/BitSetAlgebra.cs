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
/// <remarks>
/// The operators mutate the receiver rather than returning a new set, which is what makes a packed bit set fast —
/// a whole 64-bit word of the answer is computed per machine instruction, with nothing allocated. The cost is that
/// <c>left.And(right)</c> destroys <c>left</c>, so every line here works on a fresh copy to keep the operands
/// comparable.
/// </remarks>
public static class BitSetAlgebra
{
    /// <summary>
    /// Applies each bulk operator to a copy of a base set so the operands stay comparable, then shows equality.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "BitSet - set algebra and equality",
            what: "Runs Intersects, And, Or, Xor and AndNot over {1,2,3,4,5} and {4,5,6,7}, each against a fresh " +
                  "copy, then compares a 64-bit-capacity result with a 512-bit-capacity set holding the same bits.",
            why: "These operators are why a bit set exists: membership algebra over dense integer sets costs one " +
                 "instruction per 64 elements instead of one hash lookup per element. Two details matter in " +
                 "practice. Intersects answers \"do these overlap?\" without building the intersection first, which " +
                 "is the common case in a filter. And AndNot is not sugar for And(Not(right)) - a growable bit set " +
                 "has no finite universe, so there is nothing to complement against and the relative complement " +
                 "has to be a primitive.",
            expect: "The four results are the arithmetic ones: {4,5}, {1,2,3,4,5,6,7}, {1,2,3,6,7} and {1,2,3}. " +
                    "The equality pair is the interesting one - the same two bits held in an eight-times-larger " +
                    "allocation compare equal and hash equal, because both are defined over content alone.");

        // Two overlapping sets: {1,2,3,4,5} and {4,5,6,7}. The intersection is {4,5}.
        var left = Build(1, 2, 3, 4, 5);
        var right = Build(4, 5, 6, 7);

        Console.WriteLine($"  left           : {Render(left)}  (the receiver each operator below is applied to)");
        Console.WriteLine($"  right          : {Render(right)}  (the operand; never modified by any of these calls)");

        // Intersects answers "do these share any bit?" without allocating an intermediate set - cheaper than
        // materializing an And result and asking whether it is empty.
        Console.WriteLine($"  Intersects     : {left.Intersects(right)}  (expected True - 4 and 5 are shared; answered without building the intersection)");

        // The four bulk operators mutate the receiver in place, so each one runs against a fresh copy taken
        // through the copy constructor. That constructor clones logical content, not the operand's capacity.
        var and = new BitSet(left);
        and.And(right);
        Console.WriteLine($"  left AND right : {Render(and)}  (expected {{4, 5}} - intersection, the bits in both)");

        var or = new BitSet(left);
        or.Or(right);
        Console.WriteLine($"  left OR right  : {Render(or)}  (expected {{1..7}} - union, the bits in either)");

        var xor = new BitSet(left);
        xor.Xor(right);
        Console.WriteLine($"  left XOR right : {Render(xor)}  (expected {{1, 2, 3, 6, 7}} - symmetric difference, so the shared 4 and 5 drop out)");

        // AndNot is relative complement - "left with right's bits removed" - and is the operation a naive
        // And(Not(right)) cannot express, because a growable bit set has no finite universe to complement against.
        var andNot = new BitSet(left);
        andNot.AndNot(right);
        Console.WriteLine($"  left ANDNOT rt : {Render(andNot)}  (expected {{1, 2, 3}} - left with right\u0027s bits removed)");

        // Equality compares logical content only: a set built with a large capacity equals a small-capacity set
        // holding the same bits, and the hash codes agree so BitSet works as a dictionary key.
        var roomy = new BitSet(initialCapacityBits: 512);
        roomy.Set(4);
        roomy.Set(5);

        Console.WriteLine($"  roomy          : {Render(roomy)}  (the same two bits, but allocated with eight times the capacity)");
        Console.WriteLine($"  and == roomy   : {and == roomy}  (expected True - equality is over content; capacity {and.Capacity} vs {roomy.Capacity} does not enter into it)");
        Console.WriteLine($"  hashes agree   : {and.GetHashCode() == roomy.GetHashCode()}  (expected True - equal values must hash equally, so BitSet is safe as a dictionary key)");

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
