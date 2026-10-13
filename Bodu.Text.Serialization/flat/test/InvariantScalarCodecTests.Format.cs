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
    /// Verifies that a decimal is written with the invariant separator even when the current culture uses a comma.
    /// </summary>
    [TestMethod]
    public void Format_WhenCurrentCultureUsesCommaSeparator_ForConfigurationPolicy_ShouldWriteInvariantText()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            Assert.AreEqual("1.25", InvariantScalarCodec.Format(1.25m, InvariantScalarCodec.Policy.Configuration));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that a <see cref="bool" /> is written in lower case.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueIsBoolean_ForConfigurationPolicy_ShouldWriteLowerCase()
    {
        Assert.AreEqual("false", InvariantScalarCodec.Format(false, InvariantScalarCodec.Policy.Configuration));
    }

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
