// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryViewCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Implements the view plumbing shared by <see cref="EvictingDictionary{TKey, TValue}" /> and
/// <see cref="SequencedDictionary{TKey, TValue}" /> over the <see cref="IOrderedDictionaryView{TKey, TValue}" /> seam:
/// the key/value copy loops (typed and non-generic), the <see cref="DictionaryEntry" /> copy used by the dictionaries'
/// non-generic <see cref="ICollection.CopyTo" />, the value-containment scan, and the lazily allocated
/// <see cref="ICollection.SyncRoot" /> object.
/// </summary>
/// <remarks>
/// <para>
/// Every copy method applies the guard sequence the two dictionaries previously duplicated: argument-shape validation
/// first (so a caller error can never trigger the owner's pre-copy hook, which for the evicting dictionary purges
/// expired entries and raises eviction events), then
/// <see cref="IOrderedDictionaryView{TKey, TValue}.PrepareViewCopy" />, then the destination range check against
/// <see cref="IOrderedDictionaryView{TKey, TValue}.ViewCount" /> so the count validated matches the elements written.
/// </para>
/// <para>
/// These are cold copy/scan paths; they enumerate through the seam's boxed enumerator. Hot enumeration stays on each
/// dictionary's public struct enumerator.
/// </para>
/// </remarks>
internal static class DictionaryViewCore
{
    /// <summary>
    /// Returns the lazily allocated synchronization root for a dictionary, creating it on first access with a
    /// compare-and-swap so no allocation is duplicated under contention.
    /// </summary>
    /// <param name="syncRoot">The owning dictionary's sync-root field.</param>
    /// <returns>The shared synchronization root object.</returns>
    internal static object GetSyncRoot(ref object? syncRoot) =>
        syncRoot ?? Interlocked.CompareExchange(ref syncRoot, new object(), null) ?? syncRoot!;

    /// <summary>
    /// Copies the dictionary's keys, in its defining order, into the specified typed array starting at the given
    /// offset.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the source dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the source dictionary.</typeparam>
    /// <param name="source">The dictionary whose keys are copied.</param>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// The destination range starting at <paramref name="arrayIndex" /> is too small for the dictionary's entries.
    /// </exception>
    internal static void CopyKeysTo<TKey, TValue>(IOrderedDictionaryView<TKey, TValue> source, TKey[] array, int arrayIndex)
        where TKey : notnull
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);

        source.PrepareViewCopy();

        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, arrayIndex, source.ViewCount);

        using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = source.GetViewEnumerator();
        while (enumerator.MoveNext())
            array[arrayIndex++] = enumerator.Current.Key;
    }

    /// <summary>
    /// Copies the dictionary's keys, in its defining order, into the specified non-generic array starting at the given
    /// offset.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the source dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the source dictionary.</typeparam>
    /// <param name="source">The dictionary whose keys are copied.</param>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> is multidimensional, has a non-zero lower bound, or the destination range starting at
    /// <paramref name="index" /> is too small for the dictionary's entries.
    /// </exception>
    internal static void CopyKeysTo<TKey, TValue>(IOrderedDictionaryView<TKey, TValue> source, Array array, int index)
        where TKey : notnull
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(index, 0);

        source.PrepareViewCopy();

        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, source.ViewCount);

        using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = source.GetViewEnumerator();
        while (enumerator.MoveNext())
            array.SetValue(enumerator.Current.Key, index++);
    }

    /// <summary>
    /// Copies the dictionary's values, in its defining order, into the specified typed array starting at the given
    /// offset.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the source dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the source dictionary.</typeparam>
    /// <param name="source">The dictionary whose values are copied.</param>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// The destination range starting at <paramref name="arrayIndex" /> is too small for the dictionary's entries.
    /// </exception>
    internal static void CopyValuesTo<TKey, TValue>(IOrderedDictionaryView<TKey, TValue> source, TValue[] array, int arrayIndex)
        where TKey : notnull
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);

        source.PrepareViewCopy();

        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, arrayIndex, source.ViewCount);

        using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = source.GetViewEnumerator();
        while (enumerator.MoveNext())
            array[arrayIndex++] = enumerator.Current.Value;
    }

    /// <summary>
    /// Copies the dictionary's values, in its defining order, into the specified non-generic array starting at the
    /// given offset.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the source dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the source dictionary.</typeparam>
    /// <param name="source">The dictionary whose values are copied.</param>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> is multidimensional, has a non-zero lower bound, or the destination range starting at
    /// <paramref name="index" /> is too small for the dictionary's entries.
    /// </exception>
    internal static void CopyValuesTo<TKey, TValue>(IOrderedDictionaryView<TKey, TValue> source, Array array, int index)
        where TKey : notnull
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(index, 0);

        source.PrepareViewCopy();

        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, source.ViewCount);

        using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = source.GetViewEnumerator();
        while (enumerator.MoveNext())
            array.SetValue(enumerator.Current.Value, index++);
    }

    /// <summary>
    /// Copies the dictionary's entries, in its defining order, into the specified non-generic array as
    /// <see cref="DictionaryEntry" /> values starting at the given offset. Implements the dictionaries' non-generic
    /// <see cref="ICollection.CopyTo" />.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the source dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the source dictionary.</typeparam>
    /// <param name="source">The dictionary whose entries are copied.</param>
    /// <param name="array">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array" /> is multidimensional, has a non-zero lower bound, or is too short to hold the
    /// dictionary's entries starting at <paramref name="index" />.
    /// </exception>
    internal static void CopyEntriesTo<TKey, TValue>(IOrderedDictionaryView<TKey, TValue> source, Array array, int index)
        where TKey : notnull
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfLessThan(index, 0);

        source.PrepareViewCopy();

        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, index + source.ViewCount);

        using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = source.GetViewEnumerator();
        while (enumerator.MoveNext())
            array.SetValue(new DictionaryEntry(enumerator.Current.Key, enumerator.Current.Value), index++);
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified value, comparing with
    /// <see cref="EqualityComparer{T}.Default" /> over the ordered live entries.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the source dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the source dictionary.</typeparam>
    /// <param name="source">The dictionary whose values are scanned.</param>
    /// <param name="item">The value to locate.</param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains an entry with the specified value; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool ContainsValue<TKey, TValue>(IOrderedDictionaryView<TKey, TValue> source, TValue item)
        where TKey : notnull
    {
        EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;

        using IEnumerator<KeyValuePair<TKey, TValue>> enumerator = source.GetViewEnumerator();
        while (enumerator.MoveNext())
        {
            if (comparer.Equals(enumerator.Current.Value, item))
                return true;
        }

        return false;
    }
}
