// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.ContainsKey.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> honours the custom key
    /// comparer, returning <see langword="true"/> for a key that differs only in case.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenCustomComparerUsed_ShouldMatchCaseInsensitively()
    {
        var mvd =
            new MultiValueDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        mvd.Add("FOO", 1);

        Assert.IsTrue(mvd.ContainsKey("foo"));
        Assert.IsTrue(mvd.ContainsKey("FOO"));
        Assert.IsTrue(mvd.ContainsKey("Foo"));
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> returns <see langword="false"/> for an absent key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyAbsent_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.IsFalse(mvd.ContainsKey("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsNull_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.ContainsKey(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> returns <see langword="true"/> when the key is present.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresent_ShouldReturnTrue()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);

        Assert.IsTrue(mvd.ContainsKey("k"));
    }

}
