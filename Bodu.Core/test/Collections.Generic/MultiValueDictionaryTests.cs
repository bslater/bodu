// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="MultiValueDictionary{TKey, TValue}"/>.
/// </summary>
[TestClass]
public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Asserts that <paramref name="values"/> is not the mutable backing <see cref="List{T}"/> and that all
    /// mutating operations through <see cref="ICollection{T}"/> throw <see cref="NotSupportedException"/>.
    /// </summary>
    private static void AssertReadOnlyValueViewCannotBeMutated(IReadOnlyList<int> values)
    {
        Assert.IsFalse(values is List<int>, "The returned value view must not be the mutable backing List<T>.");

        if (values is ICollection<int> collection)
        {
            Assert.IsTrue(
                collection.IsReadOnly,
                "The returned value view must not expose a mutable ICollection<T>.");

            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                collection.Add(999);
            });
        }
    }

    /// <summary>
    /// Yields one value then throws <see cref="InvalidOperationException"/> to simulate a faulting source sequence.
    /// </summary>
    private static IEnumerable<int> EnumerateOneThenThrow()
    {
        yield return 1;

        throw new InvalidOperationException("Simulated source enumeration failure.");
    }

    /// <summary>Value-type key with structural (value-based) equality via record struct.</summary>
    private readonly record struct Coord(int Row, int Col);

    /// <summary>Reference-type value with no overridden equality — uses reference identity.</summary>
    private sealed class Label
    {

        public Label(string text) { Text = text; }

        public string Text { get; }

    }

}
