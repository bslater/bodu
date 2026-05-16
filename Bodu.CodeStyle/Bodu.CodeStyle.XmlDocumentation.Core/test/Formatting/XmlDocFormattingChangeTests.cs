// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormattingChangeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
            _ = new XmlDocFormattingChange(XmlDocFormatRangeKind.BlockLayout, null!);
        });

        Assert.AreEqual("description", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        var change = new XmlDocFormattingChange(XmlDocFormatRangeKind.LineWrap, "Wrapped.");

        Assert.AreEqual(XmlDocFormatRangeKind.LineWrap, change.Kind);
        Assert.AreEqual("Wrapped.", change.Description);
    }
}
