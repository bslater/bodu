// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.Exceptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the argument-validation and schema-validation exception contracts of
/// <see cref="NotableDateRuleParser" /> across both the string and <see cref="XDocument" />
/// overloads of <see cref="NotableDateRuleParser.ParseXml(string)" /> and
/// <see cref="NotableDateRuleParser.ParseDocument(string)" />.
/// </summary>
public partial class NotableDateRuleParserTests
{
    // -----------------------------------------------------------------------------------------
    // ParseXml(string)
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseXml(string)" /> throws
    /// <see cref="ArgumentNullException" /> for null, empty, or whitespace input and exposes the
    /// <c>xml</c> parameter name on the exception.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\n  ")]
    [TestMethod]
    public void ParseXml_WhenInputIsNullOrWhitespace_ShouldThrowExactly(string? xml)
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml!);
        });

        Assert.AreEqual("xml", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseXml(XDocument)" /> rejects a
    /// <see langword="null" /> document with <see cref="ArgumentNullException" /> and exposes the
    /// <c>document</c> parameter name on the exception.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenXDocumentIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml((XDocument)null!);
        });

        Assert.AreEqual("document", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseXml(string)" /> surfaces schema
    /// violations as <see cref="XmlSchemaValidationException" /> rather than a less specific
    /// parser error.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenInputViolatesSchema_ShouldThrowExactly()
    {
        const string invalidXml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Bad"">
					<Rule>
						<UnknownStrategy />
					</Rule>
				</NotableDate>
			</NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(invalidXml);
        });
    }

    // -----------------------------------------------------------------------------------------
    // ParseDocument(string)
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> throws
    /// <see cref="ArgumentNullException" /> for null, empty, or whitespace input and exposes the
    /// <c>xml</c> parameter name on the exception.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\r\n\t")]
    [TestMethod]
    public void ParseDocument_WhenInputIsNullOrWhitespace_ShouldThrowExactly(string? xml)
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateRuleParser.ParseDocument(xml!);
        });

        Assert.AreEqual("xml", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(string)" /> surfaces schema
    /// violations as <see cref="XmlSchemaValidationException" />.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenInputViolatesSchema_ShouldThrowExactly()
    {
        const string invalidXml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate>
					<Rule>
						<Fixed month=""January"" day=""1"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseDocument(invalidXml);
        });
    }

    // -----------------------------------------------------------------------------------------
    // ParseDocument(XDocument)
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateRuleParser.ParseDocument(XDocument)" /> surfaces
    /// schema violations as <see cref="XmlSchemaValidationException" /> when the document was
    /// constructed in memory and bypasses the string-form validation pipeline.
    /// </summary>
    [TestMethod]
    public void ParseDocument_WhenXDocumentViolatesSchema_ShouldThrowExactly()
    {
        const string invalidXml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Bad"">
					<Rule>
						<Fixed month=""February"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        var document = XDocument.Parse(invalidXml);

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseDocument(document);
        });
    }

    // -----------------------------------------------------------------------------------------
    // Per-element validation surfaced as InvalidOperationException
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that authoring two adjustments with the same <c>key</c> on a single rule throws
    /// <see cref="InvalidOperationException" />, matching the per-rule uniqueness invariant the
    /// merge pipeline relies on.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenRuleHasDuplicateAdjustmentKeys_ShouldThrowExactly()
    {
        const string duplicateKeyXml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Duplicate Key"">
					<Rule name=""Duplicate Key Rule"" category=""Holiday"">
						<Fixed month=""January"" day=""1"" />
						<Adjustment key=""roll"" when=""IfWeekend"" action=""MoveToNextWeekday"" />
						<Adjustment key=""roll"" when=""IfWeekend"" action=""MoveToPreviousWeekday"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(duplicateKeyXml);
        });

        Assert.IsTrue(ex.Message.Contains("roll", StringComparison.Ordinal));
    }
}
