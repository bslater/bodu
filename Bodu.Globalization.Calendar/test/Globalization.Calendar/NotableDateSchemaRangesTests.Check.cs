// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaRangesTests.Check.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaRangesTests
{
    /// <summary>
    /// Verifies that a value outside a range returns a <c>BODU-CAL-SCHEMA</c> error naming the field, the value and the
    /// bound it passes.
    /// </summary>
    /// <param name="range">The name of the range.</param>
    /// <param name="value">The value.</param>
    /// <param name="expected">The expected message.</param>
    [TestMethod]
    [DataRow("PositiveInteger", 0, "The 'field' value '0' is less than 1, the minimum the schema allows.")]
    [DataRow("UnsignedShort", -1, "The 'field' value '-1' is less than 0, the minimum the schema allows.")]
    [DataRow("UnsignedShort", 65536, "The 'field' value '65536' is greater than 65535, the maximum the schema allows.")]
    [DataRow("NonZeroOrdinal", 367, "The 'field' value '367' is greater than 366, the maximum the schema allows.")]
    [DataRow("IsoWeekValue", 54, "The 'field' value '54' is greater than 53, the maximum the schema allows.")]
    public void Check_WhenValueIsOutsideRange_ShouldReturnSchemaDiagnostic(string range, int value, string expected)
    {
        NotableDateValidationDiagnostic? diagnostic = NotableDateSchemaRanges.Check(value, Enum.Parse<NotableDateSchemaRange>(range), "field");

        Assert.IsNotNull(diagnostic);
        Assert.AreEqual("BODU-CAL-SCHEMA", diagnostic.Code);
        Assert.AreEqual(NotableDateValidationSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(expected, diagnostic.Message);
    }

    /// <summary>
    /// Verifies that a value inside a range, its bounds included, returns no diagnostic; the ordinal range's zero, which
    /// its pattern rejects, among them.
    /// </summary>
    /// <param name="range">The name of the range.</param>
    /// <param name="value">The value.</param>
    [TestMethod]
    [DataRow("PositiveInteger", 1)]
    [DataRow("PositiveInteger", int.MaxValue)]
    [DataRow("UnsignedShort", 0)]
    [DataRow("UnsignedShort", 65535)]
    [DataRow("DayValue", 31)]
    [DataRow("NonZeroOrdinal", 0)]
    [DataRow("NonZeroOrdinal", -366)]
    [DataRow("IsoWeekValue", 1)]
    public void Check_WhenValueIsInsideRange_ShouldReturnNull(string range, int value)
    {
        Assert.IsNull(NotableDateSchemaRanges.Check(value, Enum.Parse<NotableDateSchemaRange>(range), "field"));
    }
}
