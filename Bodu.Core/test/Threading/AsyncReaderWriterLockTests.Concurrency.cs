// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLockTests.Concurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLockTests
{
    /// <summary>
    /// Verifies that no reader is ever active at the same time as a writer under contention.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public async Task ReaderWriter_WhenContended_ShouldNeverOverlap()
    {
        var sut = new AsyncReaderWriterLock();
        int readers = 0;
        int writers = 0;
        int violations = 0;

        async Task Reader()
        {
            for (int i = 0; i < 200; i++)
            {
                using (await sut.ReaderAsync())
                {
                    Interlocked.Increment(ref readers);
                    if (Volatile.Read(ref writers) != 0)
                        Interlocked.Increment(ref violations);
                    await Task.Yield();
                    Interlocked.Decrement(ref readers);
                }
            }
        }

        async Task Writer()
        {
            for (int i = 0; i < 200; i++)
            {
                using (await sut.WriterAsync())
                {
                    Interlocked.Increment(ref writers);
                    if (Volatile.Read(ref readers) != 0 || Volatile.Read(ref writers) != 1)
                        Interlocked.Increment(ref violations);
                    await Task.Yield();
                    Interlocked.Decrement(ref writers);
                }
            }
        }

        var tasks = new List<Task>();
        for (int i = 0; i < 4; i++)
            tasks.Add(Task.Run(Reader));
        for (int i = 0; i < 2; i++)
            tasks.Add(Task.Run(Writer));

        await Task.WhenAll(tasks);

        Assert.AreEqual(0, violations, "Readers and writers must never overlap.");
    }
}
