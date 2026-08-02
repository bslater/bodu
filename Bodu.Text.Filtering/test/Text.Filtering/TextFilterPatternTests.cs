// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPatternTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Tests for <see cref="TextFilterPattern" />.
/// </summary>
[TestClass]
public partial class TextFilterPatternTests
{
    /// <summary>
    /// Verifies that the diagnostic representation carries the action sign, the kind, and the pattern text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalled_ShouldDescribeActionKindAndPattern()
    {
        Assert.AreEqual("+wildcard:abc*", TextFilterPattern.Include("abc*").ToString());
        Assert.AreEqual("-regex:^tmp", TextFilterPattern.Exclude("^tmp", TextFilterPatternKind.Regex).ToString());
    }
}
