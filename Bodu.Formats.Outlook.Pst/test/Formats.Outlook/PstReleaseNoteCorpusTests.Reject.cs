// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued file a fix established as structurally malformed, read on the Nodes surface, is
    /// rejected with <see cref="PstFileFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(MalformedContainerRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsMalformedContainer_ShouldThrowPstFileFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        Assert.ThrowsExactly<PstFileFormatException>(
            () =>
            {
                _ = RenderRow(fix);
            },
            fix.ToString());
    }

    /// <summary>
    /// Verifies that each catalogued file a fix established as a variant Bodu does not read, read on the Nodes surface,
    /// is rejected with <see cref="PstUnsupportedFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(UnsupportedVariantRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsUnsupportedVariant_ShouldThrowPstUnsupportedFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        Assert.ThrowsExactly<PstUnsupportedFormatException>(
            () =>
            {
                _ = RenderRow(fix);
            },
            fix.ToString());
    }

    /// <summary>
    /// Verifies that each catalogued file a fix established as a malformed mail store over a sound container is
    /// rejected by the store walk with <see cref="OutlookPstFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(MalformedMailStoreRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsMalformedMailStore_ShouldThrowOutlookPstFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        Assert.ThrowsExactly<OutlookPstFormatException>(
            () =>
            {
                _ = RenderRow(fix);
            },
            fix.ToString());
    }

    /// <summary>
    /// Verifies that each catalogued file a fix established as a container fault, read on the Store surface, is
    /// rejected with <see cref="OutlookPstFormatException" />, the mail store's own exception, whose inner exception is
    /// the container exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(StoreSurfaceContainerRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsContainerFaultOnStoreSurface_ShouldThrowOutlookPstFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        OutlookPstFormatException ex = Assert.ThrowsExactly<OutlookPstFormatException>(
            () =>
            {
                _ = RenderRow(fix);
            },
            fix.ToString());

        Assert.IsNotNull(ex.InnerException, fix.ToString());
        Assert.AreEqual(fix.Expected, ex.InnerException.GetType().Name, fix.ToString());
    }
}
