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
/// <remarks>
/// A trie is not a dictionary with extra steps: it is indexed by <em>prefix</em>, so "every key starting with
/// this" is a walk to one node rather than a scan of every key. The distinction that catches people is that
/// reaching a node is not the same as that node being a key — <c>te</c> is a real node on the way to <c>ten</c>
/// and is still not a member.
/// </remarks>
public static class TreesAndTries
{
    /// <summary>
    /// Builds a small directory tree, then runs prefix queries against a value trie and a radix trie.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Tree<T> / Trie<TValue> / RadixTrie",
            what: "Walks an n-ary tree in pre-order and level-order, looks up keys by prefix in a trie, and asks a " +
                  "radix trie about a word that is only a waypoint rather than a member.",
            why: "The two traversals answer different questions: pre-order follows each branch to its end, which " +
                  "is what you want for rendering a hierarchy; level-order sweeps by depth, which is what you want " +
                  "for breadth-first work or for anything sensitive to distance from the root. The tries exist " +
                  "because prefix is the index - completing \"http\" is a walk to one node, where a dictionary " +
                  "would have to examine every key. The membership trap is the thing to remember: passing through " +
                  "a node does not make it a key.",
            expect: "The same six nodes print in two different orders, and only the traversal differs. The trie " +
                    "returns three keys under http and answers a prefix-existence question without materialising " +
                    "the matches. In the radix trie, ten is a member and te is not, despite te being on the path " +
                    "to it.");

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
        Console.WriteLine($"  pre-order   : {string.Join(", ", root.PreOrder().Select(n => n.Value))}  (depth first - each branch is finished before the next begins, which is the order a directory listing reads in)");
        Console.WriteLine($"  level-order : {string.Join(", ", root.LevelOrder().Select(n => n.Value))}  (breadth first - every node at one depth before any at the next; same six nodes, different question)");
        Console.WriteLine($"  leaf count  : {root.PreOrder().Count(n => n.IsLeaf)}  (expected 3 - the files; traversal order does not affect a count, only the sequence)");
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
        Console.WriteLine($"  keys under \u0027http\u0027 : {string.Join(", ", httpKeys)}  (expected http, https, httpx - one walk to the http node, then everything below it; http is both a key and a prefix)");
        Console.WriteLine($"  https -> port     : {ports["https"]}  (expected 443 - an exact lookup still works; the prefix index does not cost you the dictionary behaviour)");

        // StartsWith only tests whether any stored key begins with the prefix - cheaper than
        // KeysWithPrefix when the matches themselves are not needed.
        Console.WriteLine($"  any key \u0027ft*\u0027?    : {ports.StartsWith("ft")}  (expected True - answers whether anything shares the prefix without enumerating the matches)");
    }

    /// <summary>
    /// Loads a compressed radix trie and runs membership and prefix queries.
    /// </summary>
    private static void RunRadixTrie()
    {
        // A radix trie stores the same key set as a trie but merges single-child chains to save nodes.
        var words = new RadixTrie(new[] { "team", "tea", "teapot", "ten" });

        var teaWords = words.KeysWithPrefix("tea").OrderBy(k => k, StringComparer.Ordinal);
        Console.WriteLine($"  radix keys \u0027tea*\u0027 : {string.Join(", ", teaWords)}  (a radix trie compresses single-child chains into one edge, so it stores the same keys in fewer nodes)");
        // Membership is exact: "te" is a prefix of several stored keys but was never added itself, so
        // Contains reports false for it even though prefix queries would find keys beneath it.
        Console.WriteLine($"  contains \u0027ten\u0027    : {words.Contains("ten")}  (expected True - added as a key)");
        Console.WriteLine($"  contains \u0027te\u0027     : {words.Contains("te")}  (expected False - te is on the path to ten and tea, but reaching a node is not membership)");
    }
}
