// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Compose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Composition passes applied to a parsed document before it is returned: alias resolution, merge-key (<c>&lt;&lt;</c>)
/// expansion, and core-schema tag coercion.
/// </summary>
internal sealed partial class YamlParser
{
    /// <summary>
    /// Runs the composition passes over the current row store in dependency order.
    /// </summary>
    private void Compose()
    {
        ResolveAliases();
        DetectAliasCycles();
        EnforceExpansionBudget();
        CoerceTags();

        // Merge expansion appends alias rows, so the budget is re-checked over the post-injection store. Expansion
        // cannot introduce a cycle - aliases only ever point backward, and a merge source whose subtree contains the
        // receiving mapping is already rejected above through the merge key's own alias - so cycle detection is not
        // re-run.
        if (ExpandMergeKeys())
            EnforceExpansionBudget();
    }

    /// <summary>
    /// Rejects a document whose resolved alias graph would expand to more than
    /// <see cref="YamlLimits.AbsoluteMaxExpandedNodes" /> nodes when materialized into a tree.
    /// </summary>
    /// <remarks>
    /// Aliases are stored as shared references, so the parsed row graph is linear in the input size, but every consumer
    /// that walks the graph (the node DOM, the read-only document, and the serializer) expands each alias into a full
    /// copy of its target. Chained aliases amplify exponentially - the "billion laughs" attack. This pass computes the
    /// materialized node count once, memoized over the (already acyclic) graph, and rejects the document before any
    /// consumer can exhaust memory. It runs after <see cref="DetectAliasCycles" /> so the walk is guaranteed to
    /// terminate.
    /// </remarks>
    /// <exception cref="YamlFormatException">The expanded node count exceeds the permitted maximum.</exception>
    private void EnforceExpansionBudget()
    {
        if (_rows.Count == 0)
            return;

        // Memoized expanded-node count per resolved row; 0 marks "not yet computed" (a real count is always >= 1).
        // The walk is an explicit-stack post-order traversal rather than native recursion: chained aliases can make
        // the materialized tree far deeper than the parser's physical nesting clamp, so a recursive counter would
        // itself risk a StackOverflowException on a long alias chain.
        long[] counts = new long[_rows.Count];
        var nodes = new Stack<int>();
        var nextChild = new Stack<int>();
        var accumulated = new Stack<long>();

        int root = Resolve(0);
        nodes.Push(root);
        nextChild.Push(_rows[root].FirstChild);
        accumulated.Push(1);

        while (nodes.Count > 0)
        {
            int child = nextChild.Pop();

            if (child < 0)
            {
                // Every child of the current node has been counted: finalize and fold into the parent's total.
                int done = nodes.Pop();
                long total = accumulated.Pop();
                counts[done] = total;

                if (nodes.Count > 0)
                    accumulated.Push(CheckedAdd(accumulated.Pop(), total));

                continue;
            }

            // Advance the current node to its next sibling before descending into (or folding) this child.
            nextChild.Push(_rows[child].NextSibling);

            int resolvedChild = Resolve(child);
            if (counts[resolvedChild] != 0)
            {
                // A shared target already measured: add its cached expansion without re-walking it.
                accumulated.Push(CheckedAdd(accumulated.Pop(), counts[resolvedChild]));
            }
            else
            {
                nodes.Push(resolvedChild);
                nextChild.Push(_rows[resolvedChild].FirstChild);
                accumulated.Push(1);
            }
        }
    }

