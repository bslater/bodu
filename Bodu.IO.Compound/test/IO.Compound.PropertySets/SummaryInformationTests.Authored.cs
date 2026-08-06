// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformationTests.Authored.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies every <see cref="SummaryInformation" /> accessor against a hand-authored property set, rather than only
/// the handful of fields the reference documents happen to carry.
/// </summary>
/// <remarks>
/// <para>
/// The reference fixtures populate a different subset each, so several standard fields were never read. Authoring a
/// section that sets every property identifier covers the whole accessor surface and, because the set is written and
/// re-parsed, holds the writer to the same contract.
/// </para>
/// <para>
/// <see cref="SummaryInformationBuilder" /> deliberately exposes no setter for <c>Template</c>, <c>TotalEditTime</c>,
/// <c>LastPrinted</c> or <c>Security</c>, so those four are reachable only through the section-level
/// <see cref="OlePropertySection.Set(int, OlePropertyValue)" />.
/// </para>
/// </remarks>
public partial class SummaryInformationTests
{
    /// <summary>The instant used for every date-valued property.</summary>
    private static readonly DateTimeOffset SampleInstant = new(2021, 5, 6, 7, 8, 9, TimeSpan.Zero);

    /// <summary>
    /// Builds a summary-information view over a section carrying every mapped property identifier, round-tripped
    /// through the writer and reader.
    /// </summary>
    /// <returns>The re-parsed summary information.</returns>
    private static SummaryInformation AuthorFullSet()
    {
        var section = new OlePropertySection(WellKnownFormatIds.SummaryInformation, 1252);
        section.Set(2, OlePropertyValue.Create("Title value"));
        section.Set(3, OlePropertyValue.Create("Subject value"));
        section.Set(4, OlePropertyValue.Create("Author value"));
        section.Set(5, OlePropertyValue.Create("Keywords value"));
        section.Set(6, OlePropertyValue.Create("Comments value"));
        section.Set(7, OlePropertyValue.Create("Template value"));
        section.Set(8, OlePropertyValue.Create("LastAuthor value"));
        section.Set(9, OlePropertyValue.Create("RevisionNumber value"));
        section.Set(10, new OlePropertyValue { Type = OlePropertyType.FileTime, Value = TimeSpan.FromMinutes(90).Ticks });
        section.Set(11, OlePropertyValue.Create(SampleInstant));
        section.Set(12, OlePropertyValue.Create(SampleInstant));
        section.Set(13, OlePropertyValue.Create(SampleInstant));
        section.Set(14, OlePropertyValue.Create(11));
        section.Set(15, OlePropertyValue.Create(222));
        section.Set(16, OlePropertyValue.Create(3333));
        section.Set(18, OlePropertyValue.Create("ApplicationName value"));
        section.Set(19, OlePropertyValue.Create(4));

        var set = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252);
        set.AddSection(section);

        return new SummaryInformation(OlePropertySet.Parse(set.ToArray()));
    }

    /// <summary>
    /// Verifies that every string-valued accessor round-trips its authored value.
    /// </summary>
    [TestMethod]
    public void StringProperties_WhenAuthored_ShouldRoundTrip()
    {
        SummaryInformation summary = AuthorFullSet();

        Assert.AreEqual("Title value", summary.Title);
        Assert.AreEqual("Subject value", summary.Subject);
        Assert.AreEqual("Author value", summary.Author);
        Assert.AreEqual("Keywords value", summary.Keywords);
        Assert.AreEqual("Comments value", summary.Comments);
        Assert.AreEqual("Template value", summary.Template);
        Assert.AreEqual("LastAuthor value", summary.LastAuthor);
        Assert.AreEqual("RevisionNumber value", summary.RevisionNumber);
        Assert.AreEqual("ApplicationName value", summary.ApplicationName);
    }

    /// <summary>
    /// Verifies that every count-valued accessor round-trips its authored value, and that each reads a distinct
    /// property identifier rather than aliasing another.
    /// </summary>
    [TestMethod]
    public void CountProperties_WhenAuthored_ShouldRoundTrip()
    {
        SummaryInformation summary = AuthorFullSet();

        Assert.AreEqual(11, summary.PageCount);
        Assert.AreEqual(222, summary.WordCount);
        Assert.AreEqual(3333, summary.CharacterCount);
        Assert.AreEqual(4, summary.Security);
    }

    /// <summary>
    /// Verifies that every date-valued accessor round-trips its authored instant, and that the edit-time accessor
    /// decodes its raw tick count as a <see cref="TimeSpan" />.
    /// </summary>
    [TestMethod]
    public void DateProperties_WhenAuthored_ShouldRoundTrip()
    {
        SummaryInformation summary = AuthorFullSet();

        Assert.AreEqual(SampleInstant, summary.LastPrinted);
        Assert.AreEqual(SampleInstant, summary.CreateTime);
        Assert.AreEqual(SampleInstant, summary.LastSaveTime);
        Assert.AreEqual(TimeSpan.FromMinutes(90), summary.TotalEditTime);
    }

    /// <summary>
    /// Verifies that a document that was never printed - which stores a FILETIME of zero rather than omitting the
    /// property - reports <see langword="null" /> rather than the 1601 FILETIME epoch.
    /// </summary>
    [TestMethod]
    public void LastPrinted_WhenFileTimeIsZero_ShouldReturnNull()
    {
        var section = new OlePropertySection(WellKnownFormatIds.SummaryInformation, 1252);
        section.Set(11, new OlePropertyValue { Type = OlePropertyType.FileTime, Value = 0L });

        var set = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252);
        set.AddSection(section);

        var summary = new SummaryInformation(OlePropertySet.Parse(set.ToArray()));

        Assert.IsNull(summary.LastPrinted);
    }

    /// <summary>
    /// Verifies that an absent property reports <see langword="null" /> rather than a default value, so a caller can
    /// tell "not recorded" from "recorded as empty".
    /// </summary>
    [TestMethod]
    public void Properties_WhenSectionIsEmpty_ShouldAllReportNull()
    {
        var set = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252);
        set.AddSection(new OlePropertySection(WellKnownFormatIds.SummaryInformation, 1252));

        var summary = new SummaryInformation(OlePropertySet.Parse(set.ToArray()));

        Assert.IsNull(summary.Title);
        Assert.IsNull(summary.Template);
        Assert.IsNull(summary.LastPrinted);
        Assert.IsNull(summary.TotalEditTime);
        Assert.IsNull(summary.PageCount);
        Assert.IsNull(summary.Security);
    }

    /// <summary>
    /// Verifies that <see cref="SummaryInformation.PropertySet" /> exposes the set the view was constructed over.
    /// </summary>
    [TestMethod]
    public void PropertySet_ShouldExposeUnderlyingSet()
    {
        var authored = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252);
        authored.AddSection(new OlePropertySection(WellKnownFormatIds.SummaryInformation, 1252));

        OlePropertySet set = OlePropertySet.Parse(authored.ToArray());
        var summary = new SummaryInformation(set);

        Assert.AreSame(set, summary.PropertySet);
    }

    /// <summary>
    /// Verifies that a property set carrying no sections is rejected as malformed, rather than parsing into a view
    /// whose every accessor silently returns <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSetHasNoSections_ShouldThrowExactly()
    {
        byte[] bytes = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, 1252).ToArray();

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            _ = OlePropertySet.Parse(bytes);
        });
    }
}
