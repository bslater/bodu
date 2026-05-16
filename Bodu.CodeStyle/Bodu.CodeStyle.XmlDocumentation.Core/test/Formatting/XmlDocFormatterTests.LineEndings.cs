// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.LineEndings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a CRLF input continues to emit CRLF when the context specifies CRLF.
    /// </summary>
    [TestMethod]
    public void Format_WhenInputAndContextAreCrlf_ShouldEmitCrlf()
    {
        string input = "/// <summary>Foo.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(lineEnding: "\r\n"), CreateOptions());

        StringAssert.Contains(result.FormattedText, "\r\n");
        Assert.IsFalse(result.FormattedText.Contains("\n\n"));
    }

    /// <summary>
    /// Verifies that an LF input is re-emitted with the context-supplied line ending of CRLF.
    /// </summary>
    [TestMethod]
    public void Format_WhenInputUsesLfButContextIsCrlf_ShouldEmitCrlf()
    {
        string input = "/// <summary>Foo.</summary>\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(lineEnding: "\r\n"), CreateOptions());

        Assert.IsTrue(result.Changed);
        StringAssert.Contains(result.FormattedText, "\r\n");
    }

    /// <summary>
    /// Verifies that an LF-context request emits LF line endings.
    /// </summary>
    [TestMethod]
    public void Format_WhenContextRequestsLf_ShouldEmitLfOnly()
    {
        string input = "/// <summary>Foo.</summary>\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(lineEnding: "\n"), CreateOptions());

        Assert.IsFalse(result.FormattedText.Contains('\r'));
    }

    /// <summary>
    /// Verifies that an input mixing CR-only and LF line endings is normalised to the context line ending.
    /// </summary>
    [TestMethod]
    public void Format_WhenInputMixesLineEndings_ShouldNormaliseToContext()
    {
        string input =
            "/// <summary>\r" +
            "    /// Foo.\n" +
            "    /// </summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(lineEnding: "\r\n"), CreateOptions());

        Assert.IsFalse(result.FormattedText.Contains("\r\r"));
        Assert.IsFalse(result.FormattedText.Contains("\n\n"));
    }
}
