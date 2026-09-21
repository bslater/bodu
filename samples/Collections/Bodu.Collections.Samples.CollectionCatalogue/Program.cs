// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

namespace Bodu.Collections.Samples.CollectionCatalogue;

/// <summary>
/// Entry point for the collection-catalogue sample: a tour of the specialized generic collections in
/// <c>Bodu.Collections.Generic</c> — the fixed-capacity ring and deque, the evicting cache with both its capacity
/// and time dimensions, the multi-map / multiset / ordered-set family, the bidirectional and navigable dictionaries,
/// the sequenced dictionary alongside the indexed priority queue, the layered and defaulting dictionary decorators,
/// and the two-key table with the segmented buffer. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Collections.Samples.CollectionCatalogue");
        Console.WriteLine("============================================");
        Console.WriteLine();

        RingAndDeque.Run();
        EvictingCache.Run();
        ExpiringCache.Run();
        MultiMapsAndSets.Run();
        BiDirectionalAndNavigable.Run();
        SequencedAndPriority.Run();
        ChainedAndDefaulting.Run();
        TableAndSegments.Run();

        Console.WriteLine("Done.");
    }
}
