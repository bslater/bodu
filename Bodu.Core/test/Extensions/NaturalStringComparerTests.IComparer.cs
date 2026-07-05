// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.IComparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IComparer.Compare(object?, object?)" />
    /// implementation delegates to the natural string comparison for string operands.
    /// </summary>
    [TestMethod]
    public void IComparerCompare_WhenOperandsAreStrings_ShouldReturnNaturalOrder()
    {
        System.Collections.IComparer comparer = NaturalStringComparer.Ordinal;

        Assert.IsTrue(comparer.Compare("file2", "file10") < 0);
        Assert.IsTrue(comparer.Compare("file10", "file2") > 0);
        Assert.AreEqual(0, comparer.Compare("file1", "file1"));
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IComparer.Compare(object?, object?)" />
    /// implementation sorts <see langword="null" /> before any string and treats two <see langword="null" />
    /// references as equal.
    /// </summary>
    [TestMethod]
    public void IComparerCompare_WhenOperandsIncludeNull_ShouldSortNullFirst()
    {
        System.Collections.IComparer comparer = NaturalStringComparer.Ordinal;

        Assert.AreEqual(0, comparer.Compare(null, null));
        Assert.IsTrue(comparer.Compare(null, "file1") < 0);
        Assert.IsTrue(comparer.Compare("file1", null) > 0);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IComparer.Compare(object?, object?)" />
    /// implementation throws <see cref="ArgumentException" /> identifying the first operand when it is not a string.
    /// </summary>
    [TestMethod]
    public void IComparerCompare_WhenFirstOperandIsNotString_ShouldThrowArgumentException()
    {
        System.Collections.IComparer comparer = NaturalStringComparer.Ordinal;

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = comparer.Compare(42, "file1");
        });

        Assert.AreEqual("x", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IComparer.Compare(object?, object?)" />
    /// implementation throws <see cref="ArgumentException" /> identifying the second operand when it is not a string.
    /// </summary>
    [TestMethod]
    public void IComparerCompare_WhenSecondOperandIsNotString_ShouldThrowArgumentException()
    {
        System.Collections.IComparer comparer = NaturalStringComparer.Ordinal;

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = comparer.Compare("file1", 42);
        });

        Assert.AreEqual("y", ex.ParamName);
    }
}
