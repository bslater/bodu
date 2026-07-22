// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratorDiagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Declares the diagnostics reported by <see cref="FormatFactoryGenerator" />.
/// </summary>
internal static class GeneratorDiagnostics
{
    /// <summary>The diagnostic category shared by every generator diagnostic.</summary>
    private const string Category = "Bodu.Text.Formats.Generators";

    /// <summary>BTFG001: the annotated type (or one of its containing types) is not declared <see langword="partial" />, so the generator cannot add the factory to it.</summary>
    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "BTFG001",
        title: "Annotated type must be partial",
        messageFormat: "The type '{0}' is annotated with [{1}] but it (or a containing type) is not declared partial, so no factory is generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BTFG002: a property's type is not a supported scalar, so the generated factory skips the member.</summary>
    public static readonly DiagnosticDescriptor MemberTypeUnsupported = new(
        id: "BTFG002",
        title: "Member type is not a supported scalar",
        messageFormat: "The property '{0}' on '{1}' has type '{2}', which is not a supported scalar; the generated factory skips it",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>BTFG003: the annotated type is generic, which the generated static factory property cannot represent.</summary>
    public static readonly DiagnosticDescriptor TypeMustNotBeGeneric = new(
        id: "BTFG003",
        title: "Annotated type must not be generic",
        messageFormat: "The type '{0}' is annotated with [{1}] but is generic (or nested in a generic type), so no factory is generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
