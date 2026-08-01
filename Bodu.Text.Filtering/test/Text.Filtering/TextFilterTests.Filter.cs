// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterTests.Filter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilter.Filter" /> and <see cref="TextFilter.FilterToList" />.
/// </content>
public partial class TextFilterTests
{
    /// <summary>
    /// Verifies that filtering preserves source order and keeps exactly the accepted values.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSourceHasMixedValues_ShouldKeepAcceptedInOrder()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("e*"), TextFilterPattern.Exclude("*2")]);

        var results = filter.Filter(["e1", "x", "e2", "e3"]).ToArray();

        CollectionAssert.AreEqual(new[] { "e1", "e3" }, results);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> source throws <see cref="ArgumentNullException" /> eagerly, before
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var filter = TextFilter.Build([]);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Filter(null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that evaluation is deferred: values are not evaluated until the result is enumerated.
    /// </summary>
    [TestMethod]
    public void Filter_WhenResultNotEnumerated_ShouldNotEvaluate()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("e*")]);

        var deferred = filter.Filter(["e1", "e2"]);

        Assert.AreEqual(0, filter.GetStatistics().ItemsEvaluated);
        _ = deferred.ToArray();
        Assert.AreEqual(2, filter.GetStatistics().ItemsEvaluated);
    }

    /// <summary>
    /// Verifies that <see langword="null" /> elements are dropped without being evaluated.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSourceContainsNullElements_ShouldDropThemWithoutEvaluating()
    {
        var filter = TextFilter.Build([]);

        var results = filter.Filter(["a", null!, "b"]).ToArray();

        CollectionAssert.AreEqual(new[] { "a", "b" }, results);
        Assert.AreEqual(2, filter.GetStatistics().ItemsEvaluated);
    }

    /// <summary>
    /// Verifies that the eager list surface produces the same result as the deferred sequence surface.
    /// </summary>
    [TestMethod]
    public void FilterToList_WhenGivenSameSource_ShouldMatchFilterResults()
    {
        var filter = TextFilter.Build([TextFilterPattern.Include("{e,w}*"), TextFilterPattern.Exclude("*9")]);
        string[] source = ["e1", "w2", "x3", "e9", "w4"];

        var eager = filter.FilterToList(source);
        var deferred = filter.Filter(source).ToList();

        CollectionAssert.AreEqual(deferred, eager);
    }

    /// <summary>
    /// Verifies that the eager surface rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void FilterToList_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var filter = TextFilter.Build([]);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.FilterToList(null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
