// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.IsOneOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsOneOf(string, string[])" /> returns <see langword="true" />
    /// when the value matches one of the candidates under ordinal comparison.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenValueMatchesACandidate_ShouldReturnTrue()
    {
        Assert.IsTrue("beta".IsOneOf("alpha", "beta", "gamma"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsOneOf(string, string[])" /> returns <see langword="false" />
    /// when no candidate matches under ordinal comparison.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenValueDoesNotMatchAnyCandidate_ShouldReturnFalse()
    {
        Assert.IsFalse("delta".IsOneOf("alpha", "beta", "gamma"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsOneOf(string, string[])" /> treats <see langword="null" />
    /// elements as non-matching (the receiver is never null per the guard).
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenCandidateArrayContainsNull_ShouldNotMatchNull()
    {
        Assert.IsFalse("beta".IsOneOf("alpha", null!, "gamma"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsOneOf(string, string[])" /> respects ordinal
    /// case-sensitivity by default.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenComparedOrdinally_ShouldBeCaseSensitive()
    {
        Assert.IsFalse("BETA".IsOneOf("alpha", "beta", "gamma"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsOneOf(string, IEqualityComparer{string}, string[])" />
    /// honours a case-insensitive comparer when supplied.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenCaseInsensitiveComparerSupplied_ShouldIgnoreCase()
    {
        Assert.IsTrue("BETA".IsOneOf(StringComparer.OrdinalIgnoreCase, "alpha", "beta", "gamma"));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsOneOf(string, string[])" /> returns <see langword="false" />
    /// when the candidate set is empty.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenCandidateArrayIsEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse("alpha".IsOneOf());
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="StringExtensions.IsOneOf(string, string[])" /> overload
    /// throws <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenValueIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.IsOneOf(null!, "alpha");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the comparer overload throws <see cref="ArgumentNullException" /> when
    /// <c>comparer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsOneOf_WhenComparerIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "alpha".IsOneOf((IEqualityComparer<string>)null!, "alpha");
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }
}
