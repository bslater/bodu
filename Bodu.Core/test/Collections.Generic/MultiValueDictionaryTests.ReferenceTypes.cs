// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.ReferenceTypes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that two distinct <see cref="Label" /> instances with the same text are stored as separate
    /// values, because <see cref="Label" /> uses reference identity rather than structural equality.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsReferenceType_ShouldStoreBothInstances()
    {
        var l1 = new Label("hello");
        var l2 = new Label("hello");

        var mvd = new MultiValueDictionary<string, Label>();
        mvd.Add("k", l1);
        mvd.Add("k", l2);

        Assert.AreEqual(2, mvd.Count);
        Assert.HasCount(2, mvd["k"]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue" /> uses reference equality
    /// for reference-type values that do not override <see cref="object.Equals(object)" />.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsReferenceTypeAndSameInstance_ShouldReturnTrue()
    {
        var l1 = new Label("hello");
        var l2 = new Label("hello");

        var mvd = new MultiValueDictionary<string, Label>();
        mvd.Add("k", l1);

        Assert.IsTrue(mvd.ContainsValue("k", l1));
        Assert.IsFalse(mvd.ContainsValue("k", l2));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)" /> removes only the
    /// specific <see cref="Label" /> instance requested, leaving other instances untouched.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsReferenceType_ShouldRemoveOnlyRequestedInstance()
    {
        var l1 = new Label("hello");
        var l2 = new Label("hello");

        var mvd = new MultiValueDictionary<string, Label>();
        mvd.Add("k", l1);
        mvd.Add("k", l2);

        var removed = mvd.Remove("k", l1);

        Assert.IsTrue(removed);
        Assert.AreEqual(1, mvd.Count);
        Assert.IsTrue(mvd.ContainsValue("k", l2));
        Assert.IsFalse(mvd.ContainsValue("k", l1));
    }

}
