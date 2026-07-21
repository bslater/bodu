// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

namespace Bodu.Collections.Samples.CollectionCatalogue;

/// <summary>
/// Entry point for the collection-catalogue sample: a tour of the specialized generic collections in
/// <c>Bodu.Collections.Generic</c> — the fixed-capacity ring and deque, the evicting cache, the multi-map /
/// multiset / ordered-set family, the bidirectional and navigable dictionaries, and the sequenced dictionary
/// alongside the indexed priority queue. Everything runs offline and deterministically.
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
        MultiMapsAndSets.Run();
        BiDirectionalAndNavigable.Run();
        SequencedAndPriority.Run();

        Console.WriteLine("Done.");
    }
}
