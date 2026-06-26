// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Compose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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
        CoerceTags();
        ExpandMergeKeys();
    }

    /// <summary>
    /// Walks the resolved node graph and rejects any alias cycle, which the tree profile cannot represent and which
    /// would otherwise cause unbounded recursion during traversal or binding.
    /// </summary>
    /// <exception cref="YamlFormatException">An alias forms a cycle.</exception>
    private void DetectAliasCycles()
    {
        if (_rows.Count == 0)
            return;

        var state = new AliasVisitState[_rows.Count];
        VisitForCycle(0, state);
    }

    /// <summary>
    /// Performs a depth-first visit of a node, resolving aliases and flagging a back edge to a node still on the
    /// traversal stack as a cycle.
    /// </summary>
    /// <param name="index">The row index to visit.</param>
    /// <param name="state">The per-row visit state.</param>
    /// <exception cref="YamlFormatException">A cycle is detected.</exception>
    private void VisitForCycle(int index, AliasVisitState[] state)
    {
        var resolved = Resolve(index);
        if (state[resolved] == AliasVisitState.Visiting)
        {
            var anchor = _rows[resolved].Anchor ?? string.Empty;
            throw new YamlFormatException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlCyclicAlias, anchor));
        }

        if (state[resolved] == AliasVisitState.Visited)
            return;

        state[resolved] = AliasVisitState.Visiting;

        var child = _rows[resolved].FirstChild;
        while (child >= 0)
        {
            VisitForCycle(child, state);
            child = _rows[child].NextSibling;
        }

        state[resolved] = AliasVisitState.Visited;
    }

    /// <summary>
    /// Resolves each alias row to the most recent anchor of the same name defined earlier in the document.
    /// </summary>
    /// <exception cref="YamlFormatException">An alias refers to an anchor that was never defined.</exception>
    private void ResolveAliases()
    {
        var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r.Anchor is not null && !anchors.TryAdd(r.Anchor, i))
            {
                throw new YamlFormatException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlDuplicateAnchor, r.Anchor));
            }

            if (r.Kind == YamlReaderNodeKind.Alias && r.Tag is not null)
            {
                if (!anchors.TryGetValue(r.Tag, out var target))
                {
                    throw new YamlFormatException(string.Format(
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
    private void ExpandMergeKeys()
    {
        if (_mergeKeyBehavior != YamlMergeKeyBehavior.Expand)
            return;

        var count = _rows.Count;
        for (var i = 0; i < count; i++)
        {
            if (_rows[i].Kind == YamlReaderNodeKind.Mapping)
                ExpandMappingMerge(i);
        }
    }

    /// <summary>
    /// Expands a single mapping's merge key, preserving explicit-key and earlier-source precedence.
    /// </summary>
    /// <param name="mapping">The mapping row index.</param>
    private void ExpandMappingMerge(int mapping)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var mergeChild = -1;
        var mergePrev = -1;
        var prev = -1;
        var child = _rows[mapping].FirstChild;

        while (child >= 0)
        {
            var key = _rows[child].Key;
            if (key == "<<" && mergeChild < 0)
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

        var sources = new List<int>();
        var value = Resolve(mergeChild);
        var valueRow = _rows[value];
        if (valueRow.Kind == YamlReaderNodeKind.Mapping)
        {
            sources.Add(value);
        }
        else if (valueRow.Kind == YamlReaderNodeKind.Sequence)
        {
            var element = valueRow.FirstChild;
            while (element >= 0)
            {
                sources.Add(Resolve(element));
                element = _rows[element].NextSibling;
            }
        }

        foreach (var source in sources)
        {
            if (_rows[source].Kind != YamlReaderNodeKind.Mapping)
                continue;

            var pair = _rows[source].FirstChild;
            while (pair >= 0)
            {
                var key = _rows[pair].Key;
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
        var p = _rows[parent];
        var next = _rows[child].NextSibling;

        if (previous < 0)
            p.FirstChild = next;
        else
        {
            var prevRow = _rows[previous];
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
    private int NewMergeAlias(string key, int target)
    {
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
    /// <c>float</c> core tags. The <c>str</c> tag forces any scalar to a string; the others reinterpret a plain or
    /// quoted scalar's text under the requested type.
    /// </remarks>
    private void CoerceTags()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];

            // Alias rows reuse the tag field to carry the alias name, so they are never tag-coerced.
            if (r.Tag is null || r.Kind == YamlReaderNodeKind.Alias)
                continue;

            var expanded = ExpandTagHandle(r.Tag);
            var tag = NormalizeCoreTag(expanded);

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
                var matches = (tag == "map" && r.Kind == YamlReaderNodeKind.Mapping)
                    || (tag == "seq" && r.Kind == YamlReaderNodeKind.Sequence);
                if (!matches)
                    throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);

                continue;
            }

            // A collection tag applied to a scalar is invalid.
            if (tag is "map" or "seq")
                throw ErrorAt(r.Offset, YamlResourceStrings.Format_Invalid_YamlInvalidTag);

            var text = ScalarText(r);
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
                    var resolved = ResolveTagged(text);
                    if (resolved.Kind == YamlValueKind.Float)
                    {
                        r.ValueKind = YamlValueKind.Float;
                        r.ScalarBits = resolved.Bits;
                    }
                    else if (resolved.Kind == YamlValueKind.Integer)
                    {
                        // An integral literal under an explicit !!float tag widens to a floating-point value.
                        r.ValueKind = YamlValueKind.Float;
                        r.ScalarBits = BitConverter.DoubleToInt64Bits(resolved.Bits);
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
        var bytes = Encoding.UTF8.GetBytes(text);
        var kind = YamlScalarResolver.Resolve(bytes, _version, out var bits);
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
    /// Produces the canonical text of a scalar row for tag reinterpretation.
    /// </summary>
    /// <param name="r">The scalar row.</param>
    /// <returns>The scalar's textual form.</returns>
    private string ScalarText(YamlReaderRow r) => r.ValueKind switch
    {
        YamlValueKind.String => _strings[(int)r.ScalarBits],
        YamlValueKind.Integer => r.AsInt64().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Float => r.AsDouble().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Boolean => r.AsBoolean() ? "true" : "false",
        _ => string.Empty,
    };

    /// <summary>
    /// Expands a tag's handle to its full prefix using the active <c>%TAG</c> directive table, resolving secondary
    /// (<c>!!</c>) and named (<c>!name!</c>) handles. Verbatim and already-resolved tags pass through unchanged.
    /// </summary>
    /// <param name="tag">The captured tag text.</param>
    /// <returns>The tag with its handle expanded, when applicable.</returns>
    private string ExpandTagHandle(string tag)
    {
        if (tag.Length == 0 || tag[0] != '!' || tag.StartsWith("!<", StringComparison.Ordinal))
            return tag;

        if (tag.StartsWith("!!", StringComparison.Ordinal))
            return ResolveHandlePrefix("!!", "tag:yaml.org,2002:") + tag[2..];

        var secondBang = tag.IndexOf('!', 1);
        if (secondBang > 0)
        {
            var handle = tag[..(secondBang + 1)];
            if (_tagHandles is not null && _tagHandles.TryGetValue(handle, out var prefix))
                return prefix + tag[(secondBang + 1)..];

            return tag;
        }

        if (_tagHandles is not null && _tagHandles.TryGetValue("!", out var primaryPrefix))
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
        _tagHandles is not null && _tagHandles.TryGetValue(handle, out var prefix) ? prefix : fallback;

    /// <summary>
    /// Normalizes a tag to its core-schema short name when it is a recognized core tag.
    /// </summary>
    /// <param name="tag">The captured tag text.</param>
    /// <returns>The core short name (for example <c>int</c>), or <see langword="null" /> when not a core tag.</returns>
    private static string? NormalizeCoreTag(string tag)
    {
        var name = tag switch
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
        var r = _rows[index];
        return r.Kind == YamlReaderNodeKind.Alias && r.AliasTarget >= 0 ? r.AliasTarget : index;
    }

    /// <summary>
    /// Tracks a node's state during the cycle-detection depth-first walk.
    /// </summary>
    private enum AliasVisitState
    {
        /// <summary>The node has not yet been visited.</summary>
        Unvisited = 0,

        /// <summary>The node is currently on the traversal stack.</summary>
        Visiting = 1,

        /// <summary>The node and its descendants have been fully visited.</summary>
        Visited = 2,
    }
}
