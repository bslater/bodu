// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTypeParamRequiresShortContentCodeFixProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// <summary>
    /// The code-action title for the fix that relocates trailing <c>&lt;typeparam&gt;</c> prose into <c>&lt;remarks&gt;</c>.
    /// </summary>
    private const string MoveProseTitle = "Move trailing prose to <remarks>";

    /// <summary>
    /// The equivalence key identifying the move-prose-to-remarks fix for Fix All batching.
    /// </summary>
    private const string MoveProseEquivalenceKey = "BoduMoveTypeParamProseToRemarks";

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

    /// <summary>
    /// Resolves the <c>&lt;typeparam&gt;</c> element associated with the node located at a diagnostic span.
    /// </summary>
    /// <param name="node">The node found at the diagnostic location, or <see langword="null" />.</param>
    /// <returns>The owning <c>&lt;typeparam&gt;</c> element, or <see langword="null" /> when none is found.</returns>
    private static XmlElementSyntax? ResolveTypeParamElement(SyntaxNode? node)
    {
        return node switch
        {
            XmlElementSyntax direct => direct,
            XmlElementStartTagSyntax start when start.Parent is XmlElementSyntax parent => parent,
            _ => node?.FirstAncestorOrSelf<XmlElementSyntax>(),
        };
    }

    /// <summary>
    /// Shortens a single <c>&lt;typeparam&gt;</c> to its first sentence and relocates its trailing prose into the
    /// containing documentation comment's <c>&lt;remarks&gt;</c> block.
    /// </summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="typeParam">The <c>&lt;typeparam&gt;</c> element whose trailing prose is relocated.</param>
    /// <param name="cancellationToken">A token that propagates notification that the operation should be canceled.</param>
    /// <returns>The updated document, or the original document when there is no prose to relocate.</returns>
    private static async Task<Document> MoveProseToRemarksAsync(Document document, XmlElementSyntax typeParam, CancellationToken cancellationToken)
    {
        SourceText text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        DocumentationCommentTriviaSyntax? docComment = typeParam.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docComment is null) return document;

        var lineEnding = DocCommentSource.ResolveLineEnding(text);
        IReadOnlyList<TextChange> changes = BuildChangesForDocComment(
            text, docComment, new[] { typeParam }, lineEnding);
        if (changes.Count == 0) return document;

        SourceText updated = text.WithChanges(changes);
        return document.WithText(updated);
    }

    /// <summary>
    /// Computes the ordered, non-overlapping text changes that relocate the trailing prose of every supplied
    /// <c>&lt;typeparam&gt;</c> in a single documentation comment.
    /// </summary>
    /// <param name="text">The source text of the document being fixed.</param>
    /// <param name="docComment">The documentation comment that owns the type parameters.</param>
    /// <param name="typeParams">The overflowing <c>&lt;typeparam&gt;</c> elements to shorten and relocate.</param>
    /// <param name="lineEnding">The line ending to emit between synthesized lines.</param>
    /// <returns>
    /// The ordered list of text changes: each element shortened to its first sentence in place, plus the
    /// collected paragraphs appended to the comment's existing <c>&lt;remarks&gt;</c> block — or, when none
    /// exists, wrapped in one fresh <c>&lt;remarks&gt;</c> block inserted after the last <c>&lt;typeparam&gt;</c> line.
    /// </returns>
    private static List<TextChange> BuildChangesForDocComment(
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

        var indent = DocCommentSource.ExtractIndent(text, ordered[0].SpanStart);
        var prefix = DocCommentSource.DetectPrefix(text, ordered[0].SpanStart);

        XmlElementSyntax? remarks = FindRemarksElement(docComment);
        if (remarks is not null)
        {
            // Append the new paragraphs as the final paragraphs inside the existing <remarks> body. The
            // insertion point is the start of the line carrying the </remarks> end tag, which re-uses the
            // indent + prefix already in the source.
            var endTagLineStart = DocCommentSource.FindLineStart(text, remarks.EndTag.SpanStart);
            var insertion = BuildParagraphs(indent, prefix, lineEnding, paragraphs);
            changes.Add(new TextChange(new TextSpan(endTagLineStart, 0), insertion));
        }
        else
        {
            // No <remarks> present: insert a single synthesized <remarks> block immediately after the line
            // carrying the LAST <typeparam>'s end tag, so the canonical summary → typeparam(s) → remarks
            // ordering is preserved and every relocated paragraph lands in one shared block.
            XmlElementSyntax anchor = FindLastTypeParam(docComment) ?? ordered[ordered.Count - 1];
            var afterAnchorLineEnd = DocCommentSource.FindLineEndIncludingTerminator(text, anchor.Span.End);
            var insertion = BuildRemarksBlock(indent, prefix, lineEnding, paragraphs);
            changes.Add(new TextChange(new TextSpan(afterAnchorLineEnd, 0), insertion));
        }

        return changes;
    }

    /// <summary>
    /// Locates the unprefixed <c>&lt;remarks&gt;</c> element directly within the documentation comment, if present.
    /// </summary>
    /// <param name="docComment">The documentation comment to search.</param>
    /// <returns>The <c>&lt;remarks&gt;</c> element, or <see langword="null" /> when the comment has none.</returns>
    private static XmlElementSyntax? FindRemarksElement(DocumentationCommentTriviaSyntax docComment)
    {
        return docComment.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e =>
                e.StartTag.Name.Prefix is null
                && string.Equals(e.StartTag.Name.LocalName.ValueText, "remarks", System.StringComparison.Ordinal));
    }

    /// <summary>
    /// Locates the last unprefixed <c>&lt;typeparam&gt;</c> element within the documentation comment, used as the
    /// insertion anchor for a synthesized <c>&lt;remarks&gt;</c> block.
    /// </summary>
    /// <param name="docComment">The documentation comment to search.</param>
    /// <returns>The last <c>&lt;typeparam&gt;</c> element, or <see langword="null" /> when the comment has none.</returns>
    private static XmlElementSyntax? FindLastTypeParam(DocumentationCommentTriviaSyntax docComment)
    {
        return docComment.Content
            .OfType<XmlElementSyntax>()
            .LastOrDefault(e =>
                e.StartTag.Name.Prefix is null
                && string.Equals(e.StartTag.Name.LocalName.ValueText, "typeparam", System.StringComparison.Ordinal));
    }

    /// <summary>
    /// Concatenates the textual representation of every child node in an element's content into a single string.
    /// </summary>
    /// <param name="element">The element whose content is collected.</param>
    /// <returns>The concatenated content text of the element.</returns>
    private static string GetContentText(XmlElementSyntax element)
    {
        var sb = new StringBuilder();
        foreach (XmlNodeSyntax node in element.Content)
        {
            sb.Append(node.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Trims leading spaces and tabs and trailing whitespace from the prose that follows the first sentence.
    /// </summary>
    /// <param name="remaining">The prose remaining after the first sentence boundary.</param>
    /// <returns>The trimmed prose.</returns>
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

    /// <summary>
    /// Determines whether a character is treated as trailing whitespace when trimming relocated prose.
    /// </summary>
    /// <param name="ch">The character to test.</param>
    /// <returns><see langword="true" /> when the character is a space, tab, carriage return, or line feed; otherwise <see langword="false" />.</returns>
    private static bool IsTrailingTrim(char ch) =>
        ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';

    /// <summary>
    /// Builds the concatenated <c>&lt;para&gt;</c> blocks for prose appended to an existing <c>&lt;remarks&gt;</c> body.
    /// </summary>
    /// <param name="indent">The leading indentation applied to each emitted line.</param>
    /// <param name="prefix">The documentation-comment prefix applied to each emitted line.</param>
    /// <param name="lineEnding">The line ending to emit between lines.</param>
    /// <param name="paragraphs">The relocated prose paragraphs.</param>
    /// <returns>The synthesized <c>&lt;para&gt;</c> blocks as a single string.</returns>
    private static string BuildParagraphs(string indent, string prefix, string lineEnding, IReadOnlyList<string> paragraphs)
    {
        var sb = new StringBuilder();
        foreach (var prose in paragraphs)
        {
            AppendParagraph(sb, indent, prefix, lineEnding, prose);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a fresh <c>&lt;remarks&gt;</c> block wrapping the relocated prose as <c>&lt;para&gt;</c> paragraphs.
    /// </summary>
    /// <param name="indent">The leading indentation applied to each emitted line.</param>
    /// <param name="prefix">The documentation-comment prefix applied to each emitted line.</param>
    /// <param name="lineEnding">The line ending to emit between lines.</param>
    /// <param name="paragraphs">The relocated prose paragraphs.</param>
    /// <returns>The synthesized <c>&lt;remarks&gt;</c> block as a single string.</returns>
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

    /// <summary>
    /// Appends a single <c>&lt;para&gt;</c> block carrying one prose paragraph to the supplied builder.
    /// </summary>
    /// <param name="sb">The builder receiving the emitted lines.</param>
    /// <param name="indent">The leading indentation applied to each emitted line.</param>
    /// <param name="prefix">The documentation-comment prefix applied to each emitted line.</param>
    /// <param name="lineEnding">The line ending to emit between lines.</param>
    /// <param name="prose">The prose content of the paragraph.</param>
    private static void AppendParagraph(StringBuilder sb, string indent, string prefix, string lineEnding, string prose)
    {
        sb.Append(indent).Append(prefix).Append("<para>").Append(lineEnding);
        sb.Append(indent).Append(prefix).Append(prose).Append(lineEnding);
        sb.Append(indent).Append(prefix).Append("</para>").Append(lineEnding);
    }

    /// <summary>
    /// Provides a <see cref="DocumentBasedFixAllProvider" /> that applies <c>BODU1406</c> fixes one
    /// documentation comment at a time, coalescing every relocated paragraph into a single
    /// <c>&lt;remarks&gt;</c> block so concurrently-computed fixes never produce overlapping or duplicate
    /// insertions.
    /// </summary>
    private sealed class TypeParamFixAllProvider : DocumentBasedFixAllProvider
    {
        /// <summary>
        /// Gets the shared singleton instance of the Fix All provider.
        /// </summary>
        public static TypeParamFixAllProvider Instance { get; } = new TypeParamFixAllProvider();

        /// <summary>
        /// Applies the <c>BODU1406</c> fix across all diagnostics in a single document, grouping them by their
        /// containing documentation comment so each comment receives exactly one coherent transformation.
        /// </summary>
        /// <param name="fixAllContext">The Fix All context driving the operation.</param>
        /// <param name="document">The document to transform.</param>
        /// <param name="diagnostics">The diagnostics within the document to fix.</param>
        /// <returns>The transformed document, or the original document when no changes apply.</returns>
        protected override async Task<Document?> FixAllAsync(
            FixAllContext fixAllContext,
            Document document,
            ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsDefaultOrEmpty) return document;

            SyntaxNode? root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            if (root is null) return document;

            SourceText text = await document.GetTextAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var lineEnding = DocCommentSource.ResolveLineEnding(text);

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
