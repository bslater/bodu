// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncManualResetEventTests.Reset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncManualResetEventTests
{
    /// <summary>
    /// Verifies that resetting a set event causes subsequent waits to block again.
    /// </summary>
    [TestMethod]
    public void Reset_WhenSet_ShouldReArmTheGate()
    {
        var sut = new AsyncManualResetEvent(initialState: true);

        sut.Reset();

        Assert.IsFalse(sut.IsSet);
        Assert.IsFalse(sut.WaitAsync().IsCompleted);
    }

    /// <summary>
    /// Verifies that resetting an already-unsignaled event is a no-op.
    /// </summary>
    [TestMethod]
    public void Reset_WhenAlreadyUnsignaled_ShouldRemainUnsignaled()
    {
        var sut = new AsyncManualResetEvent();
        var pending = sut.WaitAsync();

        sut.Reset();

        Assert.IsFalse(sut.IsSet);

        // The original waiter is still observed by a later Set, proving the gate was not replaced.
        sut.Set();
        Assert.IsTrue(pending.IsCompleted);
    }

    /// <summary>
    /// Verifies that the event can be set, reset, and set again across multiple cycles.
    /// </summary>
    [TestMethod]
    public async Task Reset_WhenCycledRepeatedly_ShouldReArmEachTime()
    {
        var sut = new AsyncManualResetEvent();

        for (var i = 0; i < 3; i++)
        {
            var pending = sut.WaitAsync();
            Assert.IsFalse(pending.IsCompleted);

            sut.Set();
            await pending;
            Assert.IsTrue(sut.IsSet);

            sut.Reset();
            Assert.IsFalse(sut.IsSet);
        }
    }
}
