// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.MergeComments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public partial class IniTests
{
    /// <summary>
    /// Verifies that, when a duplicate section header is merged into its first occurrence, comments preceding the
    /// duplicate header are appended to the merged section's leading comments.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionPrecededByComment_ShouldMergeCommentsIntoFirst()
    {
        IniDocument doc = Ini.Parse("[s]\na=1\n; pending\n[s]\nb=2");

        IniSection section = doc.GetSection("s")!;

        Assert.HasCount(2, section.Entries);
        Assert.HasCount(1, section.LeadingComments);
    }
}
