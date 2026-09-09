// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderStateTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffReaderState" /> and <see cref="BiffReaderOptions" />.
/// </summary>
[TestClass]
public sealed class BiffReaderStateTests
{
    /// <summary>
    /// Verifies that a default state describes an unknown version and the default code page.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldDescribeStreamStart()
    {
        var state = new BiffReaderState();

        Assert.AreEqual(BiffVersion.Unknown, state.Version);
        Assert.AreEqual(BiffLimits.DefaultCodePage, state.CodePage);
        Assert.AreEqual(default, state.Options);
    }

    /// <summary>
    /// Verifies that options seed the state and remain retrievable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptions_ShouldSeedState()
    {
        var options = new BiffReaderOptions { Version = BiffVersion.Biff8, CodePage = 1251 };

        var state = new BiffReaderState(options);

        Assert.AreEqual(BiffVersion.Biff8, state.Version);
        Assert.AreEqual(1251, state.CodePage);
        Assert.AreEqual(options, state.Options);
    }
}
