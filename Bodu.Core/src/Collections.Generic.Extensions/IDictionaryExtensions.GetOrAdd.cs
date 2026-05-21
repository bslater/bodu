// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDictionaryExtensions.GetOrAdd.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IDictionaryExtensions
{
    /// <summary>
    /// Returns the value associated with <paramref name="key" />, adding <paramref name="value" /> first when the key
    /// is not already present.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to read from and, when necessary, insert into.</param>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">The value to insert when <paramref name="key" /> is not present.</param>
    /// <returns>
    /// The existing value when <paramref name="key" /> is already present; otherwise <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dictionary" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The lookup and the conditional insert are performed as two separate operations; the helper provides no atomicity
    /// guarantee and must not be used as a substitute for
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, TValue)" /> on a
    /// shared instance.
    /// </remarks>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(dictionary);

        if (dictionary.TryGetValue(key, out TValue? existing))
            return existing;

        dictionary[key] = value;
        return value;
    }

    /// <summary>
    /// Returns the value associated with <paramref name="key" />, invoking <paramref name="valueFactory" /> to produce
    /// and insert a value when the key is not already present.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to read from and, when necessary, insert into.</param>
    /// <param name="key">The key to locate.</param>
    /// <param name="valueFactory">A delegate that produces the value to insert, given the key.</param>
    /// <returns>
    /// The existing value when <paramref name="key" /> is already present; otherwise the value produced by
    /// <paramref name="valueFactory" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dictionary" /> or <paramref name="valueFactory" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <paramref name="valueFactory" /> is invoked at most once, and only when <paramref name="key" /> is absent. The
    /// lookup and insert are not atomic; see the value-argument overload for the concurrency caveat.
    /// </remarks>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
    {
        ThrowHelper.ThrowIfNull(dictionary);
        ThrowHelper.ThrowIfNull(valueFactory);

        if (dictionary.TryGetValue(key, out TValue? existing))
            return existing;

        TValue created = valueFactory(key);
        dictionary[key] = created;
        return created;
    }
}
