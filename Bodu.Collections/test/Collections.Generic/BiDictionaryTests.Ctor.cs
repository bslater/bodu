// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an empty dictionary with the default policy and comparers.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldUseDefaults()
    {
        var dictionary = new BiDictionary<string, int>();

        Assert.AreEqual(0, dictionary.Count);
        Assert.AreEqual(BiDictionaryDuplicateValuePolicy.Throw, dictionary.DuplicateValuePolicy);
        Assert.AreSame(EqualityComparer<string>.Default, dictionary.KeyComparer);
        Assert.AreSame(EqualityComparer<int>.Default, dictionary.ValueComparer);
    }

    /// <summary>
    /// Verifies that the capacity constructor accepts zero and positive capacities.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNonNegative_ShouldCreateEmptyDictionary()
    {
        var zero = new BiDictionary<string, int>(0);
        var positive = new BiDictionary<string, int>(16);

        Assert.AreEqual(0, zero.Count);
        Assert.AreEqual(0, positive.Count);
    }

    /// <summary>
    /// Verifies that a negative capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BiDictionary<string, int>(-1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative capacity on the policy-accepting overload throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNegative_ForPolicyOverload_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace, -1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="BiDictionaryDuplicateValuePolicy" /> value throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPolicyIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new BiDictionary<string, int>((BiDictionaryDuplicateValuePolicy)42);
        });

        Assert.AreEqual("duplicateValuePolicy", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the comparer constructor stores the supplied comparers and exposes them through
    /// <see cref="BiDictionary{TKey, TValue}.KeyComparer" /> and <see cref="BiDictionary{TKey, TValue}.ValueComparer" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparersSupplied_ShouldExposeComparers()
    {
        var dictionary = new BiDictionary<string, string>(StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.KeyComparer);
        Assert.AreSame(StringComparer.Ordinal, dictionary.ValueComparer);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> comparers fall back to the default comparers.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparersAreNull_ShouldUseDefaultComparers()
    {
        var dictionary = new BiDictionary<string, int>(null, null);

        Assert.AreSame(EqualityComparer<string>.Default, dictionary.KeyComparer);
        Assert.AreSame(EqualityComparer<int>.Default, dictionary.ValueComparer);
    }

    /// <summary>
    /// Verifies that the policy constructor stores the supplied policy and exposes it through
    /// <see cref="BiDictionary{TKey, TValue}.DuplicateValuePolicy" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPolicySupplied_ShouldExposePolicy()
    {
        var dictionary = new BiDictionary<string, int>(BiDictionaryDuplicateValuePolicy.Replace);

        Assert.AreEqual(BiDictionaryDuplicateValuePolicy.Replace, dictionary.DuplicateValuePolicy);
    }

    /// <summary>
    /// Verifies that the full policy, capacity, and comparer constructor stores every supplied concern.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPolicyCapacityAndComparersSupplied_ShouldStoreAll()
    {
        var dictionary = new BiDictionary<string, string>(
            BiDictionaryDuplicateValuePolicy.Replace, 8, StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(BiDictionaryDuplicateValuePolicy.Replace, dictionary.DuplicateValuePolicy);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.KeyComparer);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.ValueComparer);
        Assert.AreEqual(0, dictionary.Count);
    }
}
