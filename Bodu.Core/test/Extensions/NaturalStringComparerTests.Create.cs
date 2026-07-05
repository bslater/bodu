// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NaturalStringComparerTests.Create.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class NaturalStringComparerTests
{
    /// <summary>
    /// Verifies that <see cref="NaturalStringComparer.Create(CultureInfo, bool)" /> throws
    /// <see cref="ArgumentNullException" /> when the culture is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenCultureIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NaturalStringComparer.Create(null!, ignoreCase: false);
        });

        Assert.AreEqual("culture", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a comparer created for German honours that culture's collation — <c>"ä"</c> sorts with
    /// <c>"a"</c>, before <c>"b"</c> — where <see cref="NaturalStringComparer.Ordinal" /> orders the same pair by
    /// code point.
    /// </summary>
    [TestMethod]
    public void Create_WhenGermanCultureSupplied_ShouldUseCultureCollationForText()
    {
        var german = NaturalStringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: false);

        Assert.IsTrue(german.Compare("ä1", "b1") < 0);
        Assert.IsTrue(NaturalStringComparer.Ordinal.Compare("ä1", "b1") > 0);
    }

    /// <summary>
    /// Verifies that a comparer created via <see cref="NaturalStringComparer.Create(CultureInfo, bool)" /> captures
    /// the supplied culture: its ordering is unaffected by later changes to
    /// <see cref="CultureInfo.CurrentCulture" />, while <see cref="NaturalStringComparer.CurrentCulture" /> reads the
    /// ambient culture at compare time.
    /// </summary>
    [TestMethod]
    public void Create_WhenCurrentCultureChanges_ShouldKeepCapturedCulture()
    {
        var german = NaturalStringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: false);
        var original = CultureInfo.CurrentCulture;

        try
        {
            // Swedish collates "ä" after "z"; German collates it with "a", before "z".
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");

            Assert.IsTrue(german.Compare("ä1", "z1") < 0, "captured de-DE must be unaffected by the ambient culture");
            Assert.IsTrue(NaturalStringComparer.CurrentCulture.Compare("ä1", "z1") > 0, "CurrentCulture must read the ambient sv-SE culture");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            Assert.IsTrue(NaturalStringComparer.CurrentCulture.Compare("ä1", "z1") < 0, "CurrentCulture must follow the ambient culture change to de-DE");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that a culture-bound comparer created with <c>ignoreCase</c> set to <see langword="true" /> treats
    /// case-folded pairs as equal while still ordering digit runs numerically.
    /// </summary>
    [TestMethod]
    public void Create_WhenIgnoreCaseIsTrue_ShouldFoldCaseAndCompareDigitsNumerically()
    {
        var comparer = NaturalStringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: true);

        Assert.AreEqual(0, comparer.Compare("WERT2", "wert2"));
        Assert.IsTrue(comparer.Compare("wert2", "WERT10") < 0);
    }
}
