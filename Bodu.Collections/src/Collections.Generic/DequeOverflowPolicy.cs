// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeOverflowPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Specifies how a fixed-capacity <see cref="Deque{T}" /> responds when an add operation is attempted while the deque
/// is already full.
/// </summary>
/// <remarks>
/// <para>
/// The policy is consulted only when <see cref="Deque{T}.AllowGrow" /> is <see langword="false" /> and
/// <see cref="RingBackedCollection{T}.Count" /> equals <see cref="RingBackedCollection{T}.Capacity" />. When
/// <see cref="Deque{T}.AllowGrow" /> is <see langword="true" /> the backing array grows instead, so the policy has no
/// effect.
/// </para>
/// <para>
/// <see cref="EvictOpposite" /> is the double-ended counterpart of the overwrite-on-full mode selected by
/// <see cref="CircularBuffer{T}.AllowOverwrite" />, and matches the semantics of Python's
/// <c>collections.deque(maxlen=N)</c>.
/// </para>
/// </remarks>
public enum DequeOverflowPolicy
{
    /// <summary>
    /// Adds to a full deque are rejected: <see cref="Deque{T}.AddFirst(T)" /> and <see cref="Deque{T}.AddLast(T)" />
    /// throw <see cref="InvalidOperationException" />, while <see cref="Deque{T}.TryAddFirst(T)" /> and
    /// <see cref="Deque{T}.TryAddLast(T)" /> return <see langword="false" /> without modifying state. This is the
    /// default and preserves the historical fixed-capacity behavior.
    /// </summary>
    Reject = 0,

    /// <summary>
    /// Adds to a full deque silently discard the element at the opposite end to make room — the tail element for
    /// <see cref="Deque{T}.AddFirst(T)" />, the head element for <see cref="Deque{T}.AddLast(T)" /> — so
    /// <see cref="RingBackedCollection{T}.Count" /> stays at <see cref="RingBackedCollection{T}.Capacity" /> and the
    /// <c>Try*</c> variants return <see langword="true" />. This mirrors Python's <c>collections.deque(maxlen=N)</c>
    /// bounded-deque semantics.
    /// </summary>
    EvictOpposite = 1,
}
