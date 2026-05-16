// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.ArgumentValidation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that passing a <see langword="null" /> trivia text throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FormatTrivia_WhenTriviaTextIsNull_ShouldThrowArgumentNullException()
    {
        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();
        XmlDocFormatOptions options = CreateOptions();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = formatter.FormatTrivia(null!, context, options);
        });

        Assert.AreEqual("triviaText", ex.ParamName);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> context throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FormatTrivia_WhenContextIsNull_ShouldThrowArgumentNullException()
    {
        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatOptions options = CreateOptions();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = formatter.FormatTrivia("/// <summary>x</summary>\r\n", null!, options);
        });

        Assert.AreEqual("context", ex.ParamName);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> options instance throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FormatTrivia_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        XmlDocFormatter formatter = CreateFormatter();
        XmlDocFormatContext context = CreateContext();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = formatter.FormatTrivia("/// <summary>x</summary>\r\n", context, null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
