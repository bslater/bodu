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
/// <c>&lt;remarks&gt;</c> exists, a fresh block is synthesized immediately after the last
/// <c>&lt;typeparam&gt;</c> line, matching the canonical Bodu doc-tag ordering of summary → typeparam →
/// remarks.
/// </para>
/// <para>
/// Fix All is served by a dedicated <see cref="DocumentBasedFixAllProvider" /> that groups all diagnostics by
/// their containing documentation comment and emits a single coherent transformation per comment — one
/// <c>&lt;remarks&gt;</c> block carrying every relocated paragraph in source order. This avoids the
/// overlapping / duplicate <c>&lt;remarks&gt;</c> insertions that a batch merge of independently-computed
/// fixes would otherwise produce when a generic type declares several overflowing type parameters.
/// </para>
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocTypeParamRequiresShortContentCodeFixProvider))]
[Shared]
public sealed class XmlDocTypeParamRequiresShortContentCodeFixProvider : CodeFixProvider
{
    private const string MoveProseTitle = "Move trailing prose to <remarks>";

    private const string MoveProseEquivalenceKey = "BoduMoveTypeParamProseToRemarks";

    private const string DefaultDocCommentPrefix = "/// ";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticIds.XmlDocTypeParamRequiresShortContent);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => TypeParamFixAllProvider.Instance;

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

        DocumentationCommentTriviaSyntax? docComment = typeParam.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docComment is null) return document;

        var lineEnding = DetectLineEnding(text);
        IReadOnlyList<TextChange> changes = BuildChangesForDocComment(
            text, docComment, new[] { typeParam }, lineEnding);
        if (changes.Count == 0) return document;

        SourceText updated = text.WithChanges(changes);
        return document.WithText(updated);
    }

    // Computes the ordered, non-overlapping text changes that relocate the trailing prose of every supplied
    // <typeparam> in a single documentation comment: each element is shortened to its first sentence in place,
    // and the collected paragraphs are appended to the comment's existing <remarks> block — or, when none
    // exists, wrapped in one fresh <remarks> block inserted after the last <typeparam> line.
    private static IReadOnlyList<TextChange> BuildChangesForDocComment(
        SourceText text,
        DocumentationCommentTriviaSyntax docComment,
        IReadOnlyList<XmlElementSyntax> typeParams,
        string lineEnding)
    {
        var ordered = typeParams.OrderBy(e => e.SpanStart).ToList();
        var changes = new List<TextChange>();
        var paragraphs = new List<string>();

        foreach (XmlElementSyntax typeParam in ordered)
        {
            // Operate on the canonical single-line content so the split point matches the analyzer's measure and
            // the rewritten element (and relocated paragraph) are emitted as clean single lines.
            var content = XmlDocProseText.Canonicalize(GetContentText(typeParam));
            var splitIndex = XmlDocSentenceBoundary.FindFirstSentenceBoundary(content);
            if (splitIndex < 0) continue;

            var firstSentence = content.Substring(0, splitIndex + 1);
            var remaining = TrimRemainingProse(content.Substring(splitIndex + 1));
            if (remaining.Length == 0) continue;

            var startTagText = XmlDocProseText.Canonicalize(typeParam.StartTag.ToString());
            var endTagText = XmlDocProseText.Canonicalize(typeParam.EndTag.ToString());
            var newTypeParamText = startTagText + firstSentence + endTagText;
            changes.Add(new TextChange(typeParam.Span, newTypeParamText));
            paragraphs.Add(remaining);
        }

        if (paragraphs.Count == 0) return changes;

        var indent = ExtractLineIndent(text, ordered[0].SpanStart);
        var prefix = DetectDocCommentPrefix(text, ordered[0].SpanStart);

        XmlElementSyntax? remarks = FindRemarksElement(docComment);
        if (remarks is not null)
        {
            // Append the new paragraphs as the final paragraphs inside the existing <remarks> body. The
            // insertion point is the start of the line carrying the </remarks> end tag, which re-uses the
            // indent + prefix already in the source.
            var endTagLineStart = FindLineStart(text, remarks.EndTag.SpanStart);
            var insertion = BuildParagraphs(indent, prefix, lineEnding, paragraphs);
            changes.Add(new TextChange(new TextSpan(endTagLineStart, 0), insertion));
        }
        else
        {
            // No <remarks> present: insert a single synthesized <remarks> block immediately after the line
            // carrying the LAST <typeparam>'s end tag, so the canonical summary → typeparam(s) → remarks
            // ordering is preserved and every relocated paragraph lands in one shared block.
            XmlElementSyntax anchor = FindLastTypeParam(docComment) ?? ordered[ordered.Count - 1];
            var afterAnchorLineEnd = FindLineEndIncludingTerminator(text, anchor.Span.End);
            var insertion = BuildRemarksBlock(indent, prefix, lineEnding, paragraphs);
            changes.Add(new TextChange(new TextSpan(afterAnchorLineEnd, 0), insertion));
        }

        return changes;
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

    private static string BuildParagraphs(string indent, string prefix, string lineEnding, IReadOnlyList<string> paragraphs)
    {
        var sb = new StringBuilder();
        foreach (var prose in paragraphs)
        {
            AppendParagraph(sb, indent, prefix, lineEnding, prose);
        }

        return sb.ToString();
    }

    private static string BuildRemarksBlock(string indent, string prefix, string lineEnding, IReadOnlyList<string> paragraphs)
    {
        var sb = new StringBuilder();
        sb.Append(indent).Append(prefix).Append("<remarks>").Append(lineEnding);
        foreach (var prose in paragraphs)
        {
            AppendParagraph(sb, indent, prefix, lineEnding, prose);
        }

        sb.Append(indent).Append(prefix).Append("</remarks>").Append(lineEnding);
        return sb.ToString();
    }

    private static void AppendParagraph(StringBuilder sb, string indent, string prefix, string lineEnding, string prose)
    {
        sb.Append(indent).Append(prefix).Append("<para>").Append(lineEnding);
        sb.Append(indent).Append(prefix).Append(prose).Append(lineEnding);
        sb.Append(indent).Append(prefix).Append("</para>").Append(lineEnding);
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

    // Recovers the documentation-comment prefix actually used on the line at `position` — the "///" run plus a
    // single following space when present — so synthesized blocks match the source style instead of assuming a
    // hardcoded "/// ". Falls back to the canonical Bodu prefix when no "///" is found.
    private static string DetectDocCommentPrefix(SourceText text, int position)
    {
        var lineStart = FindLineStart(text, position);
        var i = lineStart;
        while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) i++;

        if (i + 2 < text.Length && text[i] == '/' && text[i + 1] == '/' && text[i + 2] == '/')
        {
            var end = i + 3;
            if (end < text.Length && text[end] == ' ') end++;
            return text.ToString(TextSpan.FromBounds(i, end));
        }

        return DefaultDocCommentPrefix;
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

    /// <summary>
    /// Provides a <see cref="DocumentBasedFixAllProvider" /> that applies <c>BODU1406</c> fixes one
    /// documentation comment at a time, coalescing every relocated paragraph into a single
    /// <c>&lt;remarks&gt;</c> block so concurrently-computed fixes never produce overlapping or duplicate
    /// insertions.
    /// </summary>
    private sealed class TypeParamFixAllProvider : DocumentBasedFixAllProvider
    {
        public static TypeParamFixAllProvider Instance { get; } = new TypeParamFixAllProvider();

        protected override async Task<Document?> FixAllAsync(
            FixAllContext fixAllContext,
            Document document,
            ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsDefaultOrEmpty) return document;

            SyntaxNode? root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            if (root is null) return document;

            SourceText text = await document.GetTextAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var lineEnding = DetectLineEnding(text);

            // Resolve each diagnostic to its <typeparam> element and group by the containing documentation
            // comment, so each comment receives exactly one coherent transformation.
            var groups = new Dictionary<DocumentationCommentTriviaSyntax, List<XmlElementSyntax>>();
            foreach (Diagnostic diagnostic in diagnostics)
            {
                SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
                XmlElementSyntax? element = ResolveTypeParamElement(node);
                DocumentationCommentTriviaSyntax? docComment = element?.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
                if (element is null || docComment is null) continue;

                if (!groups.TryGetValue(docComment, out List<XmlElementSyntax>? elements))
                {
                    elements = new List<XmlElementSyntax>();
                    groups.Add(docComment, elements);
                }

                elements.Add(element);
            }

            if (groups.Count == 0) return document;

            var allChanges = new List<TextChange>();
            foreach (KeyValuePair<DocumentationCommentTriviaSyntax, List<XmlElementSyntax>> group in groups)
            {
                allChanges.AddRange(BuildChangesForDocComment(text, group.Key, group.Value, lineEnding));
            }

            if (allChanges.Count == 0) return document;

            SourceText updated = text.WithChanges(allChanges);
            return document.WithText(updated);
        }
    }
}
