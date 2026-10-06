// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Without.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>Without</c> removes the value it is given and leaves every other value as it was.
    /// </summary>
    [TestMethod]
    public void Without_WhenValueIsInDomain_ShouldRemoveOnlyThatValue()
    {
        foreach (TSet set in SampleSets().Take(16))
        {
            bool[] before = Membership(set);

            for (int index = 0; index < DomainSize; index++)
            {
                int removed = index;
                TSet result = Without(set, ElementAt(index));

                AssertSelects(i => before[i] && i != removed, result, $"set {ToUInt64(set):X} without {ElementAt(index)}");
            }
        }
    }

    /// <summary>
    /// Verifies that <c>Without</c> leaves the set it is called on unchanged, since the type is immutable.
    /// </summary>
    [TestMethod]
    public void Without_WhenCalled_ShouldNotChangeOriginal()
    {
        TSet original = Create(ElementAt(0), ElementAt(1));

        _ = Without(original, ElementAt(1));

        Assert.AreEqual(Create(ElementAt(0), ElementAt(1)), original);
    }

    /// <summary>
    /// Verifies that <c>Without</c> throws <see cref="ArgumentOutOfRangeException" /> naming its parameter for a value
    /// outside the domain.
    /// </summary>
    [TestMethod]
    public void Without_WhenValueIsOutsideDomain_ShouldThrowArgumentOutOfRangeException()
    {
        foreach (TElement value in ElementsOutsideDomain)
        {
            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = Without(All, value);
            }, $"value {value}");

            Assert.AreEqual(ElementParameterName, ex.ParamName, $"value {value}");
        }
    }
}
