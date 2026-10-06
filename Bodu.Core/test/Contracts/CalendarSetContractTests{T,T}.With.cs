// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.With.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>With</c> adds the value it is given and leaves every other value as it was.
    /// </summary>
    [TestMethod]
    public void With_WhenValueIsInDomain_ShouldAddOnlyThatValue()
    {
        foreach (TSet set in SampleSets().Take(16))
        {
            bool[] before = Membership(set);

            for (int index = 0; index < DomainSize; index++)
            {
                int added = index;
                TSet result = With(set, ElementAt(index));

                AssertSelects(i => before[i] || i == added, result, $"set {ToUInt64(set):X} with {ElementAt(index)}");
            }
        }
    }

    /// <summary>
    /// Verifies that <c>With</c> leaves the set it is called on unchanged, since the type is immutable.
    /// </summary>
    [TestMethod]
    public void With_WhenCalled_ShouldNotChangeOriginal()
    {
        TSet original = Create(ElementAt(0));

        _ = With(original, ElementAt(1));

        Assert.AreEqual(Create(ElementAt(0)), original);
    }

    /// <summary>
    /// Verifies that <c>With</c> throws <see cref="ArgumentOutOfRangeException" /> naming its parameter for a value
    /// outside the domain.
    /// </summary>
    [TestMethod]
    public void With_WhenValueIsOutsideDomain_ShouldThrowArgumentOutOfRangeException()
    {
        foreach (TElement value in ElementsOutsideDomain)
        {
            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = With(Empty, value);
            }, $"value {value}");

            Assert.AreEqual(ElementParameterName, ex.ParamName, $"value {value}");
        }
    }
}
