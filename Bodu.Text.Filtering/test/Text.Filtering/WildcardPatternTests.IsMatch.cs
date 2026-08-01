// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPatternTests.IsMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for wildcard matching across every strategy, plus the exhaustive oracle-pinned Regression sweep.
/// </content>
public partial class WildcardPatternTests
{
    /// <summary>
    /// Gets match rows exercising every strategy, the case modes, character classes, and escapes.
    /// </summary>
    public static IEnumerable<object[]> MatchKats
    {
        get
        {
            yield return new object[] { new WildcardMatchKat("MatchAllEmpty", "*", string.Empty, false, true) };
            yield return new object[] { new WildcardMatchKat("MatchAllAny", "*", "anything", false, true) };
            yield return new object[] { new WildcardMatchKat("LiteralExact", "abc", "abc", false, true) };
            yield return new object[] { new WildcardMatchKat("LiteralCaseMiss", "abc", "ABC", false, false) };
            yield return new object[] { new WildcardMatchKat("LiteralCaseFold", "abc", "ABC", true, true) };
            yield return new object[] { new WildcardMatchKat("LiteralLongerText", "abc", "abcd", false, false) };
            yield return new object[] { new WildcardMatchKat("PrefixHit", "err*", "error", false, true) };
            yield return new object[] { new WildcardMatchKat("PrefixExact", "err*", "err", false, true) };
            yield return new object[] { new WildcardMatchKat("PrefixMiss", "err*", "warning", false, false) };
            yield return new object[] { new WildcardMatchKat("PrefixCaseFold", "ERR*", "error", true, true) };
            yield return new object[] { new WildcardMatchKat("SuffixHit", "*.log", "app.log", false, true) };
            yield return new object[] { new WildcardMatchKat("SuffixMiss", "*.log", "app.txt", false, false) };
            yield return new object[] { new WildcardMatchKat("PrefixAndSuffixHit", "app*log", "app-2024.log", false, true) };
            yield return new object[] { new WildcardMatchKat("PrefixAndSuffixNoOverlap", "abc*bcd", "abcd", false, false) };
            yield return new object[] { new WildcardMatchKat("ContainsHit", "*debug*", "app-debug-x", false, true) };
            yield return new object[] { new WildcardMatchKat("ContainsWholeText", "*debug*", "debug", false, true) };
            yield return new object[] { new WildcardMatchKat("ContainsMiss", "*debug*", "release", false, false) };
            yield return new object[] { new WildcardMatchKat("QuestionOne", "a?c", "abc", false, true) };
            yield return new object[] { new WildcardMatchKat("QuestionRequiresChar", "a?c", "ac", false, false) };
            yield return new object[] { new WildcardMatchKat("QuestionNotTwo", "a?c", "abbc", false, false) };
            yield return new object[] { new WildcardMatchKat("GeneralInterleaved", "a*b?c", "axxbyc", false, true) };
            yield return new object[] { new WildcardMatchKat("GeneralBacktrack", "a*bc", "abxbc", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassMember", "[abc]x", "bx", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassNonMember", "[abc]x", "dx", false, false) };
            yield return new object[] { new WildcardMatchKat("ClassRange", "[a-f]1", "d1", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassRangeMiss", "[a-f]1", "g1", false, false) };
            yield return new object[] { new WildcardMatchKat("ClassNegated", "[!abc]x", "dx", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassNegatedMiss", "[!abc]x", "ax", false, false) };
            yield return new object[] { new WildcardMatchKat("ClassCaretNegated", "[^a]x", "bx", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassCaseFold", "[a-z]", "Q", true, true) };
            yield return new object[] { new WildcardMatchKat("ClassCaseSensitive", "[a-z]", "Q", false, false) };
            yield return new object[] { new WildcardMatchKat("ClassNegatedCaseFold", "[!a]", "A", true, false) };
            yield return new object[] { new WildcardMatchKat("ClassLiteralDashLeading", "[-a]", "-", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassLiteralDashTrailing", "[a-]", "-", false, true) };
            yield return new object[] { new WildcardMatchKat("ClassEscapedBracket", @"[\]]", "]", false, true) };
            yield return new object[] { new WildcardMatchKat("EscapedStarLiteral", @"a\*b", "a*b", false, true) };
            yield return new object[] { new WildcardMatchKat("EscapedStarNotWild", @"a\*b", "axb", false, false) };
            yield return new object[] { new WildcardMatchKat("EscapedQuestionLiteral", @"\?", "?", false, true) };
            yield return new object[] { new WildcardMatchKat("EmptyPatternEmptyText", string.Empty, string.Empty, false, true) };
            yield return new object[] { new WildcardMatchKat("EmptyPatternNonEmptyText", string.Empty, "a", false, false) };
            yield return new object[] { new WildcardMatchKat("UnicodeCaseFold", "über*", "ÜBER-X", true, true) };
        }
    }

    /// <summary>
    /// Verifies that each pattern/value row produces the expected match outcome through the full compiled-strategy
    /// dispatch.
    /// </summary>
    /// <param name="kat">The match row.</param>
    [TestMethod]
    [DynamicData(nameof(MatchKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void IsMatch_WhenPatternAndTextAreKnown_ShouldProduceExpectedOutcome(WildcardMatchKat kat) =>
        Assert.AreEqual(kat.Expected, Match(kat.Pattern, kat.Text, kat.IgnoreCase));

    /// <summary>
    /// Verifies that the optimized matcher agrees with the naive recursive oracle for every pattern over
    /// <c>{a, b, *, ?}</c> up to length 5 against every text over <c>{a, b}</c> up to length 6 — an exhaustive pin of
    /// the two-pointer algorithm and the strategy classification against an independent implementation.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IsMatch_WhenSweptExhaustivelyAgainstOracle_ShouldAgreeForEveryPatternAndText()
    {
        var patternAlphabet = new[] { 'a', 'b', '*', '?' };
        var textAlphabet = new[] { 'a', 'b' };
        var texts = EnumerateStrings(textAlphabet, 6).ToArray();

        foreach (var pattern in EnumerateStrings(patternAlphabet, 5))
        {
            var program = WildcardPattern.Compile(pattern);
            foreach (var text in texts)
            {
                var expected = OracleMatch(pattern, text);
                var actual = program.IsMatch(text, StringComparison.Ordinal, false);

                if (expected != actual)
                    Assert.Fail($"Pattern '{pattern}' vs text '{text}': oracle {expected}, matcher {actual}.");
            }
        }
    }

    /// <summary>
    /// Enumerates every string over the given alphabet with length 0 through <paramref name="maxLength" />.
    /// </summary>
    /// <param name="alphabet">The characters to draw from.</param>
    /// <param name="maxLength">The inclusive maximum string length.</param>
    /// <returns>The enumerated strings, shortest first.</returns>
    private static IEnumerable<string> EnumerateStrings(char[] alphabet, int maxLength)
    {
        var current = new List<string> { string.Empty };
        yield return string.Empty;

        for (var length = 1; length <= maxLength; length++)
        {
            var next = new List<string>(current.Count * alphabet.Length);
            foreach (var prefix in current)
            {
                foreach (var c in alphabet)
                {
                    var candidate = prefix + c;
                    next.Add(candidate);
                    yield return candidate;
                }
            }

            current = next;
        }
    }
}
