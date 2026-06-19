// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.LoadJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ParserValidationTests
{
    /// <summary>
    /// Verifies that loading <see langword="null" /> JSON throws <see cref="ArgumentNullException" /> naming the
    /// <c>json</c> parameter.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenJsonIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson((string)null!);
        });

        Assert.AreEqual("json", ex.ParamName);
    }

    /// <summary>
    /// Verifies that loading empty or white-space JSON throws <see cref="ArgumentException" /> naming the <c>json</c>
    /// parameter.
    /// </summary>
    /// <param name="json">The empty or white-space content.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\r\n\t")]
    public void LoadJson_WhenJsonIsWhiteSpace_ShouldThrowArgumentException(string json)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.AreEqual("json", ex.ParamName);
    }

    /// <summary>
    /// Verifies that JSON that is not well-formed throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenJsonIsNotWellFormed_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson("{ not valid json ");
        });
    }

    /// <summary>
    /// Verifies that the JSON path reports an impossible fixed date with the <c>BODU-CAL-DAY</c> diagnostic, matching
    /// the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenFixedDateIsImpossible_ShouldThrowDayDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 2, "day": 30 } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DAY", ex.Diagnostics, "BODU-CAL-DAY");
    }

    /// <summary>
    /// Verifies that the JSON path reports an unresolved offset reference with the <c>BODU-CAL-OFFSET-MISSING</c>
    /// diagnostic, matching the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenOffsetReferenceIsUnresolved_ShouldThrowOffsetMissingDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "offsetFromRule": { "notableDateRef": "does-not-exist", "offsetDays": 1 } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-OFFSET-MISSING", ex.Diagnostics, "BODU-CAL-OFFSET-MISSING");
    }

    /// <summary>
    /// Verifies that the JSON path reports an unknown algorithm key with the <c>BODU-CAL-ALGORITHM</c> diagnostic,
    /// matching the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenAlgorithmKeyIsUnknown_ShouldThrowAlgorithmDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "algorithm": { "key": "not-a-real-algorithm" } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-ALGORITHM", ex.Diagnostics, "BODU-CAL-ALGORITHM");
    }

    /// <summary>
    /// Verifies that the JSON path reports an inverted applicability year range with the <c>BODU-CAL-YEARS</c>
    /// diagnostic, matching the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenFromYearAfterToYear_ShouldThrowYearsDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "applicability": { "fromYear": 2030, "toYear": 2000 }, "strategy": { "fixed": { "month": 1, "day": 1 } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-YEARS", ex.Diagnostics, "BODU-CAL-YEARS");
    }
}
