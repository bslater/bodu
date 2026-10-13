// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoundedConcurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Runs an asynchronous operation over a sequence of download units with at most a fixed number in flight at once, so a
/// provider that loads several units (RBA eras, IMF months) does not wait for each download before starting the next.
/// </summary>
/// <remarks>
/// This type is compiled into each provider that loads several units for one request.
/// </remarks>
internal static class BoundedConcurrency
{
    /// <summary>The number of downloads a provider runs at once against one source.</summary>
    internal const int DefaultLimit = 4;

    /// <summary>
    /// Runs <paramref name="body" /> for each item, starting a new item whenever fewer than <paramref name="limit" />
    /// are in flight.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="items">The items to run the operation for, started in order.</param>
    /// <param name="limit">The largest number of operations in flight at once.</param>
    /// <param name="body">The operation to run for each item.</param>
    /// <returns>A task that completes when every started operation has completed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> or <paramref name="body" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="limit" /> is less than one.
    /// </exception>
    /// <remarks>
    /// Operations start synchronously, in order, up to the limit. Once an operation fails, no further item is started;
    /// the operations already in flight are awaited, and then the first failure observed is rethrown.
    /// </remarks>
    internal static Task ForEachAsync<T>(IEnumerable<T> items, int limit, Func<T, Task> body)
    {
        throw new NotImplementedException();
    }
}
