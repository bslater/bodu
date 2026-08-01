// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPatternTests.ConformanceCorpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Filtering;

/// <content>
/// A conformance corpus collated from the edge cases and historical bug classes of the established glob engines this
/// library's design borrows from: the git/rsync <c>wildmatch</c> test corpus, glibc <c>fnmatch</c> character-class
/// parsing, minimatch/micromatch brace-expansion behavior, <c>globset</c>/ripgrep literal-optimization bug classes,
/// and the DOS-derived quirks of <c>FileSystemName.MatchesSimpleExpression</c>. Expected outcomes are stated for
/// <b>this library's documented grammar</b>; rows named <c>*Divergence*</c> pin behavior that intentionally differs
/// from a source engine, with the difference noted inline.
/// </content>
public partial class WildcardPatternTests
{
    /// <summary>
    /// Gets the conformance match rows, grouped by the source engine that motivated them.
    /// </summary>
    public static IEnumerable<object[]> ConformanceKats
    {
        get
        {
            // --- git/rsync wildmatch corpus (non-path cases; our globs have no path semantics) ---
            yield return new object[] { new WildcardMatchKat("Wildmatch_Literal", "foo", "foo", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_LiteralMismatch", "foo", "bar", false, false) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_TripleQuestion", "???", "foo", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_DoubleQuestionTooShort", "??", "foo", false, false) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_StarMatchesAll", "*", "foo", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_StarMatchesEmpty", "*", string.Empty, false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_QuestionRejectsEmpty", "?", string.Empty, false, false) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_PrefixStar", "f*", "foo", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_StarThenF", "*f", "foo", false, false) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_InnerStar", "foo*bar", "foobazbar", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_ClassRangeHit", "t[a-g]n", "ten", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_NegatedClassRangeMiss", "t[!a-g]n", "ten", false, false) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_NegatedClassRangeHit", "t[!a-g]n", "ton", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_BareCloseBracketIsLiteral", "]", "]", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_EscapedStarLiteral", @"foo\*", "foo*", false, true) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_EscapedStarNotWild", @"foo\*", "foobar", false, false) };
            yield return new object[] { new WildcardMatchKat("Wildmatch_EscapedBackslash", @"foo\\bar", @"foo\bar", false, true) };
            // wildmatch's pathname mode gives '**' cross-directory meaning; without path semantics it collapses to '*'.
            yield return new object[] { new WildcardMatchKat("Wildmatch_DoubleStarDivergence_CollapsesToStar", "**", "any/thing", false, true) };

            // --- glibc fnmatch / bash character-class parsing ---
            yield return new object[] { new WildcardMatchKat("Fnmatch_MultiRangeWithStrayDash_DashHit", "[a-c-e]", "-", false, true) };
            yield return new object[] { new WildcardMatchKat("Fnmatch_MultiRangeWithStrayDash_SecondRangeNotFormed", "[a-c-e]", "d", false, false) };
            yield return new object[] { new WildcardMatchKat("Fnmatch_MultiRangeWithStrayDash_TailMemberHit", "[a-c-e]", "e", false, true) };
            yield return new object[] { new WildcardMatchKat("Fnmatch_ReversedRangeMatchesNothing", "[d-b]", "c", false, false) };
            yield return new object[] { new WildcardMatchKat("Fnmatch_NegatedReversedRangeMatchesEverything", "[!d-b]", "c", false, true) };
            yield return new object[] { new WildcardMatchKat("Fnmatch_CaretNegationAlias", "[^ab]", "c", false, true) };
            // POSIX named classes are NOT supported: '[[:alpha:]]' parses as the class {'[',':','a','l','p','h'}
            // followed by a literal ']'. These rows pin the trap so the divergence is visible.
            yield return new object[] { new WildcardMatchKat("Fnmatch_PosixClassDivergence_ParsesAsMembers", "[[:alpha:]]", "a]", false, true) };
            yield return new object[] { new WildcardMatchKat("Fnmatch_PosixClassDivergence_DoesNotMatchAlpha", "[[:alpha:]]", "a", false, false) };

            // --- globset / ripgrep literal-optimization bug classes ---
            yield return new object[] { new WildcardMatchKat("Globset_LiteralIsNotSubstringMatch", "abc", "xabcx", false, false) };
            yield return new object[] { new WildcardMatchKat("Globset_SuffixMatchesWholeValue", "*.log", ".log", false, true) };
            yield return new object[] { new WildcardMatchKat("Globset_PrefixSuffixMustNotOverlap", "abc*bcd", "abcd", false, false) };
            yield return new object[] { new WildcardMatchKat("Globset_PrefixSuffixExactLength", "ab*cd", "abcd", false, true) };
            yield return new object[] { new WildcardMatchKat("Globset_CaseInsensitiveClassRange", "[a-z]*", "QUUX", true, true) };

            // --- DOS / .NET FileSystemName.MatchesSimpleExpression divergences (we do plain matching, no DOS magic) ---
            yield return new object[] { new WildcardMatchKat("DosDivergence_StarDotStarNeedsADot", "*.*", "file", false, false) };
            yield return new object[] { new WildcardMatchKat("DosDivergence_StarDotStarWithDot", "*.*", "file.txt", false, true) };
            yield return new object[] { new WildcardMatchKat("DosDivergence_TrailingDotIsLiteral", "*.", "name", false, false) };
            yield return new object[] { new WildcardMatchKat("DosDivergence_TrailingDotMatchesDot", "*.", "name.", false, true) };
        }
    }

    /// <summary>
    /// Verifies every collated known-library row against the compiled-strategy dispatch.
    /// </summary>
    /// <param name="kat">The conformance row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ConformanceKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void IsMatch_WhenKnownLibraryConformanceRow_ShouldProduceExpectedOutcome(WildcardMatchKat kat) =>
        Assert.AreEqual(kat.Expected, Match(kat.Pattern, kat.Text, kat.IgnoreCase));

    /// <summary>
    /// Gets rows that are legal in a source engine but are documented build-time errors in this grammar — POSIX glob
    /// admits <c>]</c> as the first class member (<c>[]]</c>), while this library requires the escape <c>[\]]</c>.
    /// </summary>
    public static IEnumerable<object[]> ConformanceDivergenceInvalidKats
    {
        get
        {
            yield return new object[] { new InvalidKat<string>("PosixDivergence_LeadingCloseBracketMember", "a[]]b", typeof(ArgumentException), "pattern") };
            yield return new object[] { new InvalidKat<string>("PosixDivergence_NegatedLeadingCloseBracketMember", "[!]a]", typeof(ArgumentException), "pattern") };
        }
    }

    /// <summary>
    /// Verifies that source-engine syntax this grammar deliberately rejects fails at build time with
    /// <see cref="ArgumentException" /> rather than silently matching differently.
    /// </summary>
    /// <param name="kat">The divergence row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ConformanceDivergenceInvalidKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Compile_WhenSourceEngineSyntaxIsRejectedByThisGrammar_ShouldThrowArgumentException(InvalidKat<string> kat)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = WildcardPattern.Compile(kat.Input);
        });

        Assert.AreEqual(kat.ParamName, ex.ParamName);
    }

    /// <summary>
    /// Gets brace-expansion rows derived from minimatch/micromatch behavior, including the divergences this library
    /// documents (a comma-free group still expands; an empty group collapses).
    /// </summary>
    public static IEnumerable<object[]> ConformanceBraceKats
    {
        get
        {
            // minimatch leaves '{abc}' and '{}' literal; this grammar always expands (documented divergence).
            yield return new object[] { new ValidKat<string, string[]>("MinimatchDivergence_SingleAlternativeExpands", "a{bc}d", ["abcd"]) };
            yield return new object[] { new ValidKat<string, string[]>("MinimatchDivergence_EmptyGroupCollapses", "a{}b", ["ab"]) };
            yield return new object[] { new ValidKat<string, string[]>("Minimatch_TwoEmptyAlternatives", "x{,}y", ["xy", "xy"]) };
            yield return new object[] { new ValidKat<string, string[]>("Minimatch_CrossProduct", "{a,b}{1,2}", ["a1", "a2", "b1", "b2"]) };
            yield return new object[] { new ValidKat<string, string[]>("Minimatch_NestedGroups", "{a,{b,c}}d", ["ad", "bd", "cd"]) };
        }
    }

    /// <summary>
    /// Verifies the minimatch-derived brace-expansion rows.
    /// </summary>
    /// <param name="kat">The expansion row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ConformanceBraceKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ExpandBraces_WhenKnownLibraryConformanceRow_ShouldProduceExpectedAlternatives(ValidKat<string, string[]> kat)
    {
        var actual = WildcardPattern.ExpandBraces(kat.Input);

        CollectionAssert.AreEqual(kat.Expected, actual.ToArray());
    }

    /// <summary>
    /// Verifies that the classic exponential-backtracking input — many <c>*a</c> segments against a long run of
    /// <c>a</c> characters, the input that hangs naive recursive matchers and motivated wildmatch's iterative
    /// algorithm — completes correctly in polynomial time on the two-pointer matcher.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IsMatch_WhenPathologicalBacktrackingInput_ShouldCompleteWithoutExponentialBlowup()
    {
        var pattern = string.Concat(Enumerable.Repeat("*a", 20)) + "*b";
        var almost = new string('a', 500);

        // A naive exponential matcher explores ~2^500 paths here; the two-pointer matcher is O(n·m).
        Assert.IsFalse(Match(pattern, almost, false));
        Assert.IsTrue(Match(pattern, almost + "b", false));
    }
}
