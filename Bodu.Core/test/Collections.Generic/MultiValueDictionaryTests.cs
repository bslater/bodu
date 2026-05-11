// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

/// <summary>
/// Unit tests for <see cref="MultiValueDictionary{TKey, TValue}"/>.
/// </summary>
[TestClass]
public partial class MultiValueDictionaryTests
{    /// <summary>Value-type key with structural (value-based) equality via record struct.</summary>
    private readonly record struct Coord(int Row, int Col);

    /// <summary>Reference-type value with no overridden equality — uses reference identity.</summary>
    private sealed class Label
    {
        public string Text { get; }

        public Label(string text) { Text = text; }
    }

    private static void AssertReadOnlyValueViewCannotBeMutatedForValueViewTests(IReadOnlyList<int> values)
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
}