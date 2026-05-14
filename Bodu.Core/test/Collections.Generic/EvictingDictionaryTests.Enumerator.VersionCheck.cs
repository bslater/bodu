// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Enumerator.VersionCheck.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that mutating an <see cref="EvictingDictionary{TKey, TValue}" /> after starting enumeration causes the next
    /// <c>MoveNext</c> call to throw <see cref="InvalidOperationException" />, exercising the internal
    /// <c>ThrowIfVersionChanged</c> guard.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDictionaryIsMutatedDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        using IEnumerator<KeyValuePair<string, int>> enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        // Mutate the dictionary while the enumerator is still in flight.
        dictionary.Add("D", 4);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }
}
