// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDictionaryExtensions.AddOrUpdate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IDictionaryExtensions
{
    /// <summary>
    /// Inserts <paramref name="value" /> under <paramref name="key" />, or replaces the existing value when the key is
    /// already present.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to insert into or update.</param>
    /// <param name="key">The key to insert or update.</param>
    /// <param name="value">The value to associate with <paramref name="key" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dictionary" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The operation is unconditional: it has the same effect as the indexer assignment <c>dictionary[key] = value</c>,
    /// and exists to state the add-or-update intent explicitly at the call site.
    /// </remarks>
    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(dictionary);

        dictionary[key] = value;
    }
}
