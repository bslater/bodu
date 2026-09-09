// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffLimitsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffLimits" />.
/// </summary>
[TestClass]
public sealed class BiffLimitsTests
{
    /// <summary>
    /// Verifies that the per-version maximum payload lengths match the format's published limits.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expected">The expected limit.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 2080)]
    [DataRow(BiffVersion.Biff8, 8224)]
    [DataRow(BiffVersion.Unknown, 65535)]
    public void GetMaxPayloadLength_WhenVersion_ShouldReturnPublishedLimit(BiffVersion version, int expected)
    {
        Assert.AreEqual(expected, BiffLimits.GetMaxPayloadLength(version));
    }
}
