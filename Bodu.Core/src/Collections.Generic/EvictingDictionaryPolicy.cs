// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryPolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Specifies the eviction strategy used by an <see cref="EvictingDictionary{TKey, TValue}" /> when its capacity is
/// exceeded.
/// </summary>
/// <remarks>
/// The <see cref="EvictingDictionaryPolicy" /> enumeration defines how items are selected for removal when the
/// dictionary reaches its maximum capacity. The selected policy determines whether the dictionary behaves like a queue,
/// cache, or frequency-based store.
/// <para>
/// <strong>Eviction Example:</strong><br /> Consider a dictionary with a capacity of 3. The following operations occur
/// in order:
/// </para>
/// <code>
///<![CDATA[
/// Add("A")    // Dictionary: A
/// Add("B")    // Dictionary: A, B
/// Add("C")    // Dictionary: A, B, C
/// Access("A") // Marks "A" as recently used or increases its frequency
/// Add("D")    // Triggers eviction: Dictionary must remove one entry
///]]>
/// </code>
/// <para>
/// The item that is evicted varies depending on the selected <see cref="EvictingDictionaryPolicy" />:
/// </para>
/// <list type="table">
/// <listheader> <term>Policy</term>
/// <description>
/// Item Evicted
/// </description>
/// </listheader>
/// <item>
/// <term><see cref="FirstInFirstOut" /></term>
/// <description>
/// <c>"A"</c> - the first item added, regardless of access.
/// </description>
/// </item>
/// <item>
/// <term><see cref="LeastRecentlyUsed" /></term>
/// <description>
/// <c>"C"</c> - the item least recently accessed (A was accessed, C was not).
/// </description>
/// </item>
/// <item>
/// <term><see cref="LeastFrequentlyUsed" /></term>
/// <description>
/// <c>"B"</c> - the first item with the lowest access count.
/// </description>
/// </item>
/// <item>
/// <term><see cref="MostRecentlyUsed" /></term>
/// <description>
/// <c>"A"</c> - the most recently accessed item.
/// </description>
/// </item>
/// <item>
/// <term><see cref="RandomReplacement" /></term>
/// <description>
/// A randomly chosen item from <c>"A"</c>, <c>"B"</c>, or <c>"C"</c>.
/// </description>
/// </item>
/// <item>
/// <term><see cref="SecondChance" /></term>
/// <description>
/// <c>"B"</c> - evicted after its reference bit is cleared; <c>"A"</c> is spared due to recent access.
/// </description>
/// </item>
/// </list>
/// <para>
/// This example highlights the behavioral differences across policies using the same input sequence. Actual eviction
/// order may vary depending on access patterns, dictionary configuration, and implementation details.
/// </para>
/// </remarks>
public enum EvictingDictionaryPolicy
{
    /// <summary>
    /// First-In, First-Out (FIFO): evicts the item that was added earliest, regardless of usage.
    /// </summary>
    FirstInFirstOut,

    /// <summary>
    /// Least Recently Used (LRU): evicts the item that has not been accessed for the longest time.
    /// </summary>
    LeastRecentlyUsed,

    /// <summary>
    /// Least Frequently Used (LFU): evicts the item with the fewest total accesses.
    /// </summary>
    LeastFrequentlyUsed,

    /// <summary>
    /// Most Recently Used (MRU): evicts the most recently accessed item.
    /// </summary>
    MostRecentlyUsed,

    /// <summary>
    /// Random Replacement: evicts a randomly selected item.
    /// </summary>
    RandomReplacement,

    /// <summary>
    /// Second Chance: evicts the first item found without a recent access flag, allowing items a second chance before
    /// removal.
    /// </summary>
    SecondChance,
}
