// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

[TestClass]
public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Verifies that sorting an array of numeric-suffixed names with <see cref="NaturalStringComparer.Ordinal" />
    /// yields the natural (numeric-aware) order rather than the plain ordinal order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Compare_WhenSortingNumericSuffixedNames_ShouldOrderNaturally()
    {
        var names = new[] { "file10", "file2", "file1" };

        Array.Sort(names, NaturalStringComparer.Ordinal);

        CollectionAssert.AreEqual(new[] { "file1", "file2", "file10" }, names);
    }
}
