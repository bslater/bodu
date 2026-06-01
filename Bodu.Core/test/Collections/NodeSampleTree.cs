// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NodeSampleTree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections;

public static class NodeSampleTree
{

    public static Node[] BuildSampleTree()
    {
        return [
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
        ];
    }

}
