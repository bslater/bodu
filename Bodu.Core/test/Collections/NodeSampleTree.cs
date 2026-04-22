// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NodeSampleTree.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Collections
{
    public class Node
    {
        public string Name { get; set; } = string.Empty;

        public List<Node> Children { get; set; } = new();

        public bool Stop { get; set; } = false;

        public override string ToString() => Name;
    }

    public static class NodeSampleTree
    {
        public static Node[] BuildSampleTree()
        {
            return new[] {
                new  Node
                {
                    Name = "Root",
                    Children =
                    {
                        new Node { Name = "A" },
                        new Node
                        {
                            Name = "B",
                            Children =
                            {
                                new Node { Name = "B1",Stop = true },
                                new Node { Name = "B2" },
                            }
                        },
                        new Node
                        {
                            Name = "C",
                            Children =
                            {
                                new Node
                                {
                                    Name = "C1",
                                    Stop = true,
                                    Children =
                                    {
                                        new Node { Name = "C1A" },
                                    }
                                },
                                new Node
                                {
                                    Name = "C2",

                                    Children =
                                    {
                                        new Node { Name = "C2A" },
                                        new Node { Name = "C2B", Stop = true},
                                        new Node { Name = "C2C" },
                                    }
                                }
                            }
                        },
                        new Node{Name="D"},
                        new Node{Name="E",Stop=true},
                    }
                }
            };
        }
    }
}