// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Parsing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that parsing malformed XML throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenXmlMalformed_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() => NotableDateDocumentBuilder.FromXml("<not-closed"));
    }

    /// <summary>
    /// Verifies that parsing XML whose root is not the notable-date resource element throws
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenRootElementUnexpected_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() => NotableDateDocumentBuilder.FromXml("<Wrong />"));
    }

    /// <summary>
    /// Verifies that parsing a document that omits the <c>NotableDates</c> element succeeds, retaining the resource
    /// identifier.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenNotableDatesElementAbsent_ShouldRetainResourceId()
    {
        const string xml = """<NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo.empty" />""";

        Assert.Contains("demo.empty", NotableDateDocumentBuilder.FromXml(xml).ToXml());
    }

    /// <summary>
    /// Verifies that a numeric trigger month is parsed and normalized to its English month name on re-serialization.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenTriggerMonthIsNumeric_ShouldNormalizeToMonthName()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo.month">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p">
                  <Trigger type="IfBeforeFixedDate" month="6" day="15" />
                  <Action type="None" />
                  <Emission mode="ObservedOnly" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="nd" displayName="ND" category="Observance">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        Assert.Contains("month=\"June\"", NotableDateDocumentBuilder.FromXml(xml).ToXml());
    }

    /// <summary>
    /// Verifies that a trigger month that is neither a recognized name nor a valid number throws
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenTriggerMonthInvalid_ShouldThrowFormatException()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo.month">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p">
                  <Trigger type="IfBeforeFixedDate" month="Xyz" day="15" />
                  <Action type="None" />
                  <Emission mode="ObservedOnly" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
            </NotableDateResource>
            """;

        _ = Assert.ThrowsExactly<FormatException>(() => NotableDateDocumentBuilder.FromXml(xml));
    }

    /// <summary>
    /// Verifies that unrecognized child elements under <c>Scope</c> and <c>Overrides</c> are ignored, allowing a
    /// document authored against a newer schema to parse on an older reader.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenUnknownChildElementsPresent_ShouldIgnoreThem()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo.unknown">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p">
                  <Scope><Mystery /></Scope>
                  <Trigger type="IfWeekend" />
                  <Action type="MoveToNextWorkingDay" />
                  <Emission mode="ObservedOnly" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="nd" displayName="ND" category="Observance">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
              <Overrides><Mystery /></Overrides>
            </NotableDateResource>
            """;

        Assert.Contains("demo.unknown", NotableDateDocumentBuilder.FromXml(xml).ToXml());
    }
}
