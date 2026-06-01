// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.WordCasingOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="WordCasingOptions.Default" /> exposes the documented default property values.
    /// </summary>
    [TestMethod]
    public void WordCasingOptions_Default_ShouldExposeDocumentedDefaults()
    {
        WordCasingOptions options = WordCasingOptions.Default;

        Assert.AreSame(CultureInfo.InvariantCulture, options.Culture);
        Assert.IsTrue(options.PreserveAcronyms);
        Assert.IsTrue(options.PreserveMixedCaseWords);
        Assert.IsFalse(options.LowerCaseMinorWords);
        Assert.IsTrue(options.Acronyms.Count > 0);
        Assert.IsTrue(options.MinorWords.Count > 0);
    }

    /// <summary>
    /// Verifies that <see cref="WordCasingOptions.Default" /> returns the same shared, reusable instance on
    /// every access.
    /// </summary>
    [TestMethod]
    public void WordCasingOptions_Default_ShouldReturnSharedInstance() => Assert.AreSame(WordCasingOptions.Default, WordCasingOptions.Default);

    /// <summary>
    /// Verifies that an object initialiser on <see cref="WordCasingOptions" /> overrides the targeted
    /// properties while leaving the remaining defaults intact.
    /// </summary>
    [TestMethod]
    public void WordCasingOptions_WhenInitialised_ShouldOverrideTargetedProperties()
    {
        WordCasingOptions options = new()
        {
            Culture = CultureInfo.GetCultureInfo("tr-TR"),
            PreserveAcronyms = false,
            LowerCaseMinorWords = true,
        };

        Assert.AreEqual("tr-TR", options.Culture.Name);
        Assert.IsFalse(options.PreserveAcronyms);
        Assert.IsTrue(options.LowerCaseMinorWords);
        Assert.IsTrue(options.PreserveMixedCaseWords);
    }

    /// <summary>
    /// Verifies that <see cref="SlugOptions.Default" /> exposes the documented default property values.
    /// </summary>
    [TestMethod]
    public void SlugOptions_Default_ShouldExposeDocumentedDefaults()
    {
        SlugOptions options = SlugOptions.Default;

        Assert.AreEqual('-', options.Separator);
        Assert.IsTrue(options.Lowercase);
        Assert.IsTrue(options.NormalizeDiacritics);
        Assert.AreEqual(0, options.MaxLength);
    }

    /// <summary>
    /// Verifies that <see cref="SlugOptions.Default" /> returns the same shared, reusable instance on every
    /// access.
    /// </summary>
    [TestMethod]
    public void SlugOptions_Default_ShouldReturnSharedInstance() => Assert.AreSame(SlugOptions.Default, SlugOptions.Default);
}
