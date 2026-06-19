// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the argument-, schema-, and semantic-validation contracts of <see cref="NotableDateResourceLoader" /> for
/// both the XML and JSON ingestion paths. Ported from the v1 <c>NotableDateRuleParserTests.Exceptions</c> /
/// <c>NotableDateRuleJsonParserTests.Exceptions</c> / <c>MonthValidation</c> rows, mapped onto the v2 failure model: an
/// <see cref="ArgumentNullException" /> for null content, an <see cref="ArgumentException" /> for white-space content, a
/// <see cref="FormatException" /> for malformed input, and a <see cref="NotableDateValidationException" /> whose
/// <see cref="NotableDateValidationException.Diagnostics" /> carry a stable code for schema and semantic faults.
/// </summary>
[TestClass]
public sealed partial class ParserValidationTests
{
    /// <summary>
    /// Asserts that loading the supplied invalid fixture throws a <see cref="NotableDateValidationException" /> carrying
    /// the expected error diagnostic.
    /// </summary>
    /// <param name="fileName">The invalid fixture file name.</param>
    /// <param name="expectedCode">The diagnostic code expected among the failure diagnostics.</param>
    private static void AssertFixtureFailsWith(string fileName, string expectedCode)
    {
        string xml = NotableDateFixtures.ReadText(fileName);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(
            d => d.Code == expectedCode && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            $"Expected diagnostic '{expectedCode}'. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }
}
