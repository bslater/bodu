// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.AtomicTags.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that an inline <c>&lt;see cref="..." /&gt;</c> reference is preserved verbatim and never wrapped.
    /// </summary>
    [TestMethod]
    public void Format_WhenSeeCrefIsInline_ShouldKeepSeeInline()
    {
        var input =
            "/// <summary>\r\n" +
            "    /// See <see cref=\"Sample\" /> for details.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        StringAssert.Contains(result.FormattedText, "<see cref=\"Sample\" />");
    }

    /// <summary>
    /// Verifies that an inline <c>&lt;c&gt;…&lt;/c&gt;</c> token longer than the configured line length is kept
    /// intact rather than being split.
    /// </summary>
    [TestMethod]
    public void Format_WhenInlineCodeExceedsMaxLineLength_ShouldNotSplitCode()
    {
        const string LongCode = "<c>[A-Za-z0-9_.,:/@+\\-]+ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789</c>";

        var input =
            "/// <summary>\r\n" +
            "    /// Use " + LongCode + " in expressions.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, LongCode);
    }

    /// <summary>
    /// Verifies that a self-closing tag with the trailing-space override gets normalised to include the space.
    /// </summary>
    [TestMethod]
    public void Format_WhenSelfClosingTagMissingTrailingSpace_ShouldEmitSpace()
    {
        var input = "/// <summary>See <see cref=\"X\"/> here.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "<see cref=\"X\" />");
    }
}
