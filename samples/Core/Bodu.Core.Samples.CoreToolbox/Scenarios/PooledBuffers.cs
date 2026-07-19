// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBuffers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Buffers;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates <see cref="PooledBufferBuilder{T}" />: a growable, <see cref="System.Buffers.IBufferWriter{T}" />-style
/// builder that rents its backing storage from the shared <see cref="System.Buffers.ArrayPool{T}" /> instead of
/// allocating on the managed heap. It is the pattern for assembling a variable-length buffer without the
/// intermediate <c>List&lt;T&gt;</c> / repeated-<c>Array.Resize</c> garbage.
/// </summary>
public static class PooledBuffers
{
    /// <summary>
    /// Builds a buffer through the item, span, and repeat-fill APIs, reads it back through the zero-copy views,
    /// and returns the rented storage to the pool via <c>Dispose</c>.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- PooledBufferBuilder<T>: pooled, growable buffers ---");

        // A small initial capacity deliberately forces at least one internal grow so the sample exercises the
        // rent / copy / return path rather than a single fixed rental.
        var builder = new PooledBufferBuilder<int>(initialCapacity: 4);

        // Append single items one at a time.
        builder.Append(1);
        builder.Append(2);
        builder.Append(3);

        // Append a whole span in one call - this crosses the initial capacity and triggers a grow.
        ReadOnlySpan<int> more = stackalloc int[] { 4, 5, 6, 7 };
        builder.AppendRange(more);

        // AddMany repeats a single value - handy for padding or fills.
        builder.AddMany(value: 0, count: 2);

        Console.WriteLine($"WrittenCount     : {builder.WrittenCount}");
        Console.WriteLine($"IsEmpty          : {builder.IsEmpty}");

        // WrittenSpan is a zero-copy view over exactly the written region (no trailing free capacity).
        var sum = 0;
        foreach (var value in builder.WrittenSpan)
            sum += value;
        Console.WriteLine($"WrittenSpan sum  : {sum}");

        // ToArrayAndDispose snapshots the written region into a right-sized array AND returns the rented
        // buffer to the pool in one step, so there is no separate Dispose call afterwards.
        var snapshot = builder.ToArrayAndDispose();
        Console.WriteLine($"Snapshot         : [{string.Join(", ", snapshot)}]");

        Console.WriteLine();
    }
}
