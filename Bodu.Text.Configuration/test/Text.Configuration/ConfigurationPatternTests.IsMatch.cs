// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.IsMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that a brace group without a comma is not a choice: <c>{single}.b</c> matches only the name written
    /// with its braces, not <c>single.b</c> or <c>.b</c> (EditorConfig core-tests <c>braces_single_choice</c> and
    /// <c>braces_single_choice_negative</c>), and <c>{foo}.go</c> does not match <c>foo.go</c>.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenBraceGroupHasNoComma_ShouldMatchItLiterally()
    {
        var single = ConfigurationPattern.Compile("{single}.b");
        var foo = ConfigurationPattern.Compile("{foo}.go");

        Assert.IsTrue(single.IsMatch("{single}.b"));
        Assert.IsFalse(single.IsMatch("single.b"));
        Assert.IsFalse(single.IsMatch(".b"));
        Assert.IsFalse(foo.IsMatch("foo.go"));
    }

    /// <summary>
    /// Verifies that empty braces are literal text: <c>{}.c</c> matches only <c>{}.c</c>, not <c>.c</c>
    /// (EditorConfig core-tests <c>braces_empty_choice</c> and <c>braces_empty_choice_negative</c>).
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenBracesAreEmpty_ShouldMatchThemLiterally()
    {
        var pattern = ConfigurationPattern.Compile("{}.c");

        Assert.IsTrue(pattern.IsMatch("{}.c"));
        Assert.IsFalse(pattern.IsMatch(".c"));
    }

    /// <summary>
    /// Verifies that a range whose bounds are not integers is literal text: <c>{aardvark..antelope}</c> matches only
    /// the name written with its braces, and none of the words around it (EditorConfig core-tests
    /// <c>braces_alpha_range1</c> to <c>braces_alpha_range6</c>).
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenRangeBoundsAreNotIntegers_ShouldMatchItLiterally()
    {
        var pattern = ConfigurationPattern.Compile("{aardvark..antelope}");

        Assert.IsTrue(pattern.IsMatch("{aardvark..antelope}"));
        Assert.IsFalse(pattern.IsMatch("aardvark..antelope"));
        Assert.IsFalse(pattern.IsMatch("a"));
        Assert.IsFalse(pattern.IsMatch("aardvark"));
        Assert.IsFalse(pattern.IsMatch("agreement"));
        Assert.IsFalse(pattern.IsMatch("antelope"));
    }

    /// <summary>
    /// Verifies that a range with only one integer bound is literal text, as is a group holding only that bound.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenRangeHasOneBound_ShouldMatchItLiterally()
    {
        var pattern = ConfigurationPattern.Compile("file{1..}.txt");

        Assert.IsTrue(pattern.IsMatch("file{1..}.txt"));
        Assert.IsFalse(pattern.IsMatch("file1.txt"));
        Assert.IsFalse(pattern.IsMatch("file1...txt"));
    }

    /// <summary>
    /// Verifies that a nested group whose outer level holds a comma is still a choice, each alternative expanding,
    /// and that it does not match its own text (EditorConfig core-tests <c>braces_nested_start1</c> to
    /// <c>braces_nested_start5</c>).
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenNestedGroupHasTopLevelComma_ShouldMatchEachAlternative()
    {
        var pattern = ConfigurationPattern.Compile("{{a,b},c}.k");

        Assert.IsTrue(pattern.IsMatch("a.k"));
        Assert.IsTrue(pattern.IsMatch("b.k"));
        Assert.IsTrue(pattern.IsMatch("c.k"));
        Assert.IsFalse(pattern.IsMatch("{{a,b},c}.k"));
        Assert.IsFalse(pattern.IsMatch("{a,b}.k"));
    }

    /// <summary>
    /// Verifies that a group without a comma nested in a choice is literal inside that choice: <c>{a,{b}}.x</c>
    /// matches <c>a.x</c> and <c>{b}.x</c>, but not <c>b.x</c>.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenChoiceHoldsGroupWithoutComma_ShouldMatchThatGroupLiterally()
    {
        var pattern = ConfigurationPattern.Compile("{a,{b}}.x");

        Assert.IsTrue(pattern.IsMatch("a.x"));
        Assert.IsTrue(pattern.IsMatch("{b}.x"));
        Assert.IsFalse(pattern.IsMatch("b.x"));
    }

    /// <summary>
    /// Verifies that a pattern starting with <c>/</c> matches a relative path as if the path began with <c>/</c>, so
    /// <c>/test1/file.{java,js,rb}</c> matches <c>test1/file.java</c> (EditorConfig core-test
    /// <c>leading_slash_relevance</c>).
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternStartsWithSlashAndPathIsRelative_ShouldMatchAsIfRooted()
    {
        var pattern = ConfigurationPattern.Compile("/test1/file.{java,js,rb}");

        Assert.IsTrue(pattern.IsMatch("test1/file.java"));
        Assert.IsTrue(pattern.IsMatch("test1/file.rb"));
    }

    /// <summary>
    /// Verifies that a pattern starting with <c>/</c> still matches a path that itself starts with <c>/</c>.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternStartsWithSlashAndPathIsRooted_ShouldMatch()
    {
        var pattern = ConfigurationPattern.Compile("/test1/file.{java,js,rb}");

        Assert.IsTrue(pattern.IsMatch("/test1/file.java"));
    }

    /// <summary>
    /// Verifies that a pattern starting with <c>/</c> stays anchored at the root, so a path whose matching tail sits in
    /// another directory does not match, whether or not the path starts with <c>/</c>.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPatternStartsWithSlashAndPathIsInAnotherDirectory_ShouldNotMatch()
    {
        var pattern = ConfigurationPattern.Compile("/test1/file.{java,js,rb}");

        Assert.IsFalse(pattern.IsMatch("other/test1/file.java"));
        Assert.IsFalse(pattern.IsMatch("/other/test1/file.java"));
    }
}
