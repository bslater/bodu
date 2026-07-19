// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TreesAndTries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Trees;

namespace Bodu.Collections.Samples.RangesGraphsTrees.Scenarios;

/// <summary>
/// Demonstrates the tree and trie family: <see cref="Tree{T}" /> (an n-ary tree with pre/level-order
/// traversals), <see cref="Trie{TValue}" /> (a character trie mapping string keys to values with prefix
/// search), and <see cref="RadixTrie" /> (a compressed prefix set).
/// </summary>
public static class TreesAndTries
{
    /// <summary>
    /// Builds a small directory tree, then runs prefix queries against a value trie and a radix trie.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Tree<T> / Trie<TValue> / RadixTrie ---");

        RunTree();
        RunTrie();
        RunRadixTrie();

        Console.WriteLine();
    }

    /// <summary>
    /// Assembles a rooted tree and walks it in pre-order and level-order.
    /// </summary>
    private static void RunTree()
    {
        // Build a filesystem-shaped tree. Children retain the order they were added.
        var root = new Tree<string>("root");
        var src = root.AddChild("src");
        src.AddChild("app.cs");
        src.AddChild("util.cs");
        var docs = root.AddChild("docs");
        docs.AddChild("readme.md");

        // Pre-order visits a node before its children (depth-first); level-order visits by depth (breadth-first).
        Console.WriteLine($"  pre-order   : {string.Join(", ", root.PreOrder().Select(n => n.Value))}");
        Console.WriteLine($"  level-order : {string.Join(", ", root.LevelOrder().Select(n => n.Value))}");
        Console.WriteLine($"  leaf count  : {root.PreOrder().Count(n => n.IsLeaf)}");
    }

    /// <summary>
    /// Loads a value trie and looks up completions for a prefix plus an exact key.
    /// </summary>
    private static void RunTrie()
    {
        // A trie maps whole string keys to values and can enumerate every key under a prefix.
        var ports = new Trie<int>();
        ports.Add("http", 80);
        ports.Add("https", 443);
        ports.Add("httpx", 8080);
        ports.Add("ftp", 21);

        // KeysWithPrefix order is unspecified, so sort for stable output.
        var httpKeys = ports.KeysWithPrefix("http").OrderBy(k => k, StringComparer.Ordinal);
        Console.WriteLine($"  keys under 'http' : {string.Join(", ", httpKeys)}");
        Console.WriteLine($"  https -> port     : {ports["https"]}");
        Console.WriteLine($"  any key 'ft*'?    : {ports.StartsWith("ft")}");
    }

    /// <summary>
    /// Loads a compressed radix trie and runs membership and prefix queries.
    /// </summary>
    private static void RunRadixTrie()
    {
        // A radix trie stores the same key set as a trie but merges single-child chains to save nodes.
        var words = new RadixTrie(new[] { "team", "tea", "teapot", "ten" });

        var teaWords = words.KeysWithPrefix("tea").OrderBy(k => k, StringComparer.Ordinal);
        Console.WriteLine($"  radix keys 'tea*' : {string.Join(", ", teaWords)}");
        Console.WriteLine($"  contains 'ten'    : {words.Contains("ten")}");
        Console.WriteLine($"  contains 'te'     : {words.Contains("te")}");
    }
}
