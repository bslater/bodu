// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCache{T,T}.Maintenance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentLruCache<TKey, TValue>
{
    /// <summary>The number of cycle passes an amortized (write-triggered) maintenance run performs before yielding to a later write.</summary>
    private const int AmortizedCyclePasses = 3;

    /// <summary>The safety bound on cycle passes for the blocking <see cref="RunMaintenance" /> test hook, which loops until a pass makes no progress.</summary>
    private const int MaxConvergencePasses = 64;

    /// <summary>
    /// Runs write-amortized queue maintenance. While the cache is within its capacity, at most one thread cycles the
    /// queues at a time and contending writers return immediately; a writer that observes the cache <em>over</em>
    /// capacity instead blocks on the maintenance lock and converges before returning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The blocking path is the write back-pressure that makes the capacity contract real: writers cannot outpace
    /// eviction, because each one that pushes the live count past <see cref="Capacity" /> must finish converging it
    /// back under the bound before its own operation completes. Reads never take this path and remain lock-free; the
    /// cost lands on sustained insert pressure against a full cache, where writes serialize on the maintenance lock.
    /// </para>
    /// <para>
    /// The maintenance lock is released <em>before</em> eviction notifications are dispatched, so
    /// <see cref="ItemEvicted" /> handlers never run while queue maintenance is blocked and may safely call back into
    /// the cache.
    /// </para>
    /// </remarks>
    private void Maintain()
    {
        List<KeyValuePair<TKey, TValue>>? evicted = null;

        if (Volatile.Read(ref _liveCount) > Capacity)
        {
            lock (_maintenanceLock)
            {
                for (int pass = 0; pass < MaxConvergencePasses; pass++)
                {
                    if (!CyclePass(ref evicted))
                        break;
                }
            }
        }
        else
        {
            if (!Monitor.TryEnter(_maintenanceLock))
                return;

            try
            {
                for (int pass = 0; pass < AmortizedCyclePasses; pass++)
                {
                    if (!CyclePass(ref evicted))
                        break;
                }
            }
            finally
            {
                Monitor.Exit(_maintenanceLock);
            }
        }

        OnEvicted(evicted);
    }

    /// <summary>
    /// Runs queue maintenance to convergence, blocking on the maintenance lock. Exposed to the test assembly so tests
    /// can observe deterministic post-maintenance state.
    /// </summary>
    /// <remarks>
    /// In the absence of concurrent mutation, every queue is within its capacity slice when this method returns.
    /// </remarks>
    internal void RunMaintenance()
    {
        List<KeyValuePair<TKey, TValue>>? evicted = null;
        lock (_maintenanceLock)
        {
            for (int pass = 0; pass < MaxConvergencePasses; pass++)
            {
                if (!CyclePass(ref evicted))
                    break;
            }
        }

        OnEvicted(evicted);
    }

    /// <summary>
    /// Performs one cycle pass: drains each queue down to its capacity slice, promoting accessed entries, demoting
    /// unaccessed ones, and evicting from the cold end. The caller must hold the maintenance lock.
    /// </summary>
    /// <param name="evicted">The buffer that receives entries evicted from the cold queue.</param>
    /// <returns>
    /// <see langword="true" /> if the pass moved, dropped, or evicted at least one node — meaning another pass may make
    /// further progress; <see langword="false" /> when every queue was already within its slice.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Hot drain: an accessed node is promoted to warm, an unaccessed one demoted to cold. Warm drain: an accessed node
    /// recycles to the warm tail (its flag cleared, so it gets exactly one free pass), an unaccessed one demotes to
    /// cold; the drain is bounded by the warm count observed at the start so a fully accessed warm queue cannot spin
    /// the pass. Cold drain: an accessed node resurrects to warm, an unaccessed one is evicted — claimed with an
    /// interlocked Cold→Removed transition and then removed from the dictionary with the node-conditional overload so a
    /// same-key successor entry is never removed.
    /// </para>
    /// <para>
    /// A node observed in the <see cref="Node.StateRemoved" /> state anywhere is dead (explicitly removed, replaced, or
    /// cleared) and is dropped; a failed lifecycle transition means the node died concurrently and is likewise dropped.
    /// </para>
    /// <para>
    /// The pass ends with a hard capacity enforcement loop: while the live count exceeds <see cref="Capacity" />,
    /// entries are evicted from the cold head regardless of their accessed flags, with warm and hot demoted to refill
    /// cold as needed. This guarantees the capacity contract even when sustained reads re-flag entries faster than the
    /// policy cycling can retire them.
    /// </para>
    /// </remarks>
    private bool CyclePass(ref List<KeyValuePair<TKey, TValue>>? evicted)
    {
        bool moved = false;

        // Hot: new arrivals prove themselves or fall through toward eviction.
        while (Volatile.Read(ref _hotCount) > _hotCapacity && TryDequeue(_hotQueue, ref _hotCount, out Node? node))
        {
            moved = true;

            if (node.State == Node.StateRemoved)
                continue;

            if (node.WasAccessed)
            {
                node.WasAccessed = false;

                if (node.TryTransition(Node.StateHot, Node.StateWarm))
                    Enqueue(_warmQueue, ref _warmCount, node);
            }
            else if (node.TryTransition(Node.StateHot, Node.StateCold))
            {
                Enqueue(_coldQueue, ref _coldCount, node);
            }
        }

        // Warm: accessed entries recycle once per pass; unaccessed entries demote. The budget bounds the drain to the
        // occupancy observed at the start so a fully accessed warm queue cannot spin this loop.
        int warmBudget = Volatile.Read(ref _warmCount);
        while (Volatile.Read(ref _warmCount) > _warmCapacity && warmBudget-- > 0 && TryDequeue(_warmQueue, ref _warmCount, out Node? node))
        {
            moved = true;

            if (node.State == Node.StateRemoved)
                continue;

            if (node.WasAccessed)
            {
                node.WasAccessed = false;
                Enqueue(_warmQueue, ref _warmCount, node);
            }
            else if (node.TryTransition(Node.StateWarm, Node.StateCold))
            {
                Enqueue(_coldQueue, ref _coldCount, node);
            }
        }

        // Cold: accessed entries earn a resurrection to warm; unaccessed entries are evicted.
        while (Volatile.Read(ref _coldCount) > _coldCapacity && TryDequeue(_coldQueue, ref _coldCount, out Node? node))
        {
            moved = true;

            if (node.State == Node.StateRemoved)
                continue;

            if (node.WasAccessed)
            {
                node.WasAccessed = false;

                if (node.TryTransition(Node.StateCold, Node.StateWarm))
                    Enqueue(_warmQueue, ref _warmCount, node);
            }
            else if (node.TryTransition(Node.StateCold, Node.StateRemoved)
                && _dictionary.TryRemove(new KeyValuePair<TKey, Node>(node.Key, node)))
            {
                Interlocked.Decrement(ref _liveCount);
                (evicted ??= new List<KeyValuePair<TKey, TValue>>()).Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            }
        }

        // Hard capacity enforcement. The policy cycling above can stall against readers that continuously re-flag
        // resident entries — every cold examination then resurrects instead of evicting, and the live count creeps
        // above the capacity. When that happens, evict from the cold head regardless of accessed flags, demoting from
        // warm and hot to refill cold as needed, until the live count is back within the bound. Accessed entries
        // sacrificed here are the price of the documented capacity contract under pathological read pressure.
        while (Volatile.Read(ref _liveCount) > Capacity)
        {
            if (TryDequeue(_coldQueue, ref _coldCount, out Node? node))
            {
                moved = true;

                if (node.State == Node.StateRemoved)
                    continue;

                if (node.TryTransition(Node.StateCold, Node.StateRemoved)
                    && _dictionary.TryRemove(new KeyValuePair<TKey, Node>(node.Key, node)))
                {
                    Interlocked.Decrement(ref _liveCount);
                    (evicted ??= new List<KeyValuePair<TKey, TValue>>()).Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
                }
            }
            else if (TryDequeue(_warmQueue, ref _warmCount, out node) || TryDequeue(_hotQueue, ref _hotCount, out node))
            {
                moved = true;

                if (node.State == Node.StateRemoved)
                    continue;

                node.WasAccessed = false;
                int state = node.State;
                if (state != Node.StateRemoved && node.TryTransition(state, Node.StateCold))
                    Enqueue(_coldQueue, ref _coldCount, node);
            }
            else
            {
                // Every queue is momentarily empty (in-flight writers hold undelivered nodes); a later maintenance
                // run finishes the job.
                break;
            }
        }

        return moved;
    }

    /// <summary>
    /// Enqueues a node and increments the queue's occupancy counter.
    /// </summary>
    /// <param name="queue">The target queue.</param>
    /// <param name="count">The queue's occupancy counter field.</param>
    /// <param name="node">The node to enqueue.</param>
    private static void Enqueue(ConcurrentQueue<Node> queue, ref int count, Node node)
    {
        queue.Enqueue(node);
        Interlocked.Increment(ref count);
    }

    /// <summary>
    /// Dequeues a node and decrements the queue's occupancy counter.
    /// </summary>
    /// <param name="queue">The source queue.</param>
    /// <param name="count">The queue's occupancy counter field.</param>
    /// <param name="node">When this method returns <see langword="true" />, the dequeued node.</param>
    /// <returns><see langword="true" /> if a node was dequeued; otherwise, <see langword="false" />.</returns>
    private static bool TryDequeue(ConcurrentQueue<Node> queue, ref int count, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Node? node)
    {
        if (queue.TryDequeue(out node))
        {
            Interlocked.Decrement(ref count);
            return true;
        }

        return false;
    }
}