    /// <summary>
    /// Adds two expanded-node counts, throwing once the sum exceeds the permitted maximum.
    /// </summary>
    /// <param name="current">The running total.</param>
    /// <param name="addition">The count to add.</param>
    /// <returns>The new running total.</returns>
    /// <exception cref="YamlFormatException">
    /// The total exceeds <see cref="YamlLimits.AbsoluteMaxExpandedNodes" />.
    /// </exception>
    private static long CheckedAdd(long current, long addition)
    {
        long total = current + addition;
        if (total > YamlLimits.AbsoluteMaxExpandedNodes)
        {
            throw new YamlFormatException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlAliasExpansionTooLarge, YamlLimits.AbsoluteMaxExpandedNodes));
        }

        return total;
    }

    /// <summary>
    /// Walks the resolved node graph and rejects any alias cycle, which the tree profile cannot represent and which
    /// would otherwise cause unbounded recursion during traversal or binding.
    /// </summary>
    /// <exception cref="YamlFormatException">An alias forms a cycle; the error is reported at that alias.</exception>
    private void DetectAliasCycles()
    {
        if (_rows.Count == 0)
            return;

        var state = new AliasVisitState[_rows.Count];
        VisitForCycles(state);
    }

    /// <summary>
    /// Performs a depth-first visit of the resolved node graph, flagging a back edge to a node still on the traversal
    /// stack as a cycle.
    /// </summary>
    /// <remarks>
    /// The walk is an explicit-stack traversal for the same reason <see cref="EnforceExpansionBudget" /> uses one:
    /// chained aliases compose resolved depth far beyond the parser's physical nesting clamp, so native recursion could
    /// exhaust the call stack on a crafted document before the cycle (or its absence) is ever established.
    /// </remarks>
    /// <param name="state">The per-row visit state.</param>
    /// <exception cref="YamlFormatException">
    /// A cycle is detected; the error is reported at the alias that closes it.
    /// </exception>
    private void VisitForCycles(AliasVisitState[] state)
    {
        var nodes = new Stack<int>();
        var nextChild = new Stack<int>();

        int root = Resolve(0);
        state[root] = AliasVisitState.Visiting;
        nodes.Push(root);
        nextChild.Push(_rows[root].FirstChild);

        while (nodes.Count > 0)
        {
            int child = nextChild.Pop();

            if (child < 0)
            {
                // Every child has been visited: the node leaves the traversal stack fully explored.
                state[nodes.Pop()] = AliasVisitState.Visited;
                continue;
            }

            // Advance the current node to its next sibling before descending into this child.
            nextChild.Push(_rows[child].NextSibling);

            int resolved = Resolve(child);
            if (state[resolved] == AliasVisitState.Visiting)
            {
                // Only an alias can lead back to a node still being visited, so the child is the alias that closes the
                // cycle, and its row's offset is where the alias is written.
                string anchor = _rows[resolved].Anchor ?? string.Empty;
                throw ErrorAt(_rows[child].Offset, string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlCyclicAlias, anchor));
            }

            if (state[resolved] == AliasVisitState.Visited)
                continue;

            state[resolved] = AliasVisitState.Visiting;
            nodes.Push(resolved);
            nextChild.Push(_rows[resolved].FirstChild);
        }
    }

    /// <summary>
    /// Resolves each alias row to the anchor of the same name defined earlier in the document. Anchor names are unique
    /// within a document: the tree profile rejects a redefinition (where the YAML specification would rebind later
    /// aliases to the most recent definition) as the parser reads it, so that every alias has a single unambiguous
    /// target.
    /// </summary>
    /// <exception cref="YamlFormatException">An alias refers to an anchor that was never defined.</exception>
    private void ResolveAliases()
    {
        var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < _rows.Count; i++)
        {
            YamlReaderRow r = _rows[i];

            // The parser rejects a repeated anchor name where it is written, so each name here belongs to one row.
            if (r.Anchor is not null)
                anchors[r.Anchor] = i;

            if (r.Kind == YamlReaderNodeKind.Alias && r.Tag is not null)
            {
                // The alias row's offset is where the alias is written, so the error points at it.
                if (!anchors.TryGetValue(r.Tag, out int target))
                {
                    throw ErrorAt(r.Offset, string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlUndefinedAlias, r.Tag));
                }

                r.AliasTarget = target;
                _rows[i] = r;
            }
        }
    }

    /// <summary>
    /// Expands merge keys by injecting alias rows for keys contributed by the merged mappings.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when at least one alias row was injected; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="YamlFormatException">
    /// Injection would grow the row store past <see cref="YamlLimits.AbsoluteMaxExpandedNodes" />.
    /// </exception>
    private bool ExpandMergeKeys()
    {
        if (_mergeKeyBehavior != YamlMergeKeyBehavior.Expand)
            return false;

        int count = _rows.Count;
        for (int i = 0; i < count; i++)
        {
            if (_rows[i].Kind == YamlReaderNodeKind.Mapping)
                ExpandMappingMerge(i);
        }

        return _rows.Count > count;
    }

    /// <summary>
    /// Expands a single mapping's merge key, preserving explicit-key and earlier-source precedence.
    /// </summary>
    /// <param name="mapping">The mapping row index.</param>
    /// <exception cref="YamlFormatException">
    /// The merge key's value is not a mapping, an alias of one, or a sequence of them.
    /// </exception>
    private void ExpandMappingMerge(int mapping)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        int mergeChild = -1;
        int mergePrev = -1;
        int prev = -1;
        int child = _rows[mapping].FirstChild;

        while (child >= 0)
        {
            string? key = _rows[child].Key;
            if ((_rows[child].Flags & YamlReaderRowFlags.MergeKey) != 0 && mergeChild < 0)
            {
                mergeChild = child;
                mergePrev = prev;
            }
            else if (key is not null)
            {
                keys.Add(key);
            }

            prev = child;
            child = _rows[child].NextSibling;
        }

        if (mergeChild < 0)
            return;

        UnlinkChild(mapping, mergeChild, mergePrev);

        // Only a mapping, or a sequence of mappings, can be merged; anything else is reported where the merge value is
        // written, or, within a sequence written in place, at the offending item.
        var sources = new List<int>();
        int value = Resolve(mergeChild);
        YamlReaderRow valueRow = _rows[value];
        if (valueRow.Kind == YamlReaderNodeKind.Mapping)
        {
            sources.Add(value);
        }
        else if (valueRow.Kind == YamlReaderNodeKind.Sequence)
        {
            bool aliased = _rows[mergeChild].Kind == YamlReaderNodeKind.Alias;
            int element = valueRow.FirstChild;
            while (element >= 0)
            {
                int source = Resolve(element);
                if (_rows[source].Kind != YamlReaderNodeKind.Mapping)
                {
                    int at = aliased ? _rows[mergeChild].Offset : _rows[element].Offset;
                    throw ErrorAt(at, YamlResourceStrings.Format_Invalid_YamlMergeKeyTarget);
                }

                sources.Add(source);
                element = _rows[element].NextSibling;
            }
        }
        else
        {
            throw ErrorAt(_rows[mergeChild].Offset, YamlResourceStrings.Format_Invalid_YamlMergeKeyTarget);
        }

        foreach (int source in sources)
        {
            int pair = _rows[source].FirstChild;
            while (pair >= 0)
            {
                string? key = _rows[pair].Key;
                if (key is not null && keys.Add(key))
                    AppendChild(mapping, NewMergeAlias(key, Resolve(pair)));

                pair = _rows[pair].NextSibling;
            }
        }
    }

    /// <summary>
    /// Removes a child from a container's sibling chain and updates its bookkeeping.
    /// </summary>
    /// <param name="parent">The container row index.</param>
    /// <param name="child">The child row index to remove.</param>
    /// <param name="previous">The child's predecessor row index, or <c>-1</c> when it is the first child.</param>
    private void UnlinkChild(int parent, int child, int previous)
    {
        YamlReaderRow p = _rows[parent];
        int next = _rows[child].NextSibling;

        if (previous < 0)
        {
            p.FirstChild = next;
        }
        else
        {
            YamlReaderRow prevRow = _rows[previous];
            prevRow.NextSibling = next;
            _rows[previous] = prevRow;
        }

        if (p.LastChild == child)
            p.LastChild = previous;

        p.ChildCount--;
        _rows[parent] = p;
    }

    /// <summary>
    /// Creates a merge-injected alias row carrying a key and pointing at a merged value.
    /// </summary>
    /// <param name="key">The merged key.</param>
    /// <param name="target">The resolved target row index.</param>
    /// <returns>The row index of the new alias.</returns>
    /// <exception cref="YamlFormatException">
    /// The row store has already grown to <see cref="YamlLimits.AbsoluteMaxExpandedNodes" /> rows.
    /// </exception>
    private int NewMergeAlias(string key, int target)
    {
        // Chained merges copy earlier mappings' injected aliases forward, so row growth can be quadratic in the
        // input; bound the store by the same ceiling the expansion budget enforces rather than letting it grow
        // unchecked before the post-merge budget pass runs.
        if (_rows.Count >= YamlLimits.AbsoluteMaxExpandedNodes)
        {
            throw new YamlFormatException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlAliasExpansionTooLarge, YamlLimits.AbsoluteMaxExpandedNodes));
        }

        var row = new YamlReaderRow
        {
            Kind = YamlReaderNodeKind.Alias,
            ValueKind = YamlValueKind.None,
            ScalarStyle = YamlScalarStyle.Plain,
            Key = key,
            Anchor = null,
            Tag = null,
            Offset = _rows[target].Offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
            AliasTarget = target,
            ChildCount = 0,
            Depth = 0,
            Flags = YamlReaderRowFlags.Merged,
        };
        _rows.Add(row);
        return _rows.Count - 1;
    }

    /// <summary>
    /// Applies core-schema tag coercion to tagged scalar rows.
    /// </summary>
    /// <remarks>
    /// Handles the verbatim and shorthand spellings of the <c>str</c>, <c>null</c>, <c>bool</c>, <c>int</c>, and
    /// <c>float</c> core tags. The <c>str</c> tag, like the non-specific tag <c>!</c>, makes the scalar a string that
    /// keeps its text as written; the <c>null</c> tag makes it null; and the others reinterpret the text as written,
    /// not its implicitly resolved value, under the requested type.
    /// </remarks>
    private void CoerceTags()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            YamlReaderRow r = _rows[i];

            // Alias rows reuse the tag field to carry the alias name, so they are never tag-coerced.
            if (r.Tag is null || r.Kind == YamlReaderNodeKind.Alias)
                continue;

            string expanded = DecodeTagEscapes(ExpandTagHandle(r.Tag), r.Offset);
            string? tag = NormalizeCoreTag(expanded);

            if (tag is null)
            {
                // The non-specific tag '!' forces a non-plain (string) resolution on a scalar and is a no-op on a
                // collection. Any other unrecognized tag is outside the JSON-compatible profile.
                if (expanded == "!")
                {
                    if (r.Kind == YamlReaderNodeKind.Scalar)
                    {
                        SetString(ref r, ScalarText(r));
                        _rows[i] = r;
                    }

                    continue;
                }

                throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);
            }

            if (r.Kind != YamlReaderNodeKind.Scalar)
            {
                // A collection accepts only its matching core collection tag.
                bool matches = (tag == "map" && r.Kind == YamlReaderNodeKind.Mapping)
                    || (tag == "seq" && r.Kind == YamlReaderNodeKind.Sequence);
                if (!matches)
                    throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);

                continue;
            }

            // A collection tag applied to a scalar is invalid.
            if (tag is "map" or "seq")
                throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);

            string text = ScalarText(r);
            switch (tag)
            {
                case "str":
                    SetString(ref r, text);
                    break;

                case "null":
                    r.ValueKind = YamlValueKind.Null;
                    r.ScalarBits = 0;
                    break;

                case "bool":
                    if (ResolveTagged(text) is (YamlValueKind.Boolean, var boolBits))
                    {
                        r.ValueKind = YamlValueKind.Boolean;
                        r.ScalarBits = boolBits;
                    }
                    else
                    {
                        throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);
                    }

                    break;

                case "int":
                    if (ResolveTagged(text) is (YamlValueKind.Integer, var intBits))
                    {
                        r.ValueKind = YamlValueKind.Integer;
                        r.ScalarBits = intBits;
                    }
                    else
                    {
                        throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);
                    }

                    break;

                case "float":
                    (YamlValueKind Kind, long Bits) = ResolveTagged(text);
                    if (Kind == YamlValueKind.Float)
                    {
                        r.ValueKind = YamlValueKind.Float;
                        r.ScalarBits = Bits;
                    }
                    else if (Kind == YamlValueKind.Integer)
                    {
                        // An integral literal under an explicit !!float tag widens to a floating-point value.
                        r.ValueKind = YamlValueKind.Float;
                        r.ScalarBits = BitConverter.DoubleToInt64Bits(Bits);
                    }
                    else
                    {
                        throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);
                    }

                    break;
            }

            _rows[i] = r;
        }
    }

    /// <summary>
    /// Resolves the scalar text of an explicitly tagged scalar under the active schema, so that tagged content is
    /// validated consistently with implicit typing (for example <c>!!int 0x2A</c> and <c>!!float .inf</c>).
    /// </summary>
    /// <param name="text">The decoded scalar content.</param>
    /// <returns>The resolved value kind and its packed payload.</returns>
    private (YamlValueKind Kind, long Bits) ResolveTagged(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        YamlValueKind kind = YamlScalarResolver.Resolve(bytes, _version, out long bits);
        return (kind, bits);
    }

    /// <summary>
    /// Reinterprets the row as a string scalar carrying the given text.
    /// </summary>
    /// <param name="r">The row to modify.</param>
    /// <param name="text">The string value.</param>
    private void SetString(ref YamlReaderRow r, string text)
    {
        if (r.ValueKind == YamlValueKind.String)
            return;

        r.ValueKind = YamlValueKind.String;
        r.ScalarBits = _strings.Count;
        _strings.Add(text);
    }

    /// <summary>
    /// Reads the text of a scalar row as written, for tag reinterpretation and for naming a mapping key.
    /// </summary>
    /// <param name="r">The scalar row.</param>
    /// <returns>
    /// The decoded text of a string scalar, the source text of a plain scalar that resolved to another value, or the
    /// empty string for an empty node.
    /// </returns>
    private string ScalarText(YamlReaderRow r) =>
        r.ValueKind == YamlValueKind.String ? _strings[(int)r.ScalarBits] : Utf8(r.Offset, r.SourceLength);

    /// <summary>
    /// Decodes the <c>%</c>-escaped characters of a tag, each a <c>%</c> and two hexadecimal digits, as UTF-8.
    /// </summary>
    /// <param name="tag">The tag, with its handle expanded.</param>
    /// <param name="offset">The byte offset of the tagged node, for error reporting.</param>
    /// <returns>The tag with its escapes decoded, or the tag itself when it holds none.</returns>
    /// <exception cref="YamlFormatException">
    /// A <c>%</c> is not followed by two hexadecimal digits, or the escaped bytes are not UTF-8.
    /// </exception>
    private string DecodeTagEscapes(string tag, int offset)
    {
        if (!tag.Contains('%', StringComparison.Ordinal))
            return tag;

        var bytes = new List<byte>(tag.Length);
        int run = 0;
        for (int i = 0; i < tag.Length; i++)
        {
            if (tag[i] != '%')
                continue;

            if (i + 2 >= tag.Length
                || !byte.TryParse(tag.AsSpan(i + 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte value))
            {
                throw ErrorAt(offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);
            }

            bytes.AddRange(Encoding.UTF8.GetBytes(tag, run, i - run));
            bytes.Add(value);
            i += 2;
            run = i + 1;
        }

        bytes.AddRange(Encoding.UTF8.GetBytes(tag, run, tag.Length - run));

        try
        {
            return new UTF8Encoding(false, true).GetString(CollectionsMarshal.AsSpan(bytes));
        }
        catch (DecoderFallbackException)
        {
            throw ErrorAt(offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);
        }
    }

    /// <summary>
    /// Expands a tag's handle to its full prefix using the active <c>%TAG</c> directive table, resolving secondary (<c>!!</c>)
    /// and named (<c>!name!</c>) handles. Verbatim and already-resolved tags pass through unchanged.
    /// </summary>
    /// <param name="tag">The captured tag text.</param>
    /// <returns>The tag with its handle expanded, when applicable.</returns>
    private string ExpandTagHandle(string tag)
    {
        if (tag.Length == 0 || tag[0] != '!' || tag.StartsWith("!<", StringComparison.Ordinal))
            return tag;

        if (tag.StartsWith("!!", StringComparison.Ordinal))
            return ResolveHandlePrefix("!!", "tag:yaml.org,2002:") + tag[2..];

        int secondBang = tag.IndexOf('!', 1);
        if (secondBang > 0)
        {
            string handle = tag[..(secondBang + 1)];
            if (_tagHandles is not null && _tagHandles.TryGetValue(handle, out string? prefix))
                return prefix + tag[(secondBang + 1)..];

            return tag;
        }

        if (_tagHandles is not null && _tagHandles.TryGetValue("!", out string? primaryPrefix))
            return primaryPrefix + tag[1..];

        return tag;
    }

    /// <summary>
    /// Resolves a handle to its prefix from the active <c>%TAG</c> table, falling back to a default prefix.
    /// </summary>
    /// <param name="handle">The handle to resolve.</param>
    /// <param name="fallback">The default prefix when the handle is not redefined.</param>
    /// <returns>The resolved prefix.</returns>
    private string ResolveHandlePrefix(string handle, string fallback) =>
        _tagHandles is not null && _tagHandles.TryGetValue(handle, out string? prefix) ? prefix : fallback;

    /// <summary>
    /// Normalizes a tag to its core-schema short name when it is a recognized core tag.
    /// </summary>
    /// <param name="tag">The captured tag text.</param>
    /// <returns>The core short name (for example <c>int</c>), or <see langword="null" /> when not a core tag.</returns>
    private static string? NormalizeCoreTag(string tag)
    {
        string? name = tag switch
        {
            "!!str" or "tag:yaml.org,2002:str" or "!<tag:yaml.org,2002:str>" => "str",
            "!!null" or "tag:yaml.org,2002:null" or "!<tag:yaml.org,2002:null>" => "null",
            "!!bool" or "tag:yaml.org,2002:bool" or "!<tag:yaml.org,2002:bool>" => "bool",
            "!!int" or "tag:yaml.org,2002:int" or "!<tag:yaml.org,2002:int>" => "int",
            "!!float" or "tag:yaml.org,2002:float" or "!<tag:yaml.org,2002:float>" => "float",
            "!!map" or "tag:yaml.org,2002:map" or "!<tag:yaml.org,2002:map>" => "map",
            "!!seq" or "tag:yaml.org,2002:seq" or "!<tag:yaml.org,2002:seq>" => "seq",
            _ => null,
        };

        return name;
    }

    /// <summary>
    /// Resolves an alias row to its target, or returns the index unchanged for non-alias rows.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The resolved row index.</returns>
    private int Resolve(int index)
    {
        YamlReaderRow r = _rows[index];
        return r.Kind == YamlReaderNodeKind.Alias && r.AliasTarget >= 0 ? r.AliasTarget : index;
    }

    /// <summary>
    /// Tracks a node's state during the cycle-detection depth-first walk.
    /// </summary>
    private enum AliasVisitState
    {
        /// <summary>
        /// The node has not yet been visited.
        /// </summary>
        Unvisited = 0,

        /// <summary>
        /// The node is currently on the traversal stack.
        /// </summary>
        Visiting = 1,

        /// <summary>
        /// The node and its descendants have been fully visited.
        /// </summary>
        Visited = 2,
    }
}
