// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteFixTests.IsRunnable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteFixTests
{
    /// <summary>
    /// Verifies that an <c>applies</c> or <c>dialect</c> row of a data kind runs as data.
    /// </summary>
    /// <param name="rowClass">The row's class.</param>
    /// <param name="kind">The row's kind.</param>
    [TestMethod]
    [DataRow("applies", "parse")]
    [DataRow("applies", "reject")]
    [DataRow("applies", "write")]
    [DataRow("applies", "write-reject")]
    [DataRow("applies", "roundtrip")]
    [DataRow("dialect", "parse")]
    [DataRow("dialect", "reject")]
    public void IsRunnable_WhenRowIsAppliesOrDialectOfADataKind_ShouldReturnTrue(string rowClass, string kind)
    {
        ReleaseNoteFix row = Row(rowClass: rowClass, kind: kind);

        Assert.IsTrue(row.IsRunnable);
    }

    /// <summary>
    /// Verifies that a <c>unit</c> row, and an <c>n/a</c> or <c>unknown</c> row, does not run as data.
    /// </summary>
    /// <param name="rowClass">The row's class.</param>
    /// <param name="kind">The row's kind.</param>
    [TestMethod]
    [DataRow("applies", "unit")]
    [DataRow("dialect", "unit")]
    [DataRow("n/a", "")]
    [DataRow("unknown", "")]
    public void IsRunnable_WhenRowIsUnitOrNotApplicable_ShouldReturnFalse(string rowClass, string kind)
    {
        ReleaseNoteFix row = Row(rowClass: rowClass, kind: kind);

        Assert.IsFalse(row.IsRunnable);
    }
}
