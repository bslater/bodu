// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OverrideDiagnosticsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the override applier reports a stable diagnostic when an override targets a notable date or rule that
/// does not exist.
/// </summary>
[TestClass]
public class OverrideDiagnosticsTests
{
    private const string OverrideTargetsMissingNotableDate = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.override-nd">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
          <Overrides>
            <PatchRule notableDateRef="missing-notable-date" ruleRef="r" category="PublicHoliday" />
          </Overrides>
        </NotableDateResource>
        """;

    private const string PatchRuleTargetsMissingRule = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.override-rule">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
          <Overrides>
            <PatchRule notableDateRef="x" ruleRef="missing-rule" category="PublicHoliday" />
          </Overrides>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that an override whose notable-date reference does not exist reports the
    /// <c>BODU-CAL-OVERRIDE-ND</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenOverrideTargetsMissingNotableDate_ShouldReportOverrideNotFound()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(OverrideTargetsMissingNotableDate);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-OVERRIDE-ND", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a patch override whose rule reference does not exist reports the <c>BODU-CAL-OVERRIDE-RULE</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenPatchRuleTargetsMissingRule_ShouldReportRuleNotFound()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(PatchRuleTargetsMissingRule);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-OVERRIDE-RULE", ex.Diagnostics);
    }
}
