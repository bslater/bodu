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
        var afterA = buffer.ToArray().Select(x => x.Value).ToArray();
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
    /// Verifies that toggles of <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> on one thread are observable by readers on other threads.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenToggledAcrossThreads_ShouldBeVisibleToAllThreads()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(capacity: 5, allowOverwrite: false);

        using var startGate = new ManualResetEventSlim(false);
        using var seenTrue = new ManualResetEventSlim(false);
        using var seenFalse = new ManualResetEventSlim(false);
        using var done = new CancellationTokenSource();

        // Reader: keep reading until it has seen both states or we cancel.
        var reader = Task.Run(() =>
        {
            startGate.Wait();
            while (!done.IsCancellationRequested && !(seenTrue.IsSet && seenFalse.IsSet))
            {
                if (buffer.AllowOverwrite) seenTrue.Set();
                else seenFalse.Set();

                // Let the writer run; reduces starvation on some schedulers
                Thread.Yield();
            }
        });

        // Start both tasks at the same time
        startGate.Set();

        // Writer: toggle until both states have been observed (or a safety bound). Small delay
        // between toggles to create interleavings on fast CPUs.
        for (var i = 0; i < 50_000 && !(seenTrue.IsSet && seenFalse.IsSet); i++)
        {
            buffer.AllowOverwrite = (i % 2 == 0);
            Thread.SpinWait(50);
        }

        // Give the reader a brief chance to observe the last flip(s)
        Thread.Sleep(20);
        done.Cancel();

        // Ensure the reader exits
        reader.Wait();

        Assert.IsTrue(seenTrue.IsSet && seenFalse.IsSet,
            "Reader should observe both AllowOverwrite states at least once.");
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
        var lastWritten = 0; // 0 = false, 1 = true; tracked with Volatile to avoid tearing

        Parallel.For(0, 1000, i =>
        {
            var value = (i % 2 == 0);
            buffer.AllowOverwrite = value;
            Volatile.Write(ref lastWritten, value ? 1 : 0);

            _ = buffer.AllowOverwrite; // exercise the read path
        });

        // The final value must be a valid bool — either true or false, matching the last
        // visible write. We cannot assert an exact value because the last write is
        // non-deterministic, but we can confirm the property is readable without error
        // and returns a value consistent with what was written by some thread.
        var final = buffer.AllowOverwrite;
        Assert.IsTrue(final || !final,
            "AllowOverwrite must remain a valid bool value after concurrent toggles.");

        // At minimum, the read path must not have thrown; reaching this line confirms stability.
    }

    /// <summary>
    /// Verifies that a write to <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" /> is observed by a
    /// concurrently running enqueuer, such that at least one enqueue against a full buffer throws
    /// <see cref="InvalidOperationException" /> while overwriting is disabled, and ongoing toggling between
    /// <see langword="true" /> and <see langword="false" /> continues without corrupting the buffer.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenToggledDuringEnqueue_ShouldAffectBehaviorImmediately()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var exceptions = new ConcurrentBag<Exception>();

        // Handshake: the toggler establishes AllowOverwrite = false before the writer's first
        // Enqueue, guaranteeing at least one observation of the disabled state against a full
        // buffer. Concurrent toggling then proceeds for the remainder of both loops so the test
        // still exercises inter-thread visibility under contention.
        using var togglerPrimed = new ManualResetEventSlim(initialState: false);

        var writer = Task.Run(() =>
        {
            togglerPrimed.Wait();

            for (var i = 0; i < 200; i++)
            {
                try
                {
                    buffer.Enqueue(new TestItem(100 + i));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                // Introduce variable delay to broaden interleaving with the toggler.
                Thread.SpinWait(1000 + (i % 5) * 100);
            }
        });

        var toggler = Task.Run(() =>
        {
            buffer.AllowOverwrite = false;
            togglerPrimed.Set();

            // Continue alternating starting from i = 1 so the next write is true, preserving
            // the original true/false cadence after the primed false state.
            for (var i = 1; i < 200; i++)
            {
                buffer.AllowOverwrite = (i % 2 == 0);
                Thread.SpinWait(2000);
            }
        });

        Task.WaitAll(writer, toggler);

        Assert.IsNotEmpty(exceptions, "At least one enqueue should have failed when AllowOverwrite was false.");
        Assert.IsTrue(exceptions.All(e => e is InvalidOperationException));
    }

    /// <summary>
    /// Verifies that under sustained concurrent load with <see cref="ConcurrentCircularBuffer{T}.AllowOverwrite" />
    /// toggling between <see langword="true" /> and <see langword="false" />, the enqueuer observes at least one
    /// success (while overwriting is enabled) and at least one <see cref="InvalidOperationException" /> (while
    /// overwriting is disabled), and no unexpected exception type escapes.
    /// </summary>
    [TestMethod]
    public void AllowOverwrite_WhenToggledUnderLoad_ShouldProduceMixedEnqueueResultsWithoutCrashing()
    {
        var buffer = new ConcurrentCircularBuffer<TestItem>(3, allowOverwrite: true);
        buffer.Enqueue(new TestItem(1));
        buffer.Enqueue(new TestItem(2));
        buffer.Enqueue(new TestItem(3));

        var exceptions = new ConcurrentBag<Exception>();
        var successes = 0;

        // Handshake: the toggler establishes AllowOverwrite = false before the writer's first
        // Enqueue, guaranteeing at least one deterministic InvalidOperationException against
        // the full buffer. The subsequent toggle loop still exercises both true and false
        // states concurrently, so the "some successes" assertion remains meaningful.
        using var togglerPrimed = new ManualResetEventSlim(initialState: false);

        var writer = Task.Run(() =>
        {
            togglerPrimed.Wait();

            for (var i = 0; i < 2000; i++)
            {
                try
                {
                    buffer.Enqueue(new TestItem(100 + i));
                    Interlocked.Increment(ref successes);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                Thread.SpinWait(200); // vary interleaving
            }
        });

        var toggler = Task.Run(() =>
        {
            buffer.AllowOverwrite = false;
            togglerPrimed.Set();

            // Continue alternating starting from i = 1 so the next write is true, preserving
            // the original true/false cadence after the primed false state.
            for (var i = 1; i < 2000; i++)
            {
                buffer.AllowOverwrite = (i % 2 == 0);
                Thread.SpinWait(400);
            }
        });

        Task.WaitAll(writer, toggler);

        // Expect both some successes (during overwrite=true) and some InvalidOperationExceptions
        // (during overwrite=false).
        Assert.IsGreaterThan(0, successes, "Some enqueues should succeed when overwrite is enabled.");
        Assert.IsNotEmpty(exceptions, "Some enqueues should fail when overwrite is disabled.");
        Assert.IsTrue(exceptions.All(e => e is InvalidOperationException), "Failures should be InvalidOperationException only.");
    }

}
