// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.Unicode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that prose containing non-ASCII characters round-trips unchanged.
    /// </summary>
    [TestMethod]
    public void Format_WhenProseContainsUnicode_ShouldPreserveCharacters()
    {
        string input =
            "/// <summary>\r\n" +
            "    /// Café — résumé ☃☃☃.\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, "Café");
        StringAssert.Contains(result.FormattedText, "résumé");
        StringAssert.Contains(result.FormattedText, "☃");
    }

    /// <summary>
    /// Verifies that surrogate-pair emoji are preserved as a unit.
    /// </summary>
    [TestMethod]
    public void Format_WhenProseContainsEmoji_ShouldPreserveSurrogatePair()
    {
        string emoji = char.ConvertFromUtf32(0x1F600);
        string input =
            "/// <summary>\r\n" +
            "    /// Hello " + emoji + ".\r\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        StringAssert.Contains(result.FormattedText, emoji);
    }
}
