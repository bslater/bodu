// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Node.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections;

/// <summary>
/// Test-only tree node used by <see cref="NodeSampleTree" /> and the RecursiveSelect test suite. Each node carries a
/// human-readable <see cref="Name" />, a list of <see cref="Children" />, and a <see cref="Stop" /> flag that drives
/// the recursion-control test cases.
/// </summary>
public class Node
{

    public List<Node> Children { get; set; } = new();
    public string Name { get; set; } = string.Empty;

    public bool Stop { get; set; } = false;

    public override string ToString() => Name;

}
