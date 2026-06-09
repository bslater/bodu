// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides ergonomic, allocation-aware extension methods on <see cref="string" /> that fill gaps in the BCL —
/// positive-form null/empty predicates, fluent fallbacks, whitespace and line-ending normalisation, affix management,
/// substring windowing, case conversion, identifier sanitisation, and generic parsing.
/// </summary>
/// <remarks>
/// <para>
/// Members are split across partial files following the repository convention (see <c>CLAUDE.md</c>): one partial file
/// per method, named <c>StringExtensions.&lt;MethodName&gt;.cs</c>. Tests follow the same pattern under
/// <c>test/Extensions/StringExtensionsTests.&lt;MethodName&gt;.cs</c>.
/// </para>
/// <para>
/// Methods that conceptually deserve a place but are already short, idiomatic, or covered by the BCL are intentionally
/// omitted (for example <c>IsNullOrEmpty</c>, <c>Left</c> / <c>Right</c>, <c>Mid</c>, <c>ToLowerInvariant</c>). The
/// accompanying design document records the rationale per method.
/// </para>
/// <para>
/// Casing methods use <see cref="System.Globalization.CultureInfo.InvariantCulture" /> by default so output is
/// deterministic across machines; cultural variants are not provided to discourage accidental
/// <see cref="System.Globalization.CultureInfo.CurrentCulture" /> coupling. Comparison helpers default to
/// <see cref="System.StringComparison.Ordinal" /> for the same reason.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Sanitise then truncate user input for a log line.
/// string? raw = request?.Title;
/// string display = raw.TrimToNull()?.CollapseWhitespace().Truncate(80, "…") ?? "(untitled)";
///
/// // Build a slug for a URL.
/// string slug = "Hello, World! — café".ToSlug();   // "hello-world-cafe"
///
/// // Round-trip identifiers between conventions.
/// string pascal = "user_account_id".ToPascalCase(); // "UserAccountId"
/// string snake  = "UserAccountId".ToSnakeCase();    // "user_account_id"
///]]>
/// </code>
/// </example>
public static partial class StringExtensions
{
}
