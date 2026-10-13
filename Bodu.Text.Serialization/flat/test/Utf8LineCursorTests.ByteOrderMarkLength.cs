// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.ByteOrderMarkLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that the length of a byte-order mark is three only when the source starts with all three of its bytes.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="hex">The source bytes, in hexadecimal.</param>
    /// <param name="expected">The expected length.</param>
    [TestMethod]
    [DataRow("whole mark before text", "EFBBBF61", 3)]
    [DataRow("whole mark alone", "EFBBBF", 3)]
    [DataRow("first two bytes of the mark", "EFBB61", 0)]
    [DataRow("first byte of the mark", "EF", 0)]
    [DataRow("mark after text", "61EFBBBF", 0)]
    [DataRow("empty source", "", 0)]
    public void ByteOrderMarkLength_WhenTheSourceStartsWithTheBytesGiven_ShouldCountOnlyAWholeMark(string testName, string hex, int expected)
    {
        Assert.AreEqual(expected, Utf8LineCursor.ByteOrderMarkLength(Convert.FromHexString(hex)), testName);
    }
}
