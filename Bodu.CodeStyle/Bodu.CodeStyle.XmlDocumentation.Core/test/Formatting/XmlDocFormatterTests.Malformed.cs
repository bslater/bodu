// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Malformed.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that an opening tag with no matching close is left unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenOpeningTagHasNoClose_ShouldLeaveUnchanged()
    {
        var input = "/// <summary>Foo bar.\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a closing tag with no matching open is left unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenClosingTagHasNoOpen_ShouldLeaveUnchanged()
    {
        var input = "/// Foo bar.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that mismatched opening and closing tag names result in the input being left unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenTagNamesDoNotMatch_ShouldLeaveUnchanged()
    {
        var input = "/// <summary>Foo.</remarks>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that nested tags closed in the wrong order leave the input untouched.
    /// </summary>
    [TestMethod]
    public void Format_WhenNestedTagsClosedOutOfOrder_ShouldLeaveUnchanged()
    {
        var input = "/// <summary><para>x</summary></para>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that an unterminated inline tag (no matching closer) is treated as a block tag and reported when
    /// the rest of the XML is well-formed.
    /// </summary>
    [TestMethod]
    public void Format_WhenInlineTagHasNoClose_ShouldLeaveUnchanged()
    {
        var input = "/// <summary>See <c>foo bar.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that an empty doc comment (only the prefix) is left unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenInputIsOnlyDocPrefix_ShouldLeaveUnchanged()
    {
        var input = "/// \r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        // The original empty/whitespace-only trivia has no XML structure, so no changes are reported.
        Assert.IsFalse(result.Changed);
    }
}
