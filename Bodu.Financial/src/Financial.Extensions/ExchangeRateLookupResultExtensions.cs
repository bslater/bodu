// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResultExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

/// <summary>
/// Provides audit-convenience members for <see cref="ExchangeRateLookupResult" /> that describe how the resolved rate
/// relates to the requested date, keeping those derived helpers off the core result type.
/// </summary>
/// <remarks>
/// <para>
/// Each member is a thin projection over the result's backing data, its resolved rate, the requested date, and the
/// recorded day offset, carrying no state of its own.
/// </para>
/// <para>
/// When compiled with a tool-chain that supports C# 14 extension members the helpers are exposed as extension
/// properties (for example <c>result.IsExactDate</c>); otherwise they compile as classic extension methods (for example
/// <c>result.IsExactDate()</c>).
/// </para>
/// </remarks>
public static partial class ExchangeRateLookupResultExtensions
{
}
