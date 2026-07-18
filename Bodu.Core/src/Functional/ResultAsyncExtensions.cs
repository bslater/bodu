// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

/// <summary>
/// Provides Task-based asynchronous companions to the <see cref="Result{T}" /> and non-generic <see cref="Result" />
/// railway combinators.
/// </summary>
/// <remarks>
/// <para>
/// Each operation mirrors its synchronous counterpart — <c>MapAsync</c> pairs with
/// <see cref="Result{T}.Map{TResult}(Func{T, TResult})" />, <c>BindAsync</c> with
/// <see cref="Result{T}.Bind{TResult}(Func{T, Result{TResult}})" />, <c>MatchAsync</c> with
/// <see cref="Result{T}.Match{TResult}(Func{T, TResult}, Func{ResultError, TResult})" />, and <c>TapAsync</c> /
/// <c>TapErrorAsync</c> with <see cref="Result{T}.Tap(Action{T})" /> and
/// <see cref="Result{T}.TapError(Action{ResultError})" /> — so an asynchronous pipeline over
/// <c>Task&lt;Result&lt;T&gt;&gt;</c> composes exactly like its synchronous equivalent, with the error propagating
/// untouched past every combinator.
/// </para>
/// <para>
/// Argument validation runs synchronously, so a <see langword="null" /> source task or delegate faults at the call site
/// rather than on the returned task.
/// </para>
/// <para>
/// The combinators accept no <see cref="CancellationToken" />: they perform no I/O of their own, so cancellation
/// belongs to the caller's delegates, which can close over their own tokens.
/// </para>
/// </remarks>
public static partial class ResultAsyncExtensions
{
}
