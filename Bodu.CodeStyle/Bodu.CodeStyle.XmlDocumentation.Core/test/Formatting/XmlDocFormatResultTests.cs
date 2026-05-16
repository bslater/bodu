// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatResultTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocFormatResultTests
{
    /// <summary>
    /// Verifies that the constructor throws when the formatted text is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFormattedTextIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocFormatResult(changed: false, formattedText: null!, changes: []);
        });

        Assert.AreEqual("formattedText", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the changes array is default (uninitialised).
    /// </summary>
    [TestMethod]
    public void Ctor_WhenChangesArrayIsDefault_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XmlDocFormatResult(changed: false, formattedText: string.Empty, changes: default);
        });

        Assert.AreEqual("changes", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        var changes = ImmutableArray.Create(
            new XmlDocFormattingChange(XmlDocFormatRangeKind.BlockLayout, "Test change."));

        var result = new XmlDocFormatResult(changed: true, formattedText: "/// X\r\n", changes: changes);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual("/// X\r\n", result.FormattedText);
        Assert.AreEqual(1, result.Changes.Length);
        Assert.AreEqual("Test change.", result.Changes[0].Description);
    }
}
