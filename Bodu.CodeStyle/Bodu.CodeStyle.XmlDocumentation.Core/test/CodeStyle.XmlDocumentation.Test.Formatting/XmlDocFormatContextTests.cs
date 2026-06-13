// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatContextTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocFormatContextTests
{
    /// <summary>
    /// Verifies that the constructor throws when the base indent is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBaseIndentIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocFormatContext(null!, "\r\n", XmlDocMemberKindHint.Unknown);
        });

        Assert.AreEqual("baseIndent", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the line ending is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLineEndingIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocFormatContext("    ", null!, XmlDocMemberKindHint.Unknown);
        });

        Assert.AreEqual("lineEnding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the line ending is empty.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLineEndingIsEmpty_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XmlDocFormatContext("    ", string.Empty, XmlDocMemberKindHint.Unknown);
        });

        Assert.AreEqual("lineEnding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        var context = new XmlDocFormatContext("\t\t", "\n", XmlDocMemberKindHint.Property);

        Assert.AreEqual("\t\t", context.BaseIndent);
        Assert.AreEqual("\n", context.LineEnding);
        Assert.AreEqual(XmlDocMemberKindHint.Property, context.MemberKindHint);
    }
}
