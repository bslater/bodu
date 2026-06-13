// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

/// <summary>
/// Provides sign, magnitude, and comparison convenience members for the strongly-typed <see cref="Money{TCurrency}" />
/// type, keeping those derived helpers off the core value type while preserving familiar property-style access.
/// </summary>
/// <remarks>
/// <para>
/// Each instance member is a thin projection over <see cref="Money{TCurrency}.Amount" /> and the public arithmetic
/// surface, carrying no state of its own; the <c>Min</c>, <c>Max</c>, and <c>Clamp</c> helpers are static because they
/// compare two or more operands. Splitting them into this extension surface keeps <see cref="Money{TCurrency}" />
/// focused on construction, arithmetic, equality, and formatting.
/// </para>
/// <para>
/// When compiled with a tool-chain that supports C# 14 extension members the instance helpers are exposed as extension
/// properties (for example <c>money.IsZero</c>); otherwise they compile as classic extension methods (for example
/// <c>money.IsZero()</c>).
/// </para>
/// </remarks>
public static partial class MoneyOfTCurrencyExtensions
{
}
