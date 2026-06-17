// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocCodeRequiresCDataCodeFixProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes;

/// <summary>
/// Provides code fixes for <c>BODU1405</c> — repairs <c>&lt;code&gt;</c> documentation elements so they
/// begin with a <c>&lt;![CDATA[…]]&gt;</c> section flush against the <c>///</c> doc-comment prefix.
/// </summary>
/// <remarks>
/// Three fixes are registered depending on the violating shape:
/// <list type="bullet">
///   <item><description>Removes stray whitespace between <c>///</c> and <c>&lt;![CDATA[</c> when the CDATA is present but offset by a space or tab.</description></item>
///   <item><description>Rewrites a self-closing <c>&lt;code /&gt;</c> as a multi-line <c>&lt;code&gt;</c> element whose body is an empty <c>&lt;![CDATA[…]]&gt;</c> block, with the opener and closer each on their own line flush against <c>///</c>.</description></item>
///   <item><description>Wraps the existing body of a non-empty <c>&lt;code&gt;</c> element in a multi-line <c>&lt;![CDATA[…]]&gt;</c> block, placing the opener and closer on their own lines flush against <c>///</c> and preserving the existing body content between them.</description></item>
/// </list>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocCodeRequiresCDataCodeFixProvider))]
[Shared]
public sealed class XmlDocCodeRequiresCDataCodeFixProvider : CodeFixProvider
{
    private const string RemoveSpaceTitle = "Remove space between /// and <![CDATA[";

    private const string WrapTitle = "Wrap <code> body in <![CDATA[…]]>";

    private const string RemoveSpaceEquivalenceKey = "BoduRemoveSpaceBeforeXmlDocCData";

    private const string WrapEquivalenceKey = "BoduWrapXmlDocCodeBodyInCData";

    private const string DocCommentPrefix = "/// ";

