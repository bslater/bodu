// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Empty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>Empty</c> selects no value of the domain.
    /// </summary>
    [TestMethod]
    public void Empty_WhenAccessed_ShouldSelectNoValue()
    {
        AssertSelects(_ => false, Empty, "Empty");
        Assert.AreEqual(0UL, ToUInt64(Empty));
    }

    /// <summary>
    /// Verifies that <c>Empty</c> equals the default value of the type, so an uninitialized set is empty.
    /// </summary>
    [TestMethod]
    public void Empty_WhenComparedWithDefault_ShouldBeEqual()
    {
        Assert.AreEqual(default, Empty);
    }
}
