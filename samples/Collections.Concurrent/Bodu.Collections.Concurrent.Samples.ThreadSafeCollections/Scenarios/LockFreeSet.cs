// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LockFreeSet.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates <see cref="ConcurrentHashSet{T}" />: a lock-free split-ordered set whose <c>Add</c> reports whether
/// the element was new, making it a test-and-set rather than a fire-and-forget insert.
/// </summary>
/// <remarks>
/// Everything runs single-threaded so the transcript is reproducible. Snapshots are sorted before printing because
/// iteration order is unspecified — a split-ordered set stores elements by hash bucket, not by insertion, so relying
/// on the order you see would be relying on an implementation detail.
/// </remarks>
public static class LockFreeSet
{
    /// <summary>
    /// Exercises the add/contains/remove contract, the in-place set-algebra operators, and the non-mutating
    /// relationship predicates.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "ConcurrentHashSet<T> - lock-free set",
            what: "Adds a duplicate and removes a missing element to show what the return values mean, then runs " +
                  "union, except and intersect in place over a fresh {1,2,3,4}, and finishes with the predicates " +
                  "that answer relationship questions without mutating.",
            why: "Add returning a bool is what makes this set usable as a concurrency primitive rather than just a " +
                 "container. The return value is the atomic answer to \"did I win the race to insert this?\", so it " +
                 "serves as a lock-free claim check - exactly once semantics for a de-duplicating worker, for " +
                 "instance - which a fire-and-forget Add followed by a separate Contains cannot give you: between " +
                 "those two calls another thread may act.",
            expect: "Every first operation on an element succeeds and every repeat fails: Add 1 is True then False, " +
                    "Remove 2 is True then False. The algebra results are the arithmetic ones, and all three " +
                    "predicates are True. Sets print sorted, so the order shown is the sort, not the storage order.");

        // Add returns true only when the element was newly inserted, so it doubles as the type's "try-add": a
        // repeated element reports false and leaves the set untouched. That answer is atomic - no separate Contains
        // check is needed, and no window exists for another thread to slip between the test and the set.
        var set = new ConcurrentHashSet<int>();
        Console.WriteLine("  Add / Contains / Remove - the return value is the claim check:");
        Console.WriteLine($"    Add 1 (new)    : {set.Add(1)}  (expected True - this caller inserted it)");
        Console.WriteLine($"    Add 2 (new)    : {set.Add(2)}  (expected True)");
        Console.WriteLine($"    Add 1 (repeat) : {set.Add(1)}  (expected False - already present, set unchanged)");
        Console.WriteLine($"    Contains 2     : {set.Contains(2)}  (expected True)");
        Console.WriteLine($"    Remove 2       : {set.Remove(2)}  (expected True - this caller removed it)");
        Console.WriteLine($"    Remove 2 again : {set.Remove(2)}  (expected False - already gone, not an error)");
        Console.WriteLine($"    Count          : {set.Count}  (expected 1 - only element 1 survives)");
        Console.WriteLine();

        // The set-algebra operators mutate in place rather than returning a new set, so each line starts from a
        // fresh copy of {1, 2, 3, 4} to keep the three results independent.
        Console.WriteLine("  Set algebra (in place, so each line starts from a fresh {1,2,3,4}):");
        Console.WriteLine($"    | {{4,5,6}}      : {Show(Seed().Also(s => s.UnionWith(new[] { 4, 5, 6 })))}  (expected {{1,2,3,4,5,6}} - 4 was already present and is not duplicated)");
        Console.WriteLine($"    - {{2,4}}       : {Show(Seed().Also(s => s.ExceptWith(new[] { 2, 4 })))}  (expected {{1,3}})");
        Console.WriteLine($"    & {{2,4,8}}     : {Show(Seed().Also(s => s.IntersectWith(new[] { 2, 4, 8 })))}  (expected {{2,4}} - 8 is absent, so it contributes nothing)");
        Console.WriteLine();

        // The predicates answer relationship questions and leave the set alone, so one instance serves all three.
        var abcd = Seed();
        Console.WriteLine("  Predicates (non-mutating, all against {1,2,3,4}):");
        Console.WriteLine($"    IsSupersetOf {{2,3}}   : {abcd.IsSupersetOf(new[] { 2, 3 })}  (expected True - both are present)");
        Console.WriteLine($"    Overlaps {{9,4}}       : {abcd.Overlaps(new[] { 9, 4 })}  (expected True - one shared element is enough)");
        Console.WriteLine($"    SetEquals {{4,3,2,1}}  : {abcd.SetEquals(new[] { 4, 3, 2, 1 })}  (expected True - a set has no order, so the sequence is irrelevant)");

        Console.WriteLine();
    }

    /// <summary>
    /// Creates a fresh set seeded with the elements 1 through 4.
    /// </summary>
    /// <returns>A new <see cref="ConcurrentHashSet{T}" /> containing <c>{1, 2, 3, 4}</c>.</returns>
    private static ConcurrentHashSet<int> Seed() =>
        new(new[] { 1, 2, 3, 4 });

    /// <summary>
    /// Renders a set as a sorted, comma-separated list so the output is stable despite unspecified
    /// iteration order.
    /// </summary>
    /// <param name="set">The set to render.</param>
    /// <returns>A brace-wrapped, sorted textual view of the set's elements.</returns>
    private static string Show(ConcurrentHashSet<int> set)
    {
        var items = set.ToArray();
        Array.Sort(items);
        return $"{{{string.Join(",", items)}}}";
    }

    /// <summary>
    /// Applies a mutating action to the set and returns the same instance, so seed-mutate-render reads
    /// as a single expression.
    /// </summary>
    /// <param name="set">The set to mutate.</param>
    /// <param name="action">The in-place mutation to apply.</param>
    /// <returns>The same <paramref name="set" /> instance, after <paramref name="action" /> has run.</returns>
    private static ConcurrentHashSet<int> Also(this ConcurrentHashSet<int> set, Action<ConcurrentHashSet<int>> action)
    {
        action(set);
        return set;
    }
}
