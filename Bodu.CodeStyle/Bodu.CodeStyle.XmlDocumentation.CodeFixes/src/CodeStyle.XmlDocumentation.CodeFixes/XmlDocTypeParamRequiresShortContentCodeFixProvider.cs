// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTypeParamRequiresShortContentCodeFixProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
/// Provides the code fix for <c>BODU1406</c> — shortens a <c>&lt;typeparam&gt;</c> element to its first
/// sentence and relocates the trailing prose into a <c>&lt;para&gt;</c> appended to the existing
/// <c>&lt;remarks&gt;</c> block (or wrapped in a new <c>&lt;remarks&gt;</c> block inserted immediately after
/// the <c>&lt;typeparam&gt;</c>).
/// </summary>
/// <remarks>
/// <para>
/// The fix preserves the original surface text verbatim — no rephrasing or grammar adjustment is attempted.
/// The author may re-word the relocated <c>&lt;para&gt;</c> by hand if the standalone reading is awkward.
/// </para>
/// <para>
/// When the source already contains a type-level <c>&lt;remarks&gt;</c> block, the new <c>&lt;para&gt;</c>
/// is appended as the final paragraph so the original reading order is preserved. When no
/// <c>&lt;remarks&gt;</c> exists, a fresh three-line block is synthesized immediately after the
/// <c>&lt;typeparam&gt;</c> line, matching the canonical Bodu doc-tag ordering of summary → typeparam →
/// remarks.
/// </para>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocTypeParamRequiresShortContentCodeFixProvider))]
[Shared]
public sealed class XmlDocTypeParamRequiresShortContentCodeFixProvider : CodeFixProvider
{
    private const string MoveProseTitle = "Move trailing prose to <remarks>";

    private const string MoveProseEquivalenceKey = "BoduMoveTypeParamProseToRemarks";

    private const string DocCommentPrefix = "/// ";

