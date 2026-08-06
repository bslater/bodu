// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentSummaryInformationTests.Authored.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies every <see cref="DocumentSummaryInformation" /> accessor against a hand-authored property set, rather
/// than only the fields the reference documents happen to carry.
/// </summary>
/// <remarks>
/// <see cref="DocumentSummaryInformationBuilder" /> writes only a subset of the mapped identifiers, so
/// <c>PresentationTarget</c>, <c>Bytes</c>, <c>NoteCount</c>, <c>HiddenCount</c>, <c>MultimediaClipCount</c>,
/// <c>ScaleCrop</c> and <c>LinksUpToDate</c> are reachable only through the section-level
/// <see cref="OlePropertySection.Set(int, OlePropertyValue)" />.
/// </remarks>
public partial class DocumentSummaryInformationTests
{
    /// <summary>
    /// Builds a document-summary view over a section carrying every mapped property identifier, round-tripped
    /// through the writer and reader.
    /// </summary>
    /// <returns>The re-parsed document summary information.</returns>
    private static DocumentSummaryInformation AuthorFullSet()
    {
        var section = new OlePropertySection(WellKnownFormatIds.DocumentSummaryInformation, 1252);
        section.Set(2, OlePropertyValue.Create("Category value"));
        section.Set(3, OlePropertyValue.Create("PresentationTarget value"));
        section.Set(4, OlePropertyValue.Create(4096));
        section.Set(5, OlePropertyValue.Create(12));
        section.Set(6, OlePropertyValue.Create(34));
        section.Set(7, OlePropertyValue.Create(56));
        section.Set(8, OlePropertyValue.Create(78));
        section.Set(9, OlePropertyValue.Create(90));
        section.Set(10, OlePropertyValue.Create(13));
        section.Set(11, OlePropertyValue.Create(true));
        section.Set(14, OlePropertyValue.Create("Manager value"));
        section.Set(15, OlePropertyValue.Create("Company value"));
        section.Set(16, OlePropertyValue.Create(false));

        var set = new OlePropertySet(WellKnownFormatIds.DocumentSummaryInformation, Guid.Empty, 1252);
        set.AddSection(section);

        return new DocumentSummaryInformation(OlePropertySet.Parse(set.ToArray()));
    }

    /// <summary>
    /// Verifies that every string-valued accessor round-trips its authored value.
    /// </summary>
    [TestMethod]
    public void StringProperties_WhenAuthored_ShouldRoundTrip()
    {
        DocumentSummaryInformation summary = AuthorFullSet();

        Assert.AreEqual("Category value", summary.Category);
        Assert.AreEqual("PresentationTarget value", summary.PresentationTarget);
        Assert.AreEqual("Manager value", summary.Manager);
        Assert.AreEqual("Company value", summary.Company);
    }

    /// <summary>
    /// Verifies that every count-valued accessor round-trips its authored value, and that each reads a distinct
    /// property identifier rather than aliasing another.
    /// </summary>
    [TestMethod]
    public void CountProperties_WhenAuthored_ShouldRoundTrip()
    {
        DocumentSummaryInformation summary = AuthorFullSet();

        Assert.AreEqual(4096, summary.Bytes);
        Assert.AreEqual(12, summary.LineCount);
        Assert.AreEqual(34, summary.ParagraphCount);
        Assert.AreEqual(56, summary.SlideCount);
        Assert.AreEqual(78, summary.NoteCount);
        Assert.AreEqual(90, summary.HiddenCount);
        Assert.AreEqual(13, summary.MultimediaClipCount);
    }

    /// <summary>
    /// Verifies that both boolean accessors round-trip, including the <see langword="false" /> case - which must be
    /// distinguishable from the property being absent.
    /// </summary>
    [TestMethod]
    public void BooleanProperties_WhenAuthored_ShouldRoundTripIncludingFalse()
    {
        DocumentSummaryInformation summary = AuthorFullSet();

        Assert.AreEqual(true, summary.ScaleCrop);
        Assert.AreEqual(false, summary.LinksUpToDate);
    }

    /// <summary>
    /// Verifies that an absent property reports <see langword="null" /> rather than a default value.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSectionIsEmpty_ShouldAllReportNull()
    {
        var set = new OlePropertySet(WellKnownFormatIds.DocumentSummaryInformation, Guid.Empty, 1252);
        set.AddSection(new OlePropertySection(WellKnownFormatIds.DocumentSummaryInformation, 1252));

        var summary = new DocumentSummaryInformation(OlePropertySet.Parse(set.ToArray()));

        Assert.IsNull(summary.Category);
        Assert.IsNull(summary.Bytes);
        Assert.IsNull(summary.ScaleCrop);
        Assert.IsNull(summary.LinksUpToDate);
    }

    /// <summary>
    /// Verifies that <see cref="DocumentSummaryInformation.CustomProperties" /> is empty - rather than
    /// <see langword="null" /> or a throw - when the set carries no user-defined section.
    /// </summary>
    [TestMethod]
    public void CustomProperties_WhenNoUserDefinedSection_ShouldBeEmpty()
    {
        DocumentSummaryInformation summary = AuthorFullSet();

        Assert.AreEqual(0, summary.CustomProperties.Count);
    }

    /// <summary>
    /// Verifies that the user-defined section is located by format identifier rather than by position, so a
    /// preceding built-in section does not hide it.
    /// </summary>
    [TestMethod]
    public void CustomProperties_WhenUserDefinedSectionFollowsBuiltIn_ShouldBeFound()
    {
        var builtIn = new OlePropertySection(WellKnownFormatIds.DocumentSummaryInformation, 1252);
        builtIn.Set(2, OlePropertyValue.Create("Category value"));

        var userDefined = new OlePropertySection(WellKnownFormatIds.UserDefinedProperties, 1252);
        userDefined.Set(2, OlePropertyValue.Create("Contoso"));
        userDefined.SetName(2, "Client");
        userDefined.Set(3, OlePropertyValue.Create(7));
        userDefined.SetName(3, "Revision");

        var set = new OlePropertySet(WellKnownFormatIds.DocumentSummaryInformation, Guid.Empty, 1252);
        set.AddSection(builtIn);
        set.AddSection(userDefined);

        var summary = new DocumentSummaryInformation(OlePropertySet.Parse(set.ToArray()));

        Assert.AreEqual("Category value", summary.Category);
        Assert.AreEqual(2, summary.CustomProperties.Count);
        Assert.AreEqual("Contoso", summary.CustomProperties["Client"].AsString());
        Assert.AreEqual(7, summary.CustomProperties["Revision"].AsInt32());
    }

    /// <summary>
    /// Verifies that <see cref="DocumentSummaryInformation.PropertySet" /> exposes the set the view was constructed
    /// over.
    /// </summary>
    [TestMethod]
    public void PropertySet_ShouldExposeUnderlyingSet()
    {
        var authored = new OlePropertySet(WellKnownFormatIds.DocumentSummaryInformation, Guid.Empty, 1252);
        authored.AddSection(new OlePropertySection(WellKnownFormatIds.DocumentSummaryInformation, 1252));

        OlePropertySet set = OlePropertySet.Parse(authored.ToArray());
        var summary = new DocumentSummaryInformation(set);

        Assert.AreSame(set, summary.PropertySet);
    }
}
