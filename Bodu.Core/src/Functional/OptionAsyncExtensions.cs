// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

/// <summary>
/// Provides Task-based asynchronous companions to the <see cref="Option{T}" /> railway combinators.
/// </summary>
/// <remarks>
/// <para>
/// Each operation mirrors its synchronous counterpart — <c>MapAsync</c> pairs with
/// <see cref="Option{T}.Map{TResult}(Func{T, TResult})" />, <c>BindAsync</c> with
/// <see cref="Option{T}.Bind{TResult}(Func{T, Option{TResult}})" />, and <c>MatchAsync</c> with
/// <see cref="Option{T}.Match{TResult}(Func{T, TResult}, Func{TResult})" /> — so an asynchronous pipeline over
/// <c>Task&lt;Option&lt;T&gt;&gt;</c> composes exactly like its synchronous equivalent.
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
public static partial class OptionAsyncExtensions
{
}
