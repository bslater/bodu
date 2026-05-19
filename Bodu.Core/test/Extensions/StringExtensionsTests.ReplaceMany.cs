// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ReplaceMany.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceMany(string, IReadOnlyDictionary{string, string})" />
    /// applies every replacement in dictionary enumeration order.
    /// </summary>
    [TestMethod]
    public void ReplaceMany_WhenMultipleReplacementsSupplied_ShouldApplyEachOne()
    {
        Dictionary<string, string> replacements = new()
        {
            ["{name}"] = "Alice",
            ["{role}"] = "admin",
        };

        string actual = "Hi {name}, your role is {role}.".ReplaceMany(replacements);

        Assert.AreEqual("Hi Alice, your role is admin.", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceMany(string, IReadOnlyDictionary{string, string})" />
    /// returns the original string when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void ReplaceMany_WhenDictionaryIsEmpty_ShouldReturnSameInstance()
    {
        string value = "hello";

        string actual = value.ReplaceMany(new Dictionary<string, string>());

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that replacements are applied sequentially so that the output of an earlier replacement is
    /// visible to a later one.
    /// </summary>
    [TestMethod]
    public void ReplaceMany_WhenLaterReplacementMatchesEarlierOutput_ShouldCascade()
    {
        Dictionary<string, string> replacements = new()
        {
            ["a"] = "b",
            ["b"] = "c",
        };

        string actual = "a".ReplaceMany(replacements);

        Assert.AreEqual("c", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceMany(string, IReadOnlyDictionary{string, string})" />
    /// throws <see cref="ArgumentException" /> when any key is empty.
    /// </summary>
    [TestMethod]
    public void ReplaceMany_WhenAnyKeyIsEmpty_ShouldThrowArgumentException()
    {
        Dictionary<string, string> replacements = new() { [""] = "X" };

        Assert.ThrowsExactly<ArgumentException>(() => _ = "hello".ReplaceMany(replacements));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceMany(string, IReadOnlyDictionary{string, string})" />
    /// throws <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void ReplaceMany_WhenAnyArgumentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ReplaceMany(null!, new Dictionary<string, string>()));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".ReplaceMany(null!));
    }
}
