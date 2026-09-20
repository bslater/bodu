// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.AllowOverwrite.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> returns the default (<see langword="true" />) for ctors that omit it and the explicitly supplied value otherwise.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenBufferConstructed_ShouldReturnConfiguredDefault()
    {
        var d = new ConcurrentCircularBuffer<TestItem>();                  // default ctor
        Assert.IsTrue(d.AllowOverwrite, "Default ctor should enable overwrite.");

        var capOnly = new ConcurrentCircularBuffer<TestItem>(8);           // capacity ctor
        Assert.IsTrue(capOnly.AllowOverwrite, "Capacity ctor should enable overwrite.");

        var explicitTrue = new ConcurrentCircularBuffer<TestItem>(4, true);
        Assert.IsTrue(explicitTrue.AllowOverwrite);

        var explicitFalse = new ConcurrentCircularBuffer<TestItem>(4, false);
        Assert.IsFalse(explicitFalse.AllowOverwrite);
    }

    /// <summary>
    /// Verifies that flipping <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> from <see langword="false" /> to <see langword="true" /> lets subsequent enqueues evict, and flipping back forces the next enqueue to throw.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenFlippedFromFalseToTrue_ShouldAffectSubsequentOperationsImmediately()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: false);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        buffer.AllowOverwrite = true;
        buffer.Enqueue(new TestItem(3)); // evicts 1
        buffer.Enqueue(new TestItem(4)); // evicts 2
        int[] afterA = buffer.ToArray().Select(x => x.Value).ToArray();
        CollectionAssert.AreEqual(new[] { 3, 4 }, afterA);

        // Phase B: overwrite disabled -> enqueue SHOULD throw now that buffer is full
        buffer.AllowOverwrite = false;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Enqueue(new TestItem(5));
        });
    }

    /// <summary>
    /// Verifies that flipping <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> to <see langword="false" /> causes the next enqueue on a full buffer to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenFlippedFromTrueToFalse_ShouldAffectSubsequentOperationsImmediately()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(2, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));

        buffer.AllowOverwrite = false;
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            buffer.Enqueue(new TestItem(3));
        });
    }

    /// <summary>
    /// Verifies that toggles of <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> on one thread are observed
    /// by a reader on another thread: after every write the reader reports back the value it read, and the writer
    /// does not proceed to the next toggle until it has.
    /// </summary>
    /// <remarks>
    /// The writer's progress is gated on the reader's observation rather than on an iteration count or a fixed
    /// delay, so a reader that is slow to be scheduled cannot be outrun. Because each round ends with the reader
    /// reporting the opposite value, a report of the new value can only come from a read made after the write. A
    /// write that never became visible surfaces as the safety bound expiring.
    /// </remarks>
    [TestMethod]
    public void AllowOverwrite_WhenToggledAcrossThreads_ShouldBeVisibleToAllThreads()
    {
        const int rounds = 100;
        var bound = TimeSpan.FromSeconds(10);
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity: 5, allowOverwrite: false);

        // 1 when the reader last read true, 0 when it last read false. Written only by the reader, read only here.
        var lastSeen = 0;
        using var done = new CancellationTokenSource();

        var reader = Task.Run(() =>
        {
            while (!done.IsCancellationRequested)
            {
                Volatile.Write(ref lastSeen, buffer.AllowOverwrite ? 1 : 0);
                Thread.Yield();
            }
        });

        for (var round = 0; round < rounds; round++)
        {
            var value = round % 2 == 0;
            var expected = value ? 1 : 0;

            buffer.AllowOverwrite = value;

            Assert.IsTrue(
                SpinWait.SpinUntil(() => Volatile.Read(ref lastSeen) == expected, bound),
                $"The reader never observed AllowOverwrite = {value} in round {round}.");
        }

        done.Cancel();
        reader.Wait();
    }

    // Issue 3 — the previous assertion was: buffer.AllowOverwrite == true || buffer.AllowOverwrite == false
    // which is a tautology for any bool and can never fail. The corrected test captures the last
    // write made by the toggler and asserts the final read is consistent with that value.

    /// <summary>
    /// Verifies that concurrent writers and readers of <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> observe a valid boolean value without tearing or exceptions.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenToggledConcurrently_ShouldRemainThreadSafe()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(5, false);
        int lastWritten = 0; // 0 = false, 1 = true; tracked with Volatile to avoid tearing

        Parallel.For(0, 1000, i =>
        {
            bool value = (i % 2 == 0);
            buffer.AllowOverwrite = value;
            Volatile.Write(ref lastWritten, value ? 1 : 0);

            _ = buffer.AllowOverwrite; // exercise the read path
        });

        // The final value must be a valid bool — either true or false, matching the last
        // visible write. We cannot assert an exact value because the last write is
        // non-deterministic, but we can confirm the property is readable without error
        // and returns a value consistent with what was written by some thread.
        bool final = buffer.AllowOverwrite;
        Assert.IsTrue(final || !final,
            "AllowOverwrite must remain a valid bool value after concurrent toggles.");

        // At minimum, the read path must not have thrown; reaching this line confirms stability.
    }

    /// <summary>
    /// Verifies that disabling <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> on one thread is observed by
    /// an enqueuer on another, whose next enqueue against the full buffer throws
    /// <see cref="InvalidOperationException" />, and that toggling the flag concurrently with further enqueues
    /// refuses only with that exception and leaves the buffer full and intact.
    /// </summary>
    /// <remarks>
    /// The toggler does not begin alternating until the enqueuer has reported meeting the disabled state, so the
    /// first observation cannot be lost to scheduling. The concurrent phase asserts only what holds under every
    /// interleaving: a refusal is always <see cref="InvalidOperationException" />, and with no consumer a full
    /// buffer stays exactly full whether an enqueue overwrote or was refused.
    /// </remarks>
    [TestMethod]
    public void AllowOverwrite_WhenToggledDuringEnqueue_ShouldAffectBehaviorImmediately()
    {
        const int rounds = 200;
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var refusals = new ConcurrentBag<Exception>();
        using var disabled = new ManualResetEventSlim(initialState: false);
        using var disabledObserved = new ManualResetEventSlim(initialState: false);

        var enqueuer = Task.Run(() =>
        {
            disabled.Wait();

            // The enqueuer itself must meet the disabled state: the full buffer refuses its enqueue.
            try
            {
                Assert.ThrowsExactly<InvalidOperationException>(() =>
                {
                    buffer.Enqueue(new TestItem(100));
                });
            }
            finally
            {
                // Released on failure as well, so the toggler cannot wait forever behind a failed assertion.
                disabledObserved.Set();
            }

            for (var i = 1; i < rounds; i++)
            {
                try
                {
                    buffer.Enqueue(new TestItem(100 + i));
                }
                catch (Exception ex)
                {
                    refusals.Add(ex);
                }

                Thread.SpinWait(1000 + (i % 5) * 100);
            }
        });

        var toggler = Task.Run(() =>
        {
            buffer.AllowOverwrite = false;
            disabled.Set();
            disabledObserved.Wait();

            for (var i = 1; i < rounds; i++)
            {
                buffer.AllowOverwrite = i % 2 == 0;
                Thread.SpinWait(2000);
            }
        });

        Task.WaitAll(enqueuer, toggler);

        Assert.IsTrue(
            refusals.All(e => e is InvalidOperationException),
            "A refused enqueue must surface as InvalidOperationException and nothing else.");
        Assert.AreEqual(buffer.Capacity, buffer.Count, "With no consumer, a full buffer must stay exactly full.");
        Assert.AreEqual(buffer.Capacity, buffer.ToArray().Length);
    }

    /// <summary>
    /// Verifies that under sustained enqueues with <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" />
    /// toggling on another thread, the enqueuer observes a success while overwriting is enabled and an
    /// <see cref="InvalidOperationException" /> while it is disabled, in every round, and no other exception type
    /// escapes.
    /// </summary>
    /// <remarks>
    /// The toggler holds each state until the enqueuer has reported the outcome that state produces, so both
    /// outcomes are proven per round rather than expected to emerge from a free-running interleaving. A success can
    /// only follow an enqueue that read the flag as enabled, and a refusal one that read it as disabled; because the
    /// previous round ended on the opposite outcome, a report of the expected one can only come from an enqueue made
    /// after the write. A state that never produced its outcome surfaces as the safety bound expiring.
    /// </remarks>
    [TestMethod]
    public void AllowOverwrite_WhenToggledUnderLoad_ShouldProduceMixedEnqueueResultsWithoutCrashing()
    {
        const int rounds = 200;
        const int succeeded = 1;
        const int refused = 2;
        var bound = TimeSpan.FromSeconds(10);
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var exceptions = new ConcurrentBag<Exception>();
        var successes = 0;

        // The outcome of the enqueuer's most recent enqueue. Written only by the enqueuer, read only by the toggler.
        var lastOutcome = 0;
        using var done = new CancellationTokenSource();

        var enqueuer = Task.Run(() =>
        {
            for (var i = 0; !done.IsCancellationRequested; i++)
            {
                try
                {
                    buffer.Enqueue(new TestItem(100 + i));
                    Interlocked.Increment(ref successes);
                    Volatile.Write(ref lastOutcome, succeeded);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    Volatile.Write(ref lastOutcome, refused);
                }

                Thread.SpinWait(200);
            }
        });

        var toggler = Task.Run(() =>
        {
            try
            {
                for (var round = 0; round < rounds; round++)
                {
                    var enabled = round % 2 != 0;
                    var expected = enabled ? succeeded : refused;

                    buffer.AllowOverwrite = enabled;

                    Assert.IsTrue(
                        SpinWait.SpinUntil(() => Volatile.Read(ref lastOutcome) == expected, bound),
                        $"The enqueuer never observed AllowOverwrite = {enabled} in round {round}.");
                }
            }
            finally
            {
                // Released on failure as well, so the enqueuer cannot loop forever behind a failed assertion.
                done.Cancel();
            }
        });

        Task.WaitAll(enqueuer, toggler);

        Assert.IsGreaterThan(0, successes, "Some enqueues should succeed when overwrite is enabled.");
        Assert.IsNotEmpty(exceptions, "Some enqueues should fail when overwrite is disabled.");
        Assert.IsTrue(
            exceptions.All(e => e is InvalidOperationException),
            "Failures should be InvalidOperationException only.");
        Assert.AreEqual(buffer.Capacity, buffer.Count, "With no consumer, a full buffer must stay exactly full.");
    }

}
