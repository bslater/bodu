// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> value factory throws <see cref="ArgumentNullException" /> on every
    /// constructor overload.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueFactoryIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DefaultingDictionary<string, int>(null!);
        });

        Assert.AreEqual("valueFactory", ex.ParamName);

        ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DefaultingDictionary<string, int>(null!, StringComparer.Ordinal);
        });

        Assert.AreEqual("valueFactory", ex.ParamName);

        ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DefaultingDictionary<string, int>(null!, 4, StringComparer.Ordinal);
        });

        Assert.AreEqual("valueFactory", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the factory-only constructor creates an empty dictionary using the default comparer and exposes
    /// the factory through <see cref="DefaultingDictionary{TKey, TValue}.ValueFactory" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFactoryOnly_ShouldUseDefaultsAndExposeFactory()
    {
        Func<string, int> factory = key => key.Length;

        var dictionary = new DefaultingDictionary<string, int>(factory);

        Assert.AreEqual(0, dictionary.Count);
        Assert.AreSame(factory, dictionary.ValueFactory);
        Assert.AreSame(EqualityComparer<string>.Default, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that a supplied comparer is stored and exposed through
    /// <see cref="DefaultingDictionary{TKey, TValue}.Comparer" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldExposeComparer()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0, StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer falls back to the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var dictionary = new DefaultingDictionary<string, int>(_ => 0, null);

        Assert.AreSame(EqualityComparer<string>.Default, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that the capacity constructor accepts zero and positive capacities.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNonNegative_ShouldCreateEmptyDictionary()
    {
        var zero = new DefaultingDictionary<string, int>(_ => 0, 0, null);
        var positive = new DefaultingDictionary<string, int>(_ => 0, 16, null);

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
            _ = new DefaultingDictionary<string, int>(_ => 0, -1, null);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }
}
