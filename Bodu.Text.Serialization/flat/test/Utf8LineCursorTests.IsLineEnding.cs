// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8LineCursorTests.IsLineEnding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class Utf8LineCursorTests
{
    /// <summary>
    /// Verifies that only a CR and a LF are line endings, of every byte value.
    /// </summary>
    [TestMethod]
    public void IsLineEnding_WhenEveryByteIsTested_ShouldAcceptOnlyCrAndLf()
    {
        for (int value = 0; value <= byte.MaxValue; value++)
            Assert.AreEqual(value is '\r' or '\n', Utf8LineCursor.IsLineEnding((byte)value), $"0x{value:X2}");
    }
}
