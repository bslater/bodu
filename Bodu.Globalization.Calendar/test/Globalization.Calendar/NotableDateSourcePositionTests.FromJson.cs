// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSourcePositionTests.FromJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSourcePositionTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromJson" /> skips a leading byte order mark, which the JSON
    /// reader never sees, so that the first character after it is position 1.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenTheTextStartsWithAByteOrderMark_ShouldNotCountIt()
    {
        const string json = "\uFEFF{ oops }";

        Assert.AreEqual(new NotableDateSourcePosition(1, 3), NotableDateSourcePosition.FromJson(json, ParseFailure(json)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromJson" /> counts a character outside the Basic
    /// Multilingual Plane as the two UTF-16 characters of its surrogate pair, though it takes four UTF-8 bytes.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenALineHoldsASurrogatePair_ShouldCountItsTwoCharacters()
    {
        const string json = "{ \"a\": \"\U0001F600\", oops }";

        Assert.AreEqual(new NotableDateSourcePosition(1, 14), NotableDateSourcePosition.FromJson(json, ParseFailure(json)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromJson" /> counts a character the UTF-8 encoding writes in
    /// three bytes once.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenALineHoldsAThreeByteCharacter_ShouldCountItOnce()
    {
        const string json = "{ \"€\": 1, oops }";

        Assert.AreEqual(new NotableDateSourcePosition(1, 11), NotableDateSourcePosition.FromJson(json, ParseFailure(json)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromJson" /> ends a line at its line feed, so a carriage
    /// return before it leaves the next line's positions unchanged.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenLinesEndInCarriageReturnLineFeed_ShouldStartTheLineAfterTheLineFeed()
    {
        const string json = "{\r\n  oops\r\n}";

        Assert.AreEqual(new NotableDateSourcePosition(2, 3), NotableDateSourcePosition.FromJson(json, ParseFailure(json)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateSourcePosition.FromJson" /> returns <see langword="null" /> for a failure
    /// that reports no line.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenTheFailureReportsNoLine_ShouldReturnNull() =>
        Assert.IsNull(NotableDateSourcePosition.FromJson("{}", new JsonException("no position")));
}
