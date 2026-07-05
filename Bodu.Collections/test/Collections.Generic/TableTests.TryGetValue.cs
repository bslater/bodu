// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class TableTests
{
    /// <summary>
    /// Verifies that <c>TryGetValue</c> returns <see langword="true" /> and the stored value for an existing cell.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenCellExists_ShouldReturnTrueAndValue()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsTrue(table.TryGetValue("r2", "c1", out int value));
        Assert.AreEqual(3, value);
    }

    /// <summary>
    /// Verifies that <c>TryGetValue</c> returns <see langword="false" /> and the default value when the row is
    /// absent.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenRowMissing_ShouldReturnFalseAndDefault()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsFalse(table.TryGetValue("missing", "c1", out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that <c>TryGetValue</c> returns <see langword="false" /> and the default value when the row exists
    /// but the column is absent.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenColumnMissingInExistingRow_ShouldReturnFalseAndDefault()
    {
        Table<string, string, int> table = CreatePopulated();

        Assert.IsFalse(table.TryGetValue("r2", "c2", out int value));
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that <c>TryGetValue</c> with a <see langword="null" /> row or column key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var table = new Table<string, string, int>();

        var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.TryGetValue(null!, "c1", out _);
        });
        Assert.AreEqual("row", ex1.ParamName);

        var ex2 = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = table.TryGetValue("r1", null!, out _);
        });
        Assert.AreEqual("column", ex2.ParamName);
    }
}
