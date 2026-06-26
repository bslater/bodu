// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationAnalyzer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers;

/// <summary>
/// Reports <c>BODU0001</c> when a <c>bodu.xmldocstyle.json</c> additional file is present but cannot be parsed or
/// applied. The formatting analyzers degrade gracefully to the built-in defaults on a bad configuration file; this
/// analyzer makes that failure visible in the IDE and build log so a misconfiguration is never silent.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocConfigurationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.XmlDocConfigInvalid);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationAction(OnCompilation);
    }

    /// <summary>
    /// Collects configuration errors from the additional files and reports a <c>BODU0001</c> diagnostic for each once
    /// the compilation has completed.
    /// </summary>
    /// <param name="context">
    /// The compilation analysis context supplying the additional files and diagnostic sink.
    /// </param>
    private static void OnCompilation(CompilationAnalysisContext context)
    {
        ImmutableArray<XmlDocConfigurationError> errors = XmlDocConfigurationLoader.CollectConfigurationErrors(
            context.Options.AdditionalFiles,
            context.CancellationToken);

        if (errors.IsDefaultOrEmpty) return;

        foreach (XmlDocConfigurationError error in errors)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.XmlDocConfigInvalid,
                error.Location,
                error.Message));
        }
    }
}
