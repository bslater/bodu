// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyStatusAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

/// <summary>
/// Associates a <see cref="CurrencyStatus" /> with a <see cref="CurrencyCode" /> enum member.
/// </summary>
/// <remarks>
/// The attribute is the declarative source of truth for a currency's lifecycle status. Query it efficiently through
/// <see cref="CurrencyCodeExtensions.GetStatus(CurrencyCode)" />, which caches the reflected values at type
/// initialization.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class CurrencyStatusAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyStatusAttribute" /> class.
    /// </summary>
    /// <param name="status">The lifecycle status assigned to the annotated currency.</param>
    public CurrencyStatusAttribute(CurrencyStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Gets the lifecycle status assigned to the annotated currency.
    /// </summary>
    /// <value>The <see cref="CurrencyStatus" /> carried by the annotated member.</value>
    /// <returns>The lifecycle status of the annotated <see cref="CurrencyCode" /> member.</returns>
    public CurrencyStatus Status { get; }
}
