// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GenericConstraintLayoutKatTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Bodu.CodeStyle.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.CodeFixes.Test.Kats;

/// <summary>
/// Documents the generic-constraint-layout KAT gap. <c>BODULAYOUT001</c> (the analyzer that breaks single-line
/// <c>where</c> clauses onto their own lines) is not yet implemented; the KATs from
/// <see cref="BoduCodeStyleKats" /> are surfaced here so the gap is visible in the test runner and so the
/// data-driven entries are version-tracked alongside the other KAT suites.
/// </summary>
[TestClass]
public sealed class GenericConstraintLayoutKatTests
{
    /// <summary>
    /// Returns the known-answer fixtures for the (not-yet-implemented) generic-constraint-layout analyzer.
    /// </summary>
    public static IEnumerable<object[]> Kats() => BoduCodeStyleKats.Data(CodeStyleKatArea.GenericConstraintLayout);

    /// <summary>
    /// Reports each generic-constraint-layout KAT as inconclusive until the BODULAYOUT001 analyzer is shipped.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Kats), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetDisplayName))]
    public void GenericConstraintLayout_ShouldBeInconclusiveUntilAnalyzerExists(CodeStyleKat kat)
    {
        if (kat is null) throw new ArgumentNullException(nameof(kat));

        Assert.Inconclusive($"KAT {kat.Id}: BODULAYOUT001 (generic-constraint-layout analyzer) is not yet implemented.");
    }

    /// <summary>
    /// Returns the MSTest display name for the supplied KAT row.
    /// </summary>
    public static string GetDisplayName(MethodInfo methodInfo, object?[] data) =>
        BoduCodeStyleKats.GetDisplayName(methodInfo, data!);
}
