// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LockFreeSet.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Concurrent;

namespace Bodu.Collections.Concurrent.Samples.ThreadSafeCollections.Scenarios;

/// <summary>
/// Demonstrates <see cref="ConcurrentHashSet{T}" />: a lock-free split-ordered set. It shows the
/// idempotent add contract (<c>Add</c> returns whether the element was new), membership and removal,
/// and the in-place set-algebra operators. All work runs single-threaded so the output is
/// deterministic; snapshots are sorted before printing because iteration order is unspecified.
/// </summary>
public static class LockFreeSet
{
    /// <summary>
    /// Exercises the add/contains/remove contract and the union/intersect/except operators.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- ConcurrentHashSet<T>: lock-free set ---");

        // Add returns true only when the element was newly inserted, so it doubles as the type's
        // "try-add": a repeated element reports false without mutating the set.
        var set = new ConcurrentHashSet<int>();
        Console.WriteLine($"Add 1 (new)      : {set.Add(1)}");
        Console.WriteLine($"Add 2 (new)      : {set.Add(2)}");
        Console.WriteLine($"Add 1 (repeat)   : {set.Add(1)}");   // already present -> false
        Console.WriteLine($"Contains 2       : {set.Contains(2)}");
        Console.WriteLine($"Remove 2         : {set.Remove(2)}");
        Console.WriteLine($"Remove 2 (again) : {set.Remove(2)}");   // gone -> false
        Console.WriteLine($"Count            : {set.Count}");

        Console.WriteLine();

        // The set-algebra operators mutate in place, so each starts from a fresh copy of {1, 2, 3, 4}.
        Console.WriteLine($"union   {{1,2,3,4}} | {{4,5,6}} : {Show(Seed().Also(s => s.UnionWith(new[] { 4, 5, 6 })))}");
        Console.WriteLine($"except  {{1,2,3,4}} - {{2,4}}   : {Show(Seed().Also(s => s.ExceptWith(new[] { 2, 4 })))}");
        Console.WriteLine($"inter   {{1,2,3,4}} & {{2,4,8}} : {Show(Seed().Also(s => s.IntersectWith(new[] { 2, 4, 8 })))}");

        // The predicate operators answer relationship questions without mutating the set.
        var abcd = Seed();
        Console.WriteLine($"IsSupersetOf {{2,3}}       : {abcd.IsSupersetOf(new[] { 2, 3 })}");
        Console.WriteLine($"Overlaps     {{9,4}}       : {abcd.Overlaps(new[] { 9, 4 })}");
        Console.WriteLine($"SetEquals    {{4,3,2,1}}   : {abcd.SetEquals(new[] { 4, 3, 2, 1 })}");

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
