// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that the default constructor creates an empty table using the default comparers.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldCreateEmptyTableWithDefaultComparers()
    {
        var table = new Table<string, string, int>();

        Assert.AreEqual(0, table.Count);
        Assert.IsTrue(table.IsEmpty);
        Assert.AreSame(EqualityComparer<string>.Default, table.RowComparer);
        Assert.AreSame(EqualityComparer<string>.Default, table.ColumnComparer);
    }

    /// <summary>
    /// Verifies that the comparer constructor surfaces the supplied comparers through <c>RowComparer</c> and
    /// <c>ColumnComparer</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparersProvided_ShouldExposeThemThroughComparerProperties()
    {
        var table = new Table<string, string, int>(StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, table.RowComparer);
        Assert.AreSame(StringComparer.Ordinal, table.ColumnComparer);
    }

    /// <summary>
    /// Verifies that passing <see langword="null" /> comparers falls back to the default comparers.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparersAreNull_ShouldFallBackToDefaultComparers()
    {
        var table = new Table<string, string, int>(null, null);

        Assert.AreSame(EqualityComparer<string>.Default, table.RowComparer);
        Assert.AreSame(EqualityComparer<string>.Default, table.ColumnComparer);
    }
}
