// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarTool.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Globalization.Calendar.Tool;

/// <summary>
/// Implements the <c>bodu-calendar</c> command surface — <c>lint</c>, <c>compile</c>, and <c>info</c> — as an
/// in-process entry point the console host and the tests share.
/// </summary>
/// <remarks>
/// <para>
/// Exit codes: <c>0</c> success, <c>1</c> validation or input failure, <c>2</c> usage error. Diagnostics print one
/// per line as <c>[Severity] CODE: message</c>, so build logs and editors can match on the stable
/// <c>BODU-CAL-*</c> codes.
/// </para>
/// </remarks>
public static class CalendarTool
{
    /// <summary>The exit code for a successful run.</summary>
    public const int ExitSuccess = 0;

    /// <summary>The exit code for a validation or input failure.</summary>
    public const int ExitFailure = 1;

    /// <summary>The exit code for a usage error.</summary>
    public const int ExitUsage = 2;

    /// <summary>
    /// Runs the tool against the supplied arguments.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="output">The writer receiving normal output.</param>
    /// <param name="error">The writer receiving errors and usage help.</param>
    /// <returns>The process exit code.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="args" />, <paramref name="output" />, or <paramref name="error" /> is <see langword="null" />.
    /// </exception>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ThrowHelper.ThrowIfNull(args);
        ThrowHelper.ThrowIfNull(output);
        ThrowHelper.ThrowIfNull(error);

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            error.WriteLine(ToolResourceStrings.Usage);
            return ExitUsage;
        }

        return args[0].ToUpperInvariant() switch
        {
            "LINT" => RunLint(args, output, error),
            "COMPILE" => RunCompile(args, output, error),
            "INFO" => RunInfo(args, output, error),
            _ => Usage(error, string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_UnknownCommand, args[0])),
        };
    }

    /// <summary>
    /// Prints an error followed by the usage help.
    /// </summary>
    /// <param name="error">The error writer.</param>
    /// <param name="message">The error message.</param>
    /// <returns>The usage exit code.</returns>
    private static int Usage(TextWriter error, string message)
    {
        error.WriteLine(message);
        error.WriteLine(ToolResourceStrings.Usage);

        return ExitUsage;
    }

    /// <summary>
    /// Parses the shared verb arguments: one input file plus the optional output and resolver-directory options.
    /// </summary>
    /// <param name="args">The full argument vector; index 0 is the verb.</param>
    /// <param name="error">The error writer.</param>
    /// <param name="input">The input file path.</param>
    /// <param name="outputPath">The value of <c>-o</c>/<c>--output</c>, or <see langword="null" />.</param>
    /// <param name="resolverDir">The value of <c>--resolver-dir</c>, or <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the arguments parsed; otherwise usage help was printed.</returns>
    private static bool TryParseArguments(string[] args, TextWriter error, out string input, out string? outputPath, out string? resolverDir)
    {
        input = string.Empty;
        outputPath = null;
        resolverDir = null;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o" or "--output":
                    if (++i >= args.Length)
                    {
                        _ = Usage(error, string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_MissingArgument, args[i - 1]));
                        return false;
                    }

                    outputPath = args[i];
                    break;

                case "--resolver-dir":
                    if (++i >= args.Length)
                    {
                        _ = Usage(error, string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_MissingArgument, args[i - 1]));
                        return false;
                    }

                    resolverDir = args[i];
                    break;

                default:
                    if (args[i].StartsWith('-') || input.Length != 0)
                    {
                        _ = Usage(error, string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_UnknownOption, args[i]));
                        return false;
                    }

                    input = args[i];
                    break;
            }
        }

        if (input.Length == 0)
        {
            _ = Usage(error, string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_MissingArgument, args[0]));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds the import resolver: files in the resolver directory first, then the bundled common catalogues.
    /// </summary>
    /// <param name="resolverDir">The optional directory whose files satisfy imports.</param>
    /// <returns>The composed resolver.</returns>
    private static Func<string, string?> BuildResolver(string? resolverDir) =>
        name =>
        {
            if (resolverDir is not null)
            {
                foreach (string extension in new[] { ".xml", ".json" })
                {
                    string candidate = Path.Combine(resolverDir, name + extension);
                    if (File.Exists(candidate))
                        return File.ReadAllText(candidate);
                }
            }

            return CommonNotableDateResources.Resolve(name);
        };

    /// <summary>
    /// Loads a document in collect mode, choosing the XML or JSON entry point by extension.
    /// </summary>
    /// <param name="input">The document path.</param>
    /// <param name="resolver">The import resolver.</param>
    /// <param name="error">The error writer.</param>
    /// <param name="resource">The loaded resource, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collected diagnostics.</param>
    /// <param name="exitCode">The exit code when loading could not start.</param>
    /// <returns><see langword="true" /> when a load ran (successfully or not); otherwise <paramref name="exitCode" /> applies.</returns>
    private static bool TryLoadDocument(
        string input,
        Func<string, string?> resolver,
        TextWriter error,
        out NotableDateResource? resource,
        out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics,
        out int exitCode)
    {
        resource = null;
        diagnostics = [];
        exitCode = ExitSuccess;

        if (!File.Exists(input))
        {
            error.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_FileNotFound, input));
            exitCode = ExitFailure;
            return false;
        }

        string extension = Path.GetExtension(input);
        string content = File.ReadAllText(input);

        if (string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase))
        {
            _ = NotableDateResourceLoader.TryLoad(content, resolver, out resource, out diagnostics);
            return true;
        }

        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            _ = NotableDateResourceLoader.TryLoadJson(content, resolver, out resource, out diagnostics);
            return true;
        }

        error.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_UnsupportedExtension, extension));
        exitCode = ExitUsage;
        return false;
    }

    /// <summary>
    /// Prints every diagnostic, one per line.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to print.</param>
    /// <param name="output">The writer receiving the lines.</param>
    private static void PrintDiagnostics(IReadOnlyList<NotableDateValidationDiagnostic> diagnostics, TextWriter output)
    {
        foreach (NotableDateValidationDiagnostic diagnostic in diagnostics)
            output.WriteLine(diagnostic.ToString());
    }

    /// <summary>
    /// Runs the <c>lint</c> verb.
    /// </summary>
    /// <param name="args">The argument vector.</param>
    /// <param name="output">The output writer.</param>
    /// <param name="error">The error writer.</param>
    /// <returns>The exit code.</returns>
    private static int RunLint(string[] args, TextWriter output, TextWriter error)
    {
        if (!TryParseArguments(args, error, out string input, out _, out string? resolverDir))
            return ExitUsage;

        if (!TryLoadDocument(input, BuildResolver(resolverDir), error, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics, out int exitCode))
            return exitCode;

        PrintDiagnostics(diagnostics, output);

        if (resource is null)
        {
            int errors = diagnostics.Count(d => d.Severity == NotableDateValidationSeverity.Error);
            output.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Lint_Failed, input, errors, diagnostics.Count));
            return ExitFailure;
        }

        output.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Lint_Clean, input, resource.ResourceId, diagnostics.Count));
        return ExitSuccess;
    }

    /// <summary>
    /// Runs the <c>compile</c> verb.
    /// </summary>
    /// <param name="args">The argument vector.</param>
    /// <param name="output">The output writer.</param>
    /// <param name="error">The error writer.</param>
    /// <returns>The exit code.</returns>
    private static int RunCompile(string[] args, TextWriter output, TextWriter error)
    {
        if (!TryParseArguments(args, error, out string input, out string? outputPath, out string? resolverDir))
            return ExitUsage;

        if (!TryLoadDocument(input, BuildResolver(resolverDir), error, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics, out int exitCode))
            return exitCode;

        if (resource is null)
        {
            PrintDiagnostics(diagnostics, error);
            int errors = diagnostics.Count(d => d.Severity == NotableDateValidationSeverity.Error);
            error.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Lint_Failed, input, errors, diagnostics.Count));
            return ExitFailure;
        }

        string packPath = outputPath ?? Path.ChangeExtension(input, ".bcal");
        using (FileStream stream = File.Create(packPath))
        {
            NotableDateBinaryResource.Write(resource, stream);
        }

        byte[] pack = File.ReadAllBytes(packPath);
        string digest = Convert.ToHexString(pack.AsSpan(8, SHA256.HashSizeInBytes)).ToLowerInvariant();
        output.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Compile_Succeeded, resource.ResourceId, packPath, pack.Length, digest));

        return ExitSuccess;
    }

    /// <summary>
    /// Runs the <c>info</c> verb.
    /// </summary>
    /// <param name="args">The argument vector.</param>
    /// <param name="output">The output writer.</param>
    /// <param name="error">The error writer.</param>
    /// <returns>The exit code.</returns>
    private static int RunInfo(string[] args, TextWriter output, TextWriter error)
    {
        if (!TryParseArguments(args, error, out string input, out _, out _))
            return ExitUsage;

        if (!File.Exists(input))
        {
            error.WriteLine(string.Format(CultureInfo.CurrentCulture, ToolResourceStrings.Error_FileNotFound, input));
            return ExitFailure;
        }

        try
        {
            byte[] pack = File.ReadAllBytes(input);

            NotableDateResource resource;
            using (MemoryStream stream = new(pack))
            {
                resource = NotableDateResourceLoader.LoadBinary(stream);
            }

            ushort version = BinaryPrimitives.ReadUInt16LittleEndian(pack.AsSpan(4));
            string digest = Convert.ToHexString(pack.AsSpan(8, SHA256.HashSizeInBytes)).ToLowerInvariant();

            output.WriteLine(string.Format(
                CultureInfo.CurrentCulture,
                ToolResourceStrings.Info_Summary,
                input,
                version,
                resource.ResourceId,
                resource.SchemaVersion,
                resource.NotableDates.Count,
                resource.AdjustmentPolicies.Count,
                digest));

            return ExitSuccess;
        }
        catch (NotableDateBinaryFormatException ex)
        {
            error.WriteLine(ex.Message);
            return ExitFailure;
        }
    }
}
