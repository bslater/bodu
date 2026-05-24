// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormattingChangeTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocFormattingChangeTests
{
    /// <summary>
    /// Verifies that the constructor throws when the description is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDescriptionIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocFormattingChange(tagName: null, XmlDocFormatRangeKind.BlockLayout, description: null!);
        });

        Assert.AreEqual("description", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters when the change
    /// is cross-cutting (no tag attribution).
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTagNameIsNull_ShouldRoundTripCrossCuttingChange()
    {
        var change = new XmlDocFormattingChange(tagName: null, XmlDocFormatRangeKind.LineWrap, "Wrapped.");

        Assert.IsNull(change.TagName);
        Assert.AreEqual(XmlDocFormatRangeKind.LineWrap, change.Kind);
        Assert.AreEqual("Wrapped.", change.Description);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters when the change
    /// is tag-attributed.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTagNameProvided_ShouldRoundTripTagAttributedChange()
    {
        var change = new XmlDocFormattingChange("summary", XmlDocFormatRangeKind.BlockLayout, "Summary updated.");

        Assert.AreEqual("summary", change.TagName);
        Assert.AreEqual(XmlDocFormatRangeKind.BlockLayout, change.Kind);
        Assert.AreEqual("Summary updated.", change.Description);
    }
}
