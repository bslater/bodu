// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPatternTests.ExpandBraces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="WildcardPattern.ExpandBraces" />.
/// </content>
public partial class WildcardPatternTests
{
    /// <summary>
    /// Gets expansion rows whose input is legal: pattern text mapped to the expected alternatives in order.
    /// </summary>
    public static IEnumerable<object[]> ExpandValidKats
    {
        get
        {
            yield return new object[] { new ValidKat<string, string[]>("NoBraces", "abc*", ["abc*"]) };
            yield return new object[] { new ValidKat<string, string[]>("TwoAlternatives", "{error,warn}*", ["error*", "warn*"]) };
            yield return new object[] { new ValidKat<string, string[]>("ThreeAlternatives", "a{1,2,3}b", ["a1b", "a2b", "a3b"]) };
            yield return new object[] { new ValidKat<string, string[]>("SingleAlternative", "{abc}", ["abc"]) };
            yield return new object[] { new ValidKat<string, string[]>("EmptyAlternative", "a{b,}", ["ab", "a"]) };
            yield return new object[] { new ValidKat<string, string[]>("Nested", "{a,{b,c}}", ["a", "b", "c"]) };
            yield return new object[] { new ValidKat<string, string[]>("TwoGroups", "{a,b}{1,2}", ["a1", "a2", "b1", "b2"]) };
            yield return new object[] { new ValidKat<string, string[]>("EscapedBraceLiteral", @"\{a,b\}", [@"\{a,b\}"]) };
            yield return new object[] { new ValidKat<string, string[]>("BraceInsideClass", "[{]x", ["[{]x"]) };
            yield return new object[] { new ValidKat<string, string[]>("UnmatchedCloseIsLiteral", "a}b", ["a}b"]) };
            yield return new object[] { new ValidKat<string, string[]>("NestedSuffix", "x{a,b{c,d}}y", ["xay", "xbcy", "xbdy"]) };
        }
    }

    /// <summary>
    /// Gets expansion rows whose input violates the brace grammar or its caps.
    /// </summary>
    public static IEnumerable<object[]> ExpandInvalidKats
    {
        get
        {
            yield return new object[] { new InvalidKat<string>("Unterminated", "{a,b", typeof(ArgumentException), "pattern") };
            yield return new object[] { new InvalidKat<string>("UnterminatedNested", "{a,{b}", typeof(ArgumentException), "pattern") };
            yield return new object[]
            {
                new InvalidKat<string>(
                    "NestingTooDeep",
                    new string('{', WildcardPattern.MaxBraceNestingDepth + 1) + "a" + new string('}', WildcardPattern.MaxBraceNestingDepth + 1),
                    typeof(ArgumentException),
                    "pattern"),
            };
            yield return new object[]
            {
                new InvalidKat<string>(
                    "ExpansionTooLarge",
                    string.Concat(Enumerable.Repeat("{a,b}", 14)),
                    typeof(ArgumentException),
                    "pattern"),
            };
        }
    }

    /// <summary>
    /// Verifies that a legal pattern expands into exactly the expected alternatives, in left-to-right order.
    /// </summary>
    /// <param name="kat">The expansion row.</param>
    [TestMethod]
    [DynamicData(nameof(ExpandValidKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ExpandBraces_WhenPatternIsValid_ShouldProduceExpectedAlternatives(ValidKat<string, string[]> kat)
    {
        var actual = WildcardPattern.ExpandBraces(kat.Input);

        CollectionAssert.AreEqual(kat.Expected, actual.ToArray());
    }

    /// <summary>
    /// Verifies that a pattern violating the brace grammar or its caps throws <see cref="ArgumentException" /> with
    /// the expected parameter name.
    /// </summary>
    /// <param name="kat">The invalid-expansion row.</param>
    [TestMethod]
    [DynamicData(nameof(ExpandInvalidKats), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ExpandBraces_WhenPatternIsInvalid_ShouldThrowArgumentException(InvalidKat<string> kat)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = WildcardPattern.ExpandBraces(kat.Input);
        });

        Assert.AreEqual(kat.ParamName, ex.ParamName);
    }
}
