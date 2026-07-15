// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Configuration-bindable options for the Bodu.Financial dependency-injection surface.
/// </summary>
/// <remarks>
/// The type currently declares no options of its own; it remains the binding target for the <c>AddFinancialService</c>
/// configuration section so future options bind without an API change. JSON serialization options live in the companion
/// <c>Bodu.Financial.Serialization.Json</c> package and are registered via its <c>AddFinancialJson</c> extension.
/// </remarks>
public sealed class FinancialOptions
{
}
