// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SchemaValidationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the schema holds: well-formed, schema-valid notable-date documents load, while malformed, schema-invalid, and
/// semantically invalid notable-date documents are rejected with the expected diagnostic.
/// </summary>
[TestClass]
public sealed class SchemaValidationTests
{
    /// <summary>
    /// Verifies that the strategies fixture loads and exposes the expected concept and policy counts.
    /// </summary>
    [TestMethod]
    public void Load_WhenStrategiesFixtureIsValid_Succeeds()
    {
        NotableDateResource resource = NotableDateFixtures.Load("strategies.xml");

        Assert.AreEqual(12, resource.NotableDates.Count, "notable-date count");
        Assert.AreEqual(0, resource.AdjustmentPolicies.Count, "adjustment-policy count");
    }

    /// <summary>
    /// Verifies that the adjustments fixture loads and exposes the expected concept and policy counts.
    /// </summary>
    [TestMethod]
    public void Load_WhenAdjustmentsFixtureIsValid_Succeeds()
    {
        NotableDateResource resource = NotableDateFixtures.Load("adjustments.xml");

        Assert.AreEqual(10, resource.NotableDates.Count, "notable-date count");
        Assert.AreEqual(10, resource.AdjustmentPolicies.Count, "adjustment-policy count");
    }

    /// <summary>
    /// Verifies that malformed XML is rejected with a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenXmlIsNotWellFormed_ThrowsFormatException()
    {
        var xml = NotableDateFixtures.ReadText("invalid-not-wellformed.xml");

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });
    }

    /// <summary>
    /// Verifies that empty content is rejected with an <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenContentIsWhiteSpace_ThrowsArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateResourceLoader.Load("   ");
        });
    }

    /// <summary>
    /// Verifies that an invalid notable-date document is rejected with a <see cref="NotableDateValidationException" /> carrying the
    /// expected error diagnostic.
    /// </summary>
    /// <param name="fileName">The invalid fixture file name.</param>
    /// <param name="expectedCode">The diagnostic code expected among the failure diagnostics.</param>
    [TestMethod]
    [DataRow("invalid-bad-territory.xml", "BODU-CAL-SCHEMA")]
    [DataRow("invalid-bad-emission.xml", "BODU-CAL-SCHEMA")]
    [DataRow("invalid-impossible-date.xml", "BODU-CAL-DAY")]
    [DataRow("invalid-duplicate-rule-id.xml", "BODU-CAL-DUP-RULE")]
    [DataRow("invalid-duplicate-notable-id.xml", "BODU-CAL-DUP-ND")]
    [DataRow("invalid-unknown-policy-ref.xml", "BODU-CAL-ADJREF")]
    [DataRow("invalid-offset-missing.xml", "BODU-CAL-OFFSET-MISSING")]
    [DataRow("invalid-unknown-algorithm.xml", "BODU-CAL-ALGORITHM")]
    [DataRow("invalid-fromyear-after-toyear.xml", "BODU-CAL-YEARS")]
    [DataRow("invalid-override-missing-rule.xml", "BODU-CAL-OVERRIDE-RULE")]
    public void Load_WhenDocumentIsInvalid_ThrowsWithExpectedDiagnostic(string fileName, string expectedCode)
    {
        var xml = NotableDateFixtures.ReadText(fileName);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.IsTrue(
            ex.Diagnostics.Any(d => d.Code == expectedCode && d.Severity == NotableDateValidationSeverity.Error),
            $"Expected diagnostic '{expectedCode}'. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }
}