    private const string DocCommentPrefixNoSpace = "///";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.XmlDocTypeParamRequiresShortContent);

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

            XmlElementSyntax? element = ResolveTypeParamElement(node);
            if (element is null) continue;

            XmlElementSyntax captured = element;
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: MoveProseTitle,
                    createChangedDocument: ct => MoveProseToRemarksAsync(context.Document, captured, ct),
                    equivalenceKey: MoveProseEquivalenceKey),
                diagnostic);
        }
    }

    private static XmlElementSyntax? ResolveTypeParamElement(SyntaxNode? node)
    {
        return node switch
        {
            XmlElementSyntax direct => direct,
            XmlElementStartTagSyntax start when start.Parent is XmlElementSyntax parent => parent,
            _ => node?.FirstAncestorOrSelf<XmlElementSyntax>(),
        };
    }

    private static async Task<Document> MoveProseToRemarksAsync(Document document, XmlElementSyntax typeParam, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var content = GetContentText(typeParam);
        var splitIndex = FindFirstSentenceBoundary(content);
        if (splitIndex < 0) return document;

        var firstSentence = content.Substring(0, splitIndex + 1);
        var remainingRaw = content.Substring(splitIndex + 1);
        var remaining = TrimRemainingProse(remainingRaw);
        if (remaining.Length == 0) return document;

        var startTagText = typeParam.StartTag.ToString();
        var endTagText = typeParam.EndTag.ToString();
        var newTypeParamText = startTagText + firstSentence + endTagText;

        var lineEnding = DetectLineEnding(text);
        var indent = ExtractLineIndent(text, typeParam.SpanStart);

        DocumentationCommentTriviaSyntax? docComment = typeParam.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docComment is null) return document;

        XmlElementSyntax? remarks = FindRemarksElement(docComment);

        var changes = new List<TextChange>
        {
            new TextChange(typeParam.Span, newTypeParamText),
        };

        if (remarks is not null)
        {
            // Append a new <para> as the final paragraph inside the existing <remarks> body. The insertion
            // point is the start of the line carrying the </remarks> end tag — this keeps the indentation
            // contract by re-using the indent + prefix already in the source.
            var endTagLineStart = FindLineStart(text, remarks.EndTag.SpanStart);
            var insertion = BuildAppendedPara(indent, lineEnding, remaining);
            changes.Add(new TextChange(new TextSpan(endTagLineStart, 0), insertion));
        }
        else
        {
            // No <remarks> present: insert a synthesized <remarks><para>…</para></remarks> immediately
            // after the line carrying the LAST <typeparam>'s end tag — when a generic type declares several
            // type parameters this keeps the canonical summary → typeparam(s) → remarks ordering AND lets
            // subsequent fixes in a FixAll pass append their relocated paragraphs to one shared block.
            XmlElementSyntax anchor = FindLastTypeParam(docComment) ?? typeParam;
            var afterAnchorLineEnd = FindLineEndIncludingTerminator(text, anchor.Span.End);
            var insertion = BuildNewRemarksBlock(indent, lineEnding, remaining);
            changes.Add(new TextChange(new TextSpan(afterAnchorLineEnd, 0), insertion));
        }

        SourceText updated = text.WithChanges(changes);
        return document.WithText(updated);
    }

    private static XmlElementSyntax? FindRemarksElement(DocumentationCommentTriviaSyntax docComment)
    {
        return docComment.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e =>
                e.StartTag.Name.Prefix is null
                && string.Equals(e.StartTag.Name.LocalName.ValueText, "remarks", System.StringComparison.Ordinal));
    }

    private static XmlElementSyntax? FindLastTypeParam(DocumentationCommentTriviaSyntax docComment)
    {
        return docComment.Content
            .OfType<XmlElementSyntax>()
            .LastOrDefault(e =>
                e.StartTag.Name.Prefix is null
                && string.Equals(e.StartTag.Name.LocalName.ValueText, "typeparam", System.StringComparison.Ordinal));
    }

    private static string GetContentText(XmlElementSyntax element)
    {
        var sb = new StringBuilder();
        foreach (XmlNodeSyntax node in element.Content)
        {
            sb.Append(node.ToString());
        }

        return sb.ToString();
    }

    private static int FindFirstSentenceBoundary(string content)
    {
        for (var i = 0; i < content.Length - 1; i++)
        {
            if (content[i] != '.') continue;

            var next = content[i + 1];
            if (next == ' ' || next == '\t')
            {
                return i;
            }
        }

        return -1;
    }

    private static string TrimRemainingProse(string remaining)
    {
        var start = 0;
        while (start < remaining.Length && (remaining[start] == ' ' || remaining[start] == '\t'))
        {
            start++;
        }

        var end = remaining.Length;
        while (end > start && IsTrailingTrim(remaining[end - 1]))
        {
            end--;
        }

        return remaining.Substring(start, end - start);
    }

    private static bool IsTrailingTrim(char ch) =>
        ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';

    private static string BuildAppendedPara(string indent, string lineEnding, string proseText)
    {
        var sb = new StringBuilder();
        sb.Append(indent).Append(DocCommentPrefix).Append("<para>").Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix).Append(proseText).Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix).Append("</para>").Append(lineEnding);
        return sb.ToString();
    }

    private static string BuildNewRemarksBlock(string indent, string lineEnding, string proseText)
    {
        var sb = new StringBuilder();
        sb.Append(indent).Append(DocCommentPrefix).Append("<remarks>").Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix).Append("<para>").Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix).Append(proseText).Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix).Append("</para>").Append(lineEnding);
        sb.Append(indent).Append(DocCommentPrefix).Append("</remarks>").Append(lineEnding);
        return sb.ToString();
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

    private static string ExtractLineIndent(SourceText text, int position)
    {
        var lineStart = FindLineStart(text, position);

        var end = lineStart;
        while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
        {
            end++;
        }

        return text.ToString(TextSpan.FromBounds(lineStart, end));
    }

    private static int FindLineStart(SourceText text, int position)
    {
        var lineStart = position;
        while (lineStart > 0 && text[lineStart - 1] != '\n' && text[lineStart - 1] != '\r')
        {
            lineStart--;
        }

        return lineStart;
    }

    private static int FindLineEndIncludingTerminator(SourceText text, int position)
    {
        var index = position;
        while (index < text.Length && text[index] != '\n' && text[index] != '\r')
        {
            index++;
        }

        if (index < text.Length && text[index] == '\r')
        {
            index++;
            if (index < text.Length && text[index] == '\n')
            {
                index++;
            }
        }
        else if (index < text.Length && text[index] == '\n')
        {
            index++;
        }

        return index;
    }
}
