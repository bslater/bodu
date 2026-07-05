// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Values.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Values" /> enumerates values in iteration order.
    /// </summary>
    [TestMethod]
    public void Values_WhenEnumerated_ShouldYieldValuesInOrder()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, dictionary.Values.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Values" /> returns the same cached instance on repeated reads.
    /// </summary>
    [TestMethod]
    public void Values_WhenAccessedRepeatedly_ShouldReturnCachedInstance()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.AreSame(dictionary.Values, dictionary.Values);
    }

    /// <summary>
    /// Verifies that the values view reports membership through <see cref="ICollection{T}.Contains" />.
    /// </summary>
    [TestMethod]
    public void Values_WhenContainsCalled_ShouldReportMembership()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        Assert.IsTrue(dictionary.Values.Contains(2));
        Assert.IsFalse(dictionary.Values.Contains(99));
    }

    /// <summary>
    /// Verifies that mutating the dictionary through the values view throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Values_WhenRemoveCalled_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();
        ICollection<int> values = dictionary.Values;

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            values.Remove(1);
        });
    }
}
