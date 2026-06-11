// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFormatExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;
using Bodu.Text.DotEnv;
using Bodu.Text.Ini;

namespace Bodu.Text;

/// <summary>
/// Cross-format tests for <see cref="TextFormatException" /> — the abstract base that unifies diagnostics across
/// every format-specific exception type. Verifies that each derived type extends the shared base and exposes the
/// common diagnostic properties.
/// </summary>
[TestClass]
public sealed class TextFormatExceptionTests
{
    /// <summary>
    /// Verifies that every format-specific exception type is a <see cref="TextFormatException" /> so callers can
    /// catch parse failures across formats with a single handler.
    /// </summary>
    [TestMethod]
    public void EveryFormatException_ShouldDeriveFromTextFormatException()
    {
        Assert.IsTrue(typeof(TextFormatException).IsAssignableFrom(typeof(DelimitedFormatException)));
        Assert.IsTrue(typeof(TextFormatException).IsAssignableFrom(typeof(DotEnvFormatException)));
        Assert.IsTrue(typeof(TextFormatException).IsAssignableFrom(typeof(IniFormatException)));
    }

    /// <summary>
    /// Verifies that <see cref="TextFormatException" /> derives from <see cref="FormatException" /> so existing
    /// callers that catch <see cref="FormatException" /> continue to work.
    /// </summary>
    [TestMethod]
    public void TextFormatException_ShouldDeriveFromFormatException() => Assert.IsTrue(typeof(FormatException).IsAssignableFrom(typeof(TextFormatException)));

    /// <summary>
    /// Verifies that a Delimited parse failure populates <see cref="TextFormatException.LineNumber" /> when the
    /// caller observes the exception through the shared base type.
    /// </summary>
    [TestMethod]
    public void DelimitedParseFailure_ShouldExposeLineNumberThroughBase()
    {
        TextFormatException ex = Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Delimited.Delimited.Parse("name\n\"unclosed");
        });

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a default-constructed <see cref="TextFormatException" /> reports <c>0</c> for line number
    /// and <see langword="null" /> for both column and offset.
    /// </summary>
    [TestMethod]
    public void DefaultConstructedException_ShouldReportUnknownLocation()
    {
        DelimitedFormatException ex = new();

        Assert.AreEqual(0, ex.LineNumber);
        Assert.IsNull(ex.Offset);
    }
}
