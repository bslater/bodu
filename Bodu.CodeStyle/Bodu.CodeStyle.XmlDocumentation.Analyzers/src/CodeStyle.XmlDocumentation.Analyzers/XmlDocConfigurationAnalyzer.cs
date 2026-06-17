// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationAnalyzer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
/// Reports <c>BODU0001</c> when a <c>bodu.xmldocstyle.json</c> additional file is present but cannot be parsed
/// or applied. The formatting analyzers degrade gracefully to the built-in defaults on a bad configuration file;
/// this analyzer makes that failure visible in the IDE and build log so a misconfiguration is never silent.
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
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        ImmutableArray<XmlDocConfigurationError> errors = XmlDocConfigurationLoader.CollectConfigurationErrors(
            context.Options.AdditionalFiles,
            context.CancellationToken);

        if (errors.IsDefaultOrEmpty) return;

        // The errors are known at compilation start, but a diagnostic can only be reported from a registered
        // action; emit them once at compilation end.
        context.RegisterCompilationEndAction(endContext =>
        {
            foreach (XmlDocConfigurationError error in errors)
            {
                endContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.XmlDocConfigInvalid,
                    error.Location,
                    error.Message));
            }
        });
    }
}