    private const string DocCommentPrefixNoSpace = "///";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.XmlDocCodeRequiresCData);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            TextSpan span = diagnostic.Location.SourceSpan;
            SyntaxNode? node = root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);

            switch (node)
            {
                case XmlCDataSectionSyntax cdata:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: RemoveSpaceTitle,
                            createChangedDocument: ct => RemoveLeadingWhitespaceAsync(context.Document, cdata, ct),
                            equivalenceKey: RemoveSpaceEquivalenceKey),
                        diagnostic);
                    break;

                case XmlEmptyElementSyntax emptyElement:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: WrapTitle,
                            createChangedDocument: ct => ReplaceSelfClosingWithCDataAsync(context.Document, emptyElement, ct),
                            equivalenceKey: WrapEquivalenceKey),
                        diagnostic);
                    break;

                case XmlElementStartTagSyntax startTag when startTag.Parent is XmlElementSyntax element:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: WrapTitle,
                            createChangedDocument: ct => WrapElementBodyInCDataAsync(context.Document, element, ct),
                            equivalenceKey: WrapEquivalenceKey),
                        diagnostic);
                    break;
            }
        }
    }

    private static async Task<Document> RemoveLeadingWhitespaceAsync(Document document, XmlCDataSectionSyntax cdata, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var position = cdata.SpanStart - 1;
        while (position >= 0 && (text[position] == ' ' || text[position] == '\t'))
        {
            position--;
        }

        var whitespaceStart = position + 1;
        var whitespaceEnd = cdata.SpanStart;
        if (whitespaceStart >= whitespaceEnd) return document;

        SourceText updated = text.WithChanges(new TextChange(TextSpan.FromBounds(whitespaceStart, whitespaceEnd), string.Empty));
        return document.WithText(updated);
    }

    private static async Task<Document> ReplaceSelfClosingWithCDataAsync(Document document, XmlEmptyElementSyntax element, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var localName = element.Name.LocalName.ValueText;

        var lineEnding = DetectLineEnding(text);
        var indent = ExtractDocCommentIndent(text, element.SpanStart);

        var sb = new StringBuilder();
        sb.Append('<').Append(localName).Append('>');
        sb.Append(lineEnding);
        sb.Append(indent).Append("///<![CDATA[");
        sb.Append(lineEnding);
        sb.Append(indent).Append("///]]>");
        sb.Append(lineEnding);
        sb.Append(indent).Append("/// </").Append(localName).Append('>');

        SourceText updated = text.WithChanges(new TextChange(element.Span, sb.ToString()));
        return document.WithText(updated);
    }

    private static async Task<Document> WrapElementBodyInCDataAsync(Document document, XmlElementSyntax element, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var bodyStart = element.StartTag.Span.End;
        var bodyEnd = element.EndTag.Span.Start;
        if (bodyStart > bodyEnd) return document;

        var lineEnding = DetectLineEnding(text);
        var indent = ExtractDocCommentIndent(text, element.StartTag.SpanStart);

        var existing = text.ToString(TextSpan.FromBounds(bodyStart, bodyEnd));
        IReadOnlyList<string> bodyLines = ExtractLogicalBodyLines(existing);

        // Compose the multi-line CDATA replacement: opener and closer each on their own line flush
        // against `///`. Every logical body line is re-emitted under the standard `/// ` prefix, so the
        // physical doc-comment prefixes that introduced each source line are never embedded into — nor
        // duplicated within — the CDATA content. Blank body lines collapse to a bare `///` with no
        // trailing whitespace.
        var sb = new StringBuilder();
        sb.Append(lineEnding);
        sb.Append(indent).Append("///<![CDATA[");
        sb.Append(lineEnding);
        foreach (var line in bodyLines)
        {
            sb.Append(line.Length == 0 ? indent + DocCommentPrefixNoSpace : indent + DocCommentPrefix + line);
            sb.Append(lineEnding);
        }

        sb.Append(indent).Append("///]]>");
        sb.Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix);

        SourceText updated = text.WithChanges(new TextChange(TextSpan.FromBounds(bodyStart, bodyEnd), sb.ToString()));
        return document.WithText(updated);
    }

    private static IReadOnlyList<string> ExtractLogicalBodyLines(string body)
    {
        // Split the raw inter-tag body into physical lines, strip each line's leading indentation, its
        // `///` doc-comment prefix, and one following space to recover the logical code content, then drop
        // any leading or trailing blank lines so the CDATA wrapper alone controls the surrounding layout.
        var stripped = new List<string>();
        var start = 0;
        for (var i = 0; i <= body.Length; i++)
        {
            if (i < body.Length && body[i] != '\n' && body[i] != '\r') continue;

            stripped.Add(StripDocCommentPrefix(body.Substring(start, i - start)));
            if (i < body.Length && body[i] == '\r' && i + 1 < body.Length && body[i + 1] == '\n') i++;
            start = i + 1;
        }

        var first = 0;
        while (first < stripped.Count && stripped[first].Length == 0) first++;
        var last = stripped.Count - 1;
        while (last >= first && stripped[last].Length == 0) last--;

        var result = new List<string>();
        for (var k = first; k <= last; k++)
        {
            result.Add(stripped[k]);
        }

        return result;
    }

    private static string StripDocCommentPrefix(string physicalLine)
    {
        var i = 0;
        while (i < physicalLine.Length && (physicalLine[i] == ' ' || physicalLine[i] == '\t')) i++;

        // Strip a single `///` prefix when present, plus one following space (the standard `/// ` form).
        if (i + 2 < physicalLine.Length && physicalLine[i] == '/' && physicalLine[i + 1] == '/' && physicalLine[i + 2] == '/')
        {
            i += 3;
            if (i < physicalLine.Length && physicalLine[i] == ' ') i++;
        }

        return physicalLine.Substring(i).TrimEnd();
    }

    private static string DetectLineEnding(SourceText text)
    {
        var length = text.Length;
        for (var i = 0; i < length; i++)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                return i + 1 < length && text[i + 1] == '\n' ? "\r\n" : "\r";
            }

            if (ch == '\n')
            {
                return "\n";
            }
        }

        return "\r\n";
    }

    private static string ExtractDocCommentIndent(SourceText text, int position)
    {
        // Walk backward from `position` to find the start of the current line.
        var lineStart = position;
        while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        // The indent is the whitespace run from line start up to the first non-whitespace character
        // (which on a doc-comment line will be the first `/` of the `///` prefix).
        var end = lineStart;
        while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
        {
            end++;
        }

        return text.ToString(TextSpan.FromBounds(lineStart, end));
    }
}
