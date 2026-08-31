// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookMailStore" />, the mail-store session, with shared suppliers for the
/// member partials.
/// </summary>
[TestClass]
public partial class OutlookMailStoreTests
{
    /// <summary>The primary Unicode reference fixture.</summary>
    internal const string Sample1 = "unicode/sample1.pst";

    /// <summary>
    /// Opens the primary Unicode reference fixture as a mail store.
    /// </summary>
    /// <param name="validationLevel">The validation level to open with.</param>
    /// <returns>The open session.</returns>
    internal static OutlookMailStore OpenSample1(PstValidationLevel validationLevel = PstValidationLevel.Compatible) =>
        OutlookMailStore.Open(
            PstReferenceFixtures.OpenStream(Sample1),
            new OutlookMailStoreReaderOptions { ValidationLevel = validationLevel });

    /// <summary>
    /// Walks a folder subtree depth-first, yielding every folder including the root of the walk.
    /// </summary>
    /// <param name="folder">The folder to walk from.</param>
    /// <returns>The folders, parents before children.</returns>
    internal static IEnumerable<OutlookMailFolder> Walk(OutlookMailFolder folder)
    {
        yield return folder;

        foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
        {
            foreach (OutlookMailFolder descendant in Walk(child))
                yield return descendant;
        }
    }
}
