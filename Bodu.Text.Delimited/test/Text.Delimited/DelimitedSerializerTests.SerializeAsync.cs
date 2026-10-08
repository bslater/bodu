// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.SerializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the member backbone tests for the <c>SerializeAsync</c> overloads of <see cref="DelimitedSerializer" />,
/// which write a collection of records, or an asynchronous sequence of them, to a stream.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>How long a blocked caller waits for an asynchronous operation before it is taken to have deadlocked.</summary>
    private static readonly TimeSpan s_deadlockTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Verifies that a caller which blocks a single-threaded <see cref="SynchronizationContext" /> on either
    /// <c>SerializeAsync</c> overload, writing to a stream whose writes complete asynchronously, or on enumerating
    /// <see cref="DelimitedSerializer.DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, CancellationToken)" />
    /// over a stream whose reads complete asynchronously, sees each operation complete rather than deadlock.
    /// </summary>
    [TestMethod]
    public void SerializeAsync_WhenBlockingUnderASingleThreadedSynchronizationContext_ShouldNotDeadlock()
    {
        var people = new List<Person> { new() { Name = "Ada", Age = 36 }, new() { Name = "Grace", Age = 45 } };
        using var bufferedDestination = new AsynchronousOnlyStream();
        using var incrementalDestination = new AsynchronousOnlyStream();
        using var source = new AsynchronousOnlyStream(Encoding.UTF8.GetBytes(DelimitedSerializer.Serialize(people)));

        var context = new QueueingSynchronizationContext();
        SynchronizationContext? previous = SynchronizationContext.Current;
        bool bufferedCompleted;
        bool incrementalCompleted;
        bool readCompleted;
        Task<List<Person>> read;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);

            bufferedCompleted = DelimitedSerializer.SerializeAsync(bufferedDestination, people).AsTask().Wait(s_deadlockTimeout);
            incrementalCompleted = DelimitedSerializer
                .SerializeAsync(incrementalDestination, ToContextFreeAsyncEnumerable(people))
                .AsTask()
                .Wait(s_deadlockTimeout);
            read = ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<Person>(source));
            readCompleted = read.Wait(s_deadlockTimeout);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);

            // Let any continuation that did wait for the blocked context finish, so that nothing is left waiting.
            context.RunPosted();
        }

        Assert.IsTrue(bufferedCompleted, "Blocking on SerializeAsync over a collection deadlocked.");
        Assert.IsTrue(incrementalCompleted, "Blocking on SerializeAsync over an asynchronous sequence deadlocked.");
        Assert.IsTrue(readCompleted, "Blocking on the enumeration of DeserializeAsyncEnumerableAsync deadlocked.");
        Assert.AreEqual(DelimitedSerializer.Serialize(people), Encoding.UTF8.GetString(bufferedDestination.ToArray()));
        Assert.AreEqual(DelimitedSerializer.Serialize(people), Encoding.UTF8.GetString(incrementalDestination.ToArray()));
        CollectionAssert.AreEqual(new[] { "Ada", "Grace" }, read.Result.Select(person => person.Name).ToArray());
    }

    /// <summary>
    /// Verifies that both <c>SerializeAsync</c> overloads write their records to a stream whose synchronous
    /// <see cref="Stream.Write(byte[], int, int)" /> and <see cref="Stream.Flush" /> throw
    /// <see cref="NotSupportedException" />, as an ASP.NET Core response body does when synchronous I/O is disallowed.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenTheStreamForbidsSynchronousWrites_ShouldWrite()
    {
        var people = new List<Person> { new() { Name = "Ada", Age = 36 }, new() { Name = "Grace", Age = 45 } };
        using var buffered = new AsynchronousOnlyStream();
        using var incremental = new AsynchronousOnlyStream();

        await DelimitedSerializer.SerializeAsync(buffered, people);
        await DelimitedSerializer.SerializeAsync(incremental, ToAsyncEnumerable(people));

        Assert.AreEqual(DelimitedSerializer.Serialize(people), Encoding.UTF8.GetString(buffered.ToArray()));
        Assert.AreEqual(DelimitedSerializer.Serialize(people), Encoding.UTF8.GetString(incremental.ToArray()));
    }

    /// <summary>
    /// Yields records as an asynchronous sequence whose every step completes asynchronously without resuming on the
    /// caller's <see cref="SynchronizationContext" />, so that only the consumer could bring a deadlock about.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="records">The records to yield.</param>
    /// <returns>An asynchronous sequence of the records.</returns>
    private static async IAsyncEnumerable<TRecord> ToContextFreeAsyncEnumerable<TRecord>(IEnumerable<TRecord> records)
    {
        foreach (TRecord record in records)
        {
            await Task.Delay(1).ConfigureAwait(false);
            yield return record;
        }
    }
}
