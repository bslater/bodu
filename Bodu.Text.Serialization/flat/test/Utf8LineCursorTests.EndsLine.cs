// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.EndsLine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that a LF and a CR that no LF follows each end a line, so a CR LF pair ends one line, at its LF, and
    /// that no other byte does.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The text holding the byte.</param>
    /// <param name="position">The position of the byte to test.</param>
    /// <param name="expected">Whether the byte ends a line.</param>
    [TestMethod]
    [DataRow("LF", "a\nb", 1, true)]
    [DataRow("lone CR", "a\rb", 1, true)]
    [DataRow("lone CR at the end", "a\r", 1, true)]
    [DataRow("CR of a CR LF", "a\r\nb", 1, false)]
    [DataRow("LF of a CR LF", "a\r\nb", 2, true)]
    [DataRow("CR before a CR", "a\r\rb", 1, true)]
    [DataRow("other byte", "ab", 1, false)]
    public void EndsLine_WhenTheByteIsTested_ShouldCountEachLineOnce(string testName, string source, int position, bool expected)
    {
        Assert.AreEqual(expected, Utf8LineCursor.EndsLine(Encoding.UTF8.GetBytes(source), position), testName);
    }
}
