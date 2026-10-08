// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Bodu.Test.Corpus;

namespace Bodu.Text.Configuration;

/// <summary>
/// Holds Bodu.Text.Configuration to the defect fixes EditorConfig cores list in their release notes, as catalogued in
/// <c>corpus/configuration/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A row's input is decoded as UTF-8, a byte order mark kept as the character U+FEFF, parsed with
/// <see cref="ConfigurationDocument.Parse(string, ConfigurationParseOptions?)" /> under the options
/// <see cref="ConfigurationParseOptions.For(ConfigurationProfile)" /> gives for the row's profile, and resolved for the
/// row's target path under <see cref="ConfigurationResolveOptions.For(ConfigurationProfile)" />. The view is rendered
/// as a compact JSON object of its pairs in enumeration order, each key and value a JSON string that escapes only the
/// quotation mark, the backslash and control characters, with <c>null</c> standing for a null value:
/// <c>{"indent_size":"4"}</c>, or <c>{}</c> when nothing applies.
/// </para>
/// <para>
/// A round-trip row saves the parsed document with <see cref="ConfigurationWriteOptions.For(ConfigurationProfile)" />,
/// parses the written text again, and compares the two renderings, and the written text with the row's expected text
/// when the row gives one.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class ConfigurationReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "configuration";

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames = ["Profile", "Target"];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(ConfigurationReleaseNoteCorpusTests).Assembly));

    /// <summary>
    /// Gets the runnable rows of kind <c>parse</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> ParseRows =>
        RunnableRows("parse");

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RejectRows =>
        RunnableRows("reject");

    /// <summary>
    /// Gets the runnable rows of kind <c>roundtrip</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RoundTripRows =>
        RunnableRows("roundtrip");

    /// <summary>
    /// Gets every row of every embedded catalogue.
    /// </summary>
    /// <value>The rows, catalogue by catalogue in file order.</value>
    private static IEnumerable<ReleaseNoteFix> AllRows =>
        s_catalogs.Value.SelectMany(catalog => catalog.Rows);

    /// <summary>
    /// Selects the runnable rows of one kind as test data.
    /// </summary>
    /// <param name="kind">The row kind.</param>
    /// <returns>
    /// One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.
    /// </returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Parses configuration text under a row's profile and resolves it for the row's target path.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The resolved view.</returns>
    /// <exception cref="ConfigurationParseException">The text could not be parsed under the row's profile.</exception>
    private static ConfigurationView ReadAndResolve(string text, RowOptions options)
    {
        var document = ConfigurationDocument.Parse(text, ConfigurationParseOptions.For(options.Profile));
        return document.Resolve(options.Target, ConfigurationResolveOptions.For(options.Profile));
    }

    /// <summary>
    /// Parses and resolves configuration text, rendering the view, or describing the parse exception when the text is
    /// rejected.
    /// </summary>
    /// <param name="text">The configuration text.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering of the view, or the description of the exception the parse threw.</returns>
    private static string RenderOrDescribe(string text, RowOptions options)
    {
        try
        {
            return Render(ReadAndResolve(text, options));
        }
        catch (ConfigurationParseException ex)
        {
            return Describe(ex);
        }
    }

    /// <summary>
    /// Saves a document with the write options of a row's profile.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The written text.</returns>
    private static string Write(ConfigurationDocument document, RowOptions options)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        ConfigurationDocument.Save(document, writer, ConfigurationWriteOptions.For(options.Profile));
        return writer.ToString();
    }

    /// <summary>
    /// Renders a resolved view as a compact JSON object in the view's enumeration order.
    /// </summary>
    /// <param name="view">The view to render.</param>
    /// <returns>
    /// The JSON text, such as <c>{"indent_size":"4"}</c>, with <c>null</c> standing for a null value.
    /// </returns>
    private static string Render(ConfigurationView view)
    {
        var builder = new StringBuilder("{");
        bool first = true;
        foreach (KeyValuePair<string, string?> pair in view)
        {
            if (!first)
                builder.Append(',');

            first = false;
            AppendJsonString(builder, pair.Key);
            builder.Append(':');
            if (pair.Value is null)
                builder.Append("null");
            else
                AppendJsonString(builder, pair.Value);
        }

        return builder.Append('}').ToString();
    }

    /// <summary>
    /// Appends a JSON string literal, escaping only the quotation mark, the backslash and control characters.
    /// </summary>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="value">The string to write.</param>
    /// <remarks>
    /// Every other character, non-ASCII text included, is written as it is, so a rendering compares equal to the
    /// decoded <c>expected</c> field of its row.
    /// </remarks>
    private static void AppendJsonString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;

                case '\\':
                    builder.Append(@"\\");
                    break;

                case '\b':
                    builder.Append(@"\b");
                    break;

                case '\f':
                    builder.Append(@"\f");
                    break;

                case '\n':
                    builder.Append(@"\n");
                    break;

                case '\r':
                    builder.Append(@"\r");
                    break;

                case '\t':
                    builder.Append(@"\t");
                    break;

                default:
                    if (c < ' ')
                        builder.Append(@"\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);

                    break;
            }
        }

        builder.Append('"');
    }

    /// <summary>
    /// Describes a parse exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name, the code of its diagnostic when it carries one, and its message.</returns>
    private static string Describe(ConfigurationParseException exception)
    {
        string code = exception.Diagnostic is { } diagnostic ? $" [{diagnostic.Code}]" : string.Empty;
        return $"{exception.GetType().Name}{code}: {exception.Message}";
    }
}
