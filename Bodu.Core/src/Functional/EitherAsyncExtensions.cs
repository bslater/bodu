// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

/// <summary>
/// Provides Task-based asynchronous companions to the <see cref="Either{TLeft, TRight}" /> combinators.
/// </summary>
/// <remarks>
/// <para>
/// Each operation mirrors its synchronous counterpart — <c>MapLeftAsync</c> pairs with
/// <see cref="Either{TLeft, TRight}.MapLeft{TResult}(Func{TLeft, TResult})" />, <c>MapRightAsync</c> with
/// <see cref="Either{TLeft, TRight}.MapRight{TResult}(Func{TRight, TResult})" />, and <c>MatchAsync</c> with
/// <see cref="Either{TLeft, TRight}.Match{TResult}(Func{TLeft, TResult}, Func{TRight, TResult})" /> — so an
/// asynchronous pipeline over <c>Task&lt;Either&lt;TLeft, TRight&gt;&gt;</c> composes exactly like its synchronous
/// equivalent.
/// </para>
/// <para>
/// Argument validation runs synchronously, so a <see langword="null" /> source task or delegate faults at the call site
/// rather than on the returned task.
/// </para>
/// <para>
/// Every combinator preserves the uninitialized-instance contract of the underlying type: operating on
/// <c>default(Either&lt;TLeft, TRight&gt;)</c> throws <see cref="InvalidOperationException" />, matching the
/// synchronous combinators.
/// </para>
/// <para>
/// The combinators accept no <see cref="CancellationToken" />: they perform no I/O of their own, so cancellation
/// belongs to the caller's delegates, which can close over their own tokens.
/// </para>
/// </remarks>
public static partial class EitherAsyncExtensions
{
}
