// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.Format.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization;

public sealed partial class InvariantScalarCodecTests
{
    /// <summary>
    /// Verifies that a <see cref="DateTime" /> is written in the round-trip form, which keeps its sub-second ticks.
    /// </summary>
    [TestMethod]
    public void Format_WhenDateTimeHasTicks_ForDelimitedPolicy_ShouldWriteRoundTripText()
    {
        string text = InvariantScalarCodec.Format(s_instant, InvariantScalarCodec.Policy.Delimited);

        Assert.AreEqual(s_instant.ToString("O", CultureInfo.InvariantCulture), text);
    }
}
