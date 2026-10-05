// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>Contains</c> answers <see langword="true" /> for the selected values only, value by value over
    /// the domain.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetSelectsValues_ShouldReportEachValueSelected()
    {
        int[] selected = [0, DomainSize / 3, DomainSize - 1];
        TSet set = Create(selected.Select(ElementAt).ToArray());

        for (int index = 0; index < DomainSize; index++)
        {
            bool expected = selected.Contains(index);

            Assert.AreEqual(expected, Contains(set, ElementAt(index)), $"value {ElementAt(index)}");
        }
    }

    /// <summary>
    /// Verifies that <c>Contains</c> answers <see langword="false" /> for a value outside the domain, rather than
    /// throwing, even on a set that selects every value in it.
    /// </summary>
    [TestMethod]
    public void Contains_WhenValueIsOutsideDomain_ShouldReturnFalse()
    {
        foreach (TElement value in ElementsOutsideDomain)
        {
            Assert.IsFalse(Contains(All, value), $"value {value}");
            Assert.IsFalse(Contains(Empty, value), $"value {value}");
        }
    }
}
