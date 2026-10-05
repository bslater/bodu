// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that the constructor selects exactly the values it is given, whatever their order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Ctor_WhenValuesProvided_ShouldSelectExactlyThoseValues()
    {
        int middle = DomainSize / 2;
        int last = DomainSize - 1;

        TSet set = Create(ElementAt(last), ElementAt(0), ElementAt(middle));

        AssertSelects(i => i == 0 || i == middle || i == last, set, "first, middle and last values");
    }

    /// <summary>
    /// Verifies that the constructor selects each value of the domain on its own.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOneValueProvided_ShouldSelectOnlyThatValue()
    {
        for (int index = 0; index < DomainSize; index++)
        {
            int selected = index;

            AssertSelects(i => i == selected, Create(ElementAt(index)), $"value {ElementAt(index)}");
        }
    }

    /// <summary>
    /// Verifies that a value given more than once is selected, and counted, once.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValuesRepeat_ShouldSelectEachOnce()
    {
        TSet set = Create(ElementAt(1), ElementAt(1), ElementAt(0), ElementAt(1));

        Assert.AreEqual(2, Count(set));
        Assert.AreEqual(Create(ElementAt(0), ElementAt(1)), set);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> array creates the empty set rather than throwing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValuesAreNull_ShouldCreateEmptySet()
    {
        TSet set = Create(null);

        Assert.AreEqual(Empty, set);
    }

    /// <summary>
    /// Verifies that no values create the empty set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNoValuesProvided_ShouldCreateEmptySet()
    {
        TSet set = Create();

        Assert.AreEqual(Empty, set);
    }

    /// <summary>
    /// Verifies that every value of the domain creates the set equal to <c>All</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEveryValueProvided_ShouldEqualAll()
    {
        TSet set = Create(Domain.Reverse().ToArray());

        Assert.AreEqual(All, set);
    }

    /// <summary>
    /// Verifies that a value outside the domain throws <see cref="ArgumentOutOfRangeException" /> naming the
    /// constructor's parameter, even when it follows a valid value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueIsOutsideDomain_ShouldThrowArgumentOutOfRangeException()
    {
        foreach (TElement value in ElementsOutsideDomain)
        {
            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = Create(ElementAt(0), value);
            }, $"value {value}");

            Assert.AreEqual(ConstructorParameterName, ex.ParamName, $"value {value}");
        }
    }
}
